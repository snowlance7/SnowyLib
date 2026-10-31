using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using static SnowyLib.Plugin;

namespace SnowyLib;

public class NightVision : MonoBehaviour
{
    internal static NightVision Instance { get; private set; } = null!;

    Light FarLight = null!;
    Light NearLight = null!;

    public static bool Enabled => Intensity > 0;
    public static float Intensity { get; private set; } = 0f;

    public static void Init() // TODO: Figure out why it doesnt work the first time
    {
        Instance ??= new GameObject("NightVision").AddComponent<NightVision>();
        DontDestroyOnLoad(Instance.gameObject);
    }

    private void Awake()
    {
        Transform mapCamera = GameObject.Find("MapCamera").transform;
        transform.SetParent(localPlayer.gameplayCamera.transform);
        NearLight = new GameObject("Near").AddComponent<Light>();
        NearLight.transform.SetParent(transform);
        NearLight.transform.position = mapCamera.position + Vector3.down * 80f;
        NearLight.range = 70f;
        NearLight.color = new Color(0.875f, 0.788f, 0.791f, 1f);
        FarLight = new GameObject("Far").AddComponent<Light>();
        FarLight.transform.SetParent(transform);
        FarLight.transform.position = mapCamera.position + Vector3.down * 30f;
        FarLight.range = 500f;
        NearLight.intensity = 0f;
        FarLight.intensity = 0f;
    }

    public static void Toggle()
    {
        Enable(!Enabled);
    }

    public static void Enable(bool value)
    {
        SetIntensity(value ? 1 : 0);
    }

    public static void SetIntensity(float intensity)
    {
        Intensity = Mathf.Clamp01(intensity);
        logger.LogDebug("NightVision Intensity set to " + Intensity);
        Instance.NearLight.intensity = 10000 * Intensity;
        Instance.FarLight.intensity = 110000 * Intensity;
    }
}

[HarmonyPatch]
internal static class NightVisionPatches
{
    [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
    public static void ConnectClientToPlayerObjectPostfix(PlayerControllerB __instance)
    {
        try
        {
            NightVision.Init();
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
            return;
        }
    }
}