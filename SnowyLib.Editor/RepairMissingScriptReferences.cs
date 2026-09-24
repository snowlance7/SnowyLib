using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SnowyLib.Editor
{
    public static class RepairMissingScriptReferences
    {
        private const string ModAssetsFolder = "Assets/ModAssets";

        private static readonly Regex ScriptReferenceRegex = new Regex(
            @"m_Script:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*3\}",
            RegexOptions.Compiled
        );

        private class ScriptInfo
        {
            public string Guid;
            public long FileId;
            public string Name;
            public string Path;
            public Type Type;
        }

        [MenuItem("Tools/Modding/Repair ModAssets Script References")]
        public static void Repair()
        {
            Debug.Log("=== ModAssets Script Reference Repair ===");

            Dictionary<long, List<ScriptInfo>> scriptsByFileId =
                BuildCurrentScriptDatabase();

            string absoluteRoot = Path.GetFullPath(ModAssetsFolder);

            if (!Directory.Exists(absoluteRoot))
            {
                Debug.LogError(
                    $"Could not find ModAssets folder: {ModAssetsFolder}"
                );

                return;
            }

            string[] files = Directory.GetFiles(
                absoluteRoot,
                "*.*",
                SearchOption.AllDirectories
            );

            int scanned = 0;
            int modified = 0;
            int referencesRepaired = 0;
            int ambiguous = 0;

            foreach (string file in files)
            {
                string extension = Path.GetExtension(file);

                if (extension != ".asset" && extension != ".prefab")
                    continue;

                scanned++;

                string yaml = File.ReadAllText(file);

                MatchCollection matches =
                    ScriptReferenceRegex.Matches(yaml);

                if (matches.Count == 0)
                    continue;

                string updatedYaml = yaml;
                bool fileModified = false;

                foreach (Match match in matches)
                {
                    long fileId = long.Parse(match.Groups[1].Value);
                    string oldGuid = match.Groups[2].Value;

                    // Is this already a valid current reference?
                    if (scriptsByFileId.TryGetValue(
                            fileId,
                            out List<ScriptInfo> candidates))
                    {
                        List<ScriptInfo> matchingCandidates =
                            candidates.FindAll(
                                x => x.Guid.Equals(
                                    oldGuid,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            );

                        if (matchingCandidates.Count > 0)
                        {
                            // Already points to a valid current script.
                            continue;
                        }

                        // We have exactly one possible current script
                        // with this fileID.
                        if (candidates.Count == 1)
                        {
                            ScriptInfo replacement = candidates[0];

                            string oldReference = match.Value;

                            string newReference =
                                $"m_Script: {{fileID: {fileId}, guid: {replacement.Guid}, type: 3}}";

                            updatedYaml = updatedYaml.Replace(
                                oldReference,
                                newReference
                            );

                            fileModified = true;
                            referencesRepaired++;

                            Debug.Log(
                                $"Repaired:\n" +
                                $"  {GetRelativePath(file)}\n" +
                                $"  {oldGuid} -> {replacement.Guid}\n" +
                                $"  fileID: {fileId}\n" +
                                $"  Script: {replacement.Name}"
                            );
                        }
                        else
                        {
                            ambiguous++;

                            Debug.LogWarning(
                                $"AMBIGUOUS SCRIPT REFERENCE:\n" +
                                $"  {GetRelativePath(file)}\n" +
                                $"  GUID: {oldGuid}\n" +
                                $"  fileID: {fileId}\n" +
                                $"  Possible scripts: {candidates.Count}\n" +
                                $"  This reference was NOT changed."
                            );
                        }
                    }
                }

                if (fileModified)
                {
                    File.WriteAllText(file, updatedYaml);
                    modified++;
                }
            }

            AssetDatabase.Refresh();

            Debug.Log(
                $"=== Repair Complete ===\n" +
                $"Files scanned: {scanned}\n" +
                $"Files modified: {modified}\n" +
                $"Script references repaired: {referencesRepaired}\n" +
                $"Ambiguous references skipped: {ambiguous}"
            );
        }

        private static Dictionary<long, List<ScriptInfo>>
            BuildCurrentScriptDatabase()
        {
            var database =
                new Dictionary<long, List<ScriptInfo>>();

            MonoScript[] scripts =
                Resources.FindObjectsOfTypeAll<MonoScript>();

            foreach (MonoScript script in scripts)
            {
                if (script == null)
                    continue;

                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        script,
                        out string guid,
                        out long fileId))
                {
                    continue;
                }

                Type type = script.GetClass();

                if (!database.TryGetValue(
                        fileId,
                        out List<ScriptInfo> list))
                {
                    list = new List<ScriptInfo>();
                    database[fileId] = list;
                }

                list.Add(new ScriptInfo
                {
                    Guid = guid,
                    FileId = fileId,
                    Name = script.name,
                    Path = AssetDatabase.GetAssetPath(script),
                    Type = type
                });
            }

            return database;
        }

        private static string GetRelativePath(string absolutePath)
        {
            string fullRoot =
                Path.GetFullPath(ModAssetsFolder)
                    .TrimEnd(Path.DirectorySeparatorChar);

            string relative =
                absolutePath.Substring(fullRoot.Length)
                    .TrimStart(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar
                    );

            return $"{ModAssetsFolder}/{relative}"
                .Replace('\\', '/');
        }
    }
}