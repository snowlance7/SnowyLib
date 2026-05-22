using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SnowyLib
{
    public static class GameObjectExtensions
    {
        public static StatusEffectController StatusEffectController(this GameObject gameObject)
        {
            return gameObject.TryGetComponent(out StatusEffectController controller) ? controller : gameObject.AddComponent<StatusEffectController>();
        }
    }
}
