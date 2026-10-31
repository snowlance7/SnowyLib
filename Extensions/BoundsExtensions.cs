using Dawn.Utils;
using UnityEngine;

namespace SnowyLib;

public static class BoundsExtensions
{
    public static Vector3 GetRandomPointInsideBounds(this Bounds bounds)
    {
        for (int i = 0; i < 100; i++)
        {
            Vector3 point = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );

            if (bounds.ClosestPoint(point) == point)
                return point;
        }

        return bounds.center;
    }

    public static Vector3 GetRandomPointInsideBounds(this Bounds bounds, System.Random random)
    {
        for (int i = 0; i < 100; i++)
        {
            Vector3 point = new Vector3(
                random.NextFloat(bounds.min.x, bounds.max.x),
                random.NextFloat(bounds.min.y, bounds.max.y),
                random.NextFloat(bounds.min.z, bounds.max.z)
            );

            if (bounds.ClosestPoint(point) == point)
                return point;
        }

        return bounds.center;
    }
}