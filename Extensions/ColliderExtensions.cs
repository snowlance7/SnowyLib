using Dawn.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SnowyLib;

public static class ColliderExtensions
{
    public static Vector3 GetRandomPointInsideBounds(this Collider collider)
    {
        Bounds bounds = collider.bounds;

        for (int i = 0; i < 100; i++)
        {
            Vector3 point = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );

            if (collider.ClosestPoint(point) == point)
                return point;
        }

        return collider.bounds.center;
    }

    public static Vector3 GetRandomPointInsideBounds(this Collider collider, System.Random random)
    {
        Bounds bounds = collider.bounds;

        for (int i = 0; i < 100; i++)
        {
            Vector3 point = new Vector3(
                random.NextFloat(bounds.min.x, bounds.max.x),
                random.NextFloat(bounds.min.y, bounds.max.y),
                random.NextFloat(bounds.min.z, bounds.max.z)
            );

            if (collider.ClosestPoint(point) == point)
                return point;
        }

        return collider.bounds.center;
    }
}