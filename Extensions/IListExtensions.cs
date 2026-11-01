using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SnowyLib;

public static class IListExtensions
{
    public static IList<T> Shuffle<T>(this IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }

        return list;
    }

    public static IList<T> Shuffle<T>(this IList<T> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);

            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }

        return list;
    }

    public static IList<T> OrderByDistance<T>(this IList<T> list, Vector3 position, Func<T, Vector3> positionSelector)
    {
        return list.OrderBy(x => Vector3.Distance(position, positionSelector(x))).ToList();
    }
}