using System.Linq;
using UnityEngine;
using static SnowyLib.Plugin;

namespace SnowyLib;

public static partial class Utils
{
    internal static void OnFinishGeneratingLevelPatches()
    {
        elevator = null;
        entrances.Clear();

        entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>().ToList();
        elevator = UnityEngine.Object.FindObjectOfType<MineshaftElevatorController>();

        currentLevelTrees = GameObject.FindGameObjectsWithTag("Tree");
        logger.LogDebug($"{Utils.currentLevelTrees.Count()} trees found!");

        SetRandoms();

        OnFinishGeneratingLevel.Invoke();
    }
}