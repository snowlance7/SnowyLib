//using GameNetcodeStuff;
//using HarmonyLib;
//using UnityEngine;
//using UnityEngine.UI;
//using static SnowyLib.Plugin;

//namespace SnowyLib
//{
//    public class VignetteOverlay : MonoBehaviour // TODO: Figure this out later
//    {
//        public static VignetteOverlay Instance { get; private set; } = null!;

//        Image image = null!;
//        RectTransform rt = null!;

//        Texture2D? vignetteTexture;

//        public float intensityDecreasePerSecond { get; private set; } = 0.01f;

//        public float currentIntensity { get; private set; }

//        void Awake()
//        {
//            logger.LogWarning("VignetteOverlay Awake running");

//            gameObject.layer = LayerMask.NameToLayer("UI");

//            Canvas canvas = gameObject.AddComponent<Canvas>();
//            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//            //canvas.sortingOrder = 9999;
//            canvas.sortingOrder = 0;
//            canvas.pixelPerfect = true;

//            CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
//            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
//            gameObject.AddComponent<GraphicRaycaster>();

//            GameObject vignetteObj = new GameObject("Vignette");
//            vignetteObj.layer = LayerMask.NameToLayer("UI");
//            vignetteObj.transform.SetParent(gameObject.transform, false);
//            image = vignetteObj.AddComponent<Image>(); 
            
//            rt = vignetteObj.GetComponent<RectTransform>();

//            rt.anchorMin = Vector2.zero;
//            rt.anchorMax = Vector2.one;

//            rt.offsetMin = Vector2.zero;
//            rt.offsetMax = Vector2.zero;

//            image.raycastTarget = false;
//            //rt.localScale = Vector3.one;

//            vignetteTexture = Utils.LoadEmbeddedTexture("SnowyLib.Embedded.vignette.png");
//            if (vignetteTexture == null) { logger.LogError("vignetteTexture is null"); return; }
//            image.sprite = Sprite.Create(vignetteTexture, new Rect(0, 0, vignetteTexture.width, vignetteTexture.height), new Vector2(0.5f, 0.5f));
//        }

//        void Update()
//        {
//            if (currentIntensity <= 0f) return;

//            currentIntensity = Mathf.Max(
//                0f,
//                currentIntensity - intensityDecreasePerSecond * Time.deltaTime);

//            Apply(currentIntensity);
//        }

//        public void SetIntensity(float intensity, float intensityDecreasePerSecond = 0.01f)
//        {
//            this.intensityDecreasePerSecond = intensityDecreasePerSecond;
//            currentIntensity = Mathf.Clamp01(intensity);
//            logger.LogDebug("Setting intensity");
//            Apply(currentIntensity);
//        }

//        void Apply(float t)
//        {
//            image.color = new Color(1f, 1f, 1f, t);

//            float scale = Mathf.Lerp(1f, 2.5f, t);
//            rt.localScale = Vector3.one * scale;
//        }

//        internal static void Init()
//        {
//            logger.LogDebug("Initiating VignetteOverlay");
//            Instance = Instantiate(new GameObject("VignetteOverlay")).AddComponent<VignetteOverlay>();
//        }
//    }

//    [HarmonyPatch]
//    internal class VignetteOverlayPatches
//    {
//        [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
//        public static void ConnectClientToPlayerObjectPostfix()
//        {
//            try
//            {
//                //VignetteOverlay.Init();
//            }
//            catch
//            {
//                return;
//            }
//        }
//    }
//}
