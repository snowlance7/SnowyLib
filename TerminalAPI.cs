using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public static class TerminalAPI
    {
        internal static TerminalResult? previousResult;
        internal static List<TerminalCommand> registeredTerminalCommands = new List<TerminalCommand>();
        internal static TerminalResult? TryParseCommand(string[] args)
        {
            if (previousResult != null)
            {
                TerminalCommand? callbackCommand = previousResult.callbacks.FirstOrDefault(x => SharesCommonPrefix(x.command, args[0]));
                if (callbackCommand != null)
                {
                    previousResult = callbackCommand.operation.Invoke(args);
                    return previousResult;
                }
            }

            previousResult = null;
            TerminalCommand? command = registeredTerminalCommands.FirstOrDefault(x => x.command == args[0]);
            if (command == null) { return null; }

            previousResult = command.operation.Invoke(args);
            return previousResult;
        }

        internal static bool SharesCommonPrefix(string word1, string word2)
        {
            word1 = word1.ToLower();
            word2 = word2.ToLower();

            for (int num = word1.Length; num > 0; num--)
            {
                if (word2.StartsWith(word1.Substring(0, num)))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool RegisterTerminalCommand(string command, Func<string[], TerminalResult> operation, string category = "", string title = "", string description = "")
        {
            return RegisterTerminalCommand(new TerminalCommand(command, operation, category, title, description));
        }

        public static bool RegisterTerminalCommand(string command, Func<string[], string> operation, string category = "", string title = "", string description = "")
        {
            return RegisterTerminalCommand(new TerminalCommand(command, operation, category, title, description));
        }

        public static bool RegisterTerminalCommand(TerminalCommand terminalCommand)
        {
            if (registeredTerminalCommands.Any(x => x.command == terminalCommand.command)) { return false; }
            registeredTerminalCommands.Add(terminalCommand);
            return true;
        }

        public static void RemoveTerminalCommand(TerminalCommand terminalCommand)
        {
            registeredTerminalCommands.Remove(terminalCommand);
        }

        public static void RemoveTerminalCommand(string command)
        {
            TerminalCommand? terminalCommand = registeredTerminalCommands.FirstOrDefault(x => x.command == command);
            if (terminalCommand == null) { return; }
            registeredTerminalCommands.Remove(terminalCommand);
        }

        internal static void SetCategories(Terminal terminal)
        {
            foreach (var terminalCommand in registeredTerminalCommands)
            {
                if (string.IsNullOrWhiteSpace(terminalCommand.category)) { continue; }
                TerminalKeyword? categoryKeyword = terminal.terminalNodes.allKeywords.FirstOrDefault(x => x.word.ToLower() == terminalCommand.category.ToLower());
                if (categoryKeyword == null || categoryKeyword.specialKeywordResult == null) { continue; }

                string text = $"\n>{(string.IsNullOrWhiteSpace(terminalCommand.title) ? terminalCommand.command.ToUpper() : terminalCommand.title)}";
                
                if (!string.IsNullOrWhiteSpace(terminalCommand.description))
                    text += $"\n{terminalCommand.description}";

                text += "\n\n";
                categoryKeyword.specialKeywordResult.displayText = categoryKeyword.specialKeywordResult.displayText.Trim() + "\n" + text;
            }
        }
    }

    public class TerminalCommand
    {
        public string command;
        public Func<string[], TerminalResult> operation;
        public string category;
        public string title;
        public string description;

        public TerminalCommand(string command, Func<string[], TerminalResult> operation, string category = "", string title = "", string description = "")
        {
            this.command = command;
            this.operation = operation;
            this.category = category;
            this.title = title;
            this.description = description;
        }

        public TerminalCommand(string command, Func<string[], string> operation, string category = "", string title = "", string description = "")
        {
            this.command = command;

            this.operation = args =>
            {
                string output = operation(args);
                return new TerminalResult(output);
            };

            this.category = category;
            this.title = title;
            this.description = description;
        }
    }

    public class TerminalResult
    {
        public TerminalNode output;
        public TerminalCommand[] callbacks;

        public TerminalResult(string output)
        {
            this.output = new TerminalNode
            {
                clearPreviousText = true,
                displayText = output + "\n\n"
            };
            this.callbacks = [];
        }

        public TerminalResult(string output, TerminalCommand[] callbacks)
        {
            this.output = new TerminalNode
            {
                clearPreviousText = true,
                displayText = output + "\n\n"
            };
            this.callbacks = callbacks;
        }

        public TerminalResult(TerminalNode output)
        {
            this.output = output;
            this.callbacks = [];
        }

        public TerminalResult(TerminalNode output, TerminalCommand[] callbacks)
        {
            this.output = output;
            this.callbacks = callbacks;
        }
    }

    [HarmonyPatch]
    internal static class TerminalAPIPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(Terminal), nameof(Terminal.ParsePlayerSentence))]
        public static bool Terminal_ParsePlayerSentence_PreFix(Terminal __instance, ref TerminalNode __result)
        {
            try
            {
                __instance.broadcastedCodeThisFrame = false;
                string s = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);
                s = __instance.RemovePunctuation(s);
                string[] array = s.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                TerminalResult? result = TerminalAPI.TryParseCommand(array);
                if (result == null) { return true; }
                __result = result.output;
                return false;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Terminal), nameof(Terminal.Start))]
        public static void Terminal_Start_PostFix(Terminal __instance)
        {
            try
            {
                TerminalAPI.SetCategories(__instance);
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
