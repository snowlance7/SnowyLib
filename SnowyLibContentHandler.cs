using Dusk;
using UnityEngine;

namespace SnowyLib
{
    public class SnowyLibContentHandler : ContentHandler<SnowyLibContentHandler>
    {
        public class StatusEffectControllerAssets(DuskMod mod, string filePath) : AssetBundleLoader<StatusEffectControllerAssets>(mod, filePath)
        {
            [LoadFromBundle("StatusEffectController.prefab")]
            public GameObject StatusEffectControllerPrefab { get; private set; } = null!;
        }
        public StatusEffectControllerAssets? StatusEffectController;

        public SnowyLibContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("statuseffectcontroller", out StatusEffectController);
        }
    }
}