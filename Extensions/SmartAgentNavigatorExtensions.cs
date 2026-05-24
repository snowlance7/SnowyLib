using Dawn.Utils;
using UnityEngine;

namespace SnowyLib
{
    public static class SmartAgentNavigatorExtensions
    {
        public static bool SmartCanPathToPoint(this SmartAgentNavigator nav, Vector3 pos)
        {
            return Utils.SmartCanPathToPoint(nav.agent.transform.position, pos, nav.IsAgentOutside());
        }

        public static bool SmartCanPathToPoint(this SmartAgentNavigator nav, Vector3 startPos, Vector3 endPos)
        {
            return Utils.SmartCanPathToPoint(startPos, endPos, nav.IsAgentOutside());
        }

        public static bool CanPathToPoint(this SmartAgentNavigator nav, Vector3 pos)
        {
            return nav.CanPathToPoint(nav.agent.transform.position, pos) > 0;
        }
    }
}
