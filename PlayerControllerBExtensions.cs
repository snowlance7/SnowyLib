using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnowyLib
{
    public static class PlayerControllerBExtensions
    {
        public static StatusEffectController StatusEffectController(this PlayerControllerB player)
        {
            return player.gameObject.TryGetComponent(out StatusEffectController controller) ? controller : player.gameObject.AddComponent<StatusEffectController>();
        }
    }
}
