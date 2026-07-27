using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public class VignetteOverlay : MonoBehaviour
    {
        public static VignetteOverlay? Instance { get; private set; }

        [SerializeField] Image image = null!;

        Material material = null!;

        static readonly int InsetId = Shader.PropertyToID("_Inset");

        public static float currentIntensityDecreasePerSecond { get; private set; } = 0.01f;

        public static float currentIntensity { get; private set; }

        void Awake()
        {
            material = image.material;
            image.canvas.overrideSorting = true;
            image.canvas.sortingOrder = -1;
        }

        void Update()
        {
            if (currentIntensity <= 0f) return;

            currentIntensity = Mathf.Max(0f,
                currentIntensity - currentIntensityDecreasePerSecond * Time.deltaTime);

            material.SetFloat(InsetId, currentIntensity);
        }

        public static void SetIntensity(float intensity, float intensityDecreasePerSecond = 0.01f)
        {
            currentIntensityDecreasePerSecond = intensityDecreasePerSecond;
            currentIntensity = Mathf.Clamp01(intensity);
            Instance?.material.SetFloat(InsetId, currentIntensity);
        }

        internal static void Init(PlayerControllerB player)
        {
            GameObject prefab = ModAssets.LoadAsset<GameObject>("Assets/ModAssets/VignetteOverlay.prefab");
            Instance = Instantiate(prefab).GetComponent<VignetteOverlay>();
        }
    }

    [HarmonyPatch]
    internal static class VignetteOverlayPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        public static void ConnectClientToPlayerObjectPostfix(PlayerControllerB __instance)
        {
            try
            {
                VignetteOverlay.Init(__instance);
            }
            catch
            {
                return;
            }
        }
    }
}
