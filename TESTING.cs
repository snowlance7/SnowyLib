using HarmonyLib;

namespace SnowyLib
{
    [HarmonyPatch]
    public static class TESTING
    {
        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            if (!Utils.testing) { return; }
        }

        [StaticUpdate]
        public static void Update()
        {
            if (!Utils.testing) { return; }
        }
    }
}