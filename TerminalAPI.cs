using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public static class TerminalAPI
    {
        internal static List<TerminalCommand> registeredTerminalCommands = new List<TerminalCommand>();
        internal static TerminalNode? TryParseCommand(string[] args)
        {
            TerminalCommand? terminalCommand = registeredTerminalCommands.Where(x => x.command == args[0]).FirstOrDefault();
            if (terminalCommand == null) { return null; }

            TerminalNode? node = terminalCommand.operation.Invoke(args);
            return node;
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
            TerminalCommand? terminalCommand = registeredTerminalCommands.Where(x => x.command == command).FirstOrDefault();
            if (terminalCommand == null) { return; }
            registeredTerminalCommands.Remove(terminalCommand);
        }
    }

    public class TerminalCommand
    {
        public string command;
        public Func<string[], TerminalNode> operation;

        public TerminalCommand(string command, Func<string[], TerminalNode> operation)
        {
            this.command = command;
            this.operation = operation;
        }

        public TerminalCommand(string command, Func<string[], string> operation)
        {
            this.command = command;

            this.operation = args =>
            {
                string output = operation(args);

                TerminalNode node = new TerminalNode
                {
                    clearPreviousText = true,
                    displayText = output
                };

                return node;
            };
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
                TerminalNode? result = TerminalAPI.TryParseCommand(array);
                if (result == null) { return true; }
                __result = result;
                return false;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return true;
            }
        }
    }
}
