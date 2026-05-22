using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SnowyLib
{
    public class PositionInfo(Vector3 position, bool isOutside)
    {
        public Vector3 position = position;
        public bool isOutside = isOutside;
    }
}
