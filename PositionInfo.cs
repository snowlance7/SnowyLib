using UnityEngine;

namespace SnowyLib
{
    public class PositionInfo(Vector3 position, bool isOutside)
    {
        public Vector3 position = position;
        public bool isOutside = isOutside;
    }
}
