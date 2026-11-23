using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace SnowyLib;

public static class SkinnedMeshRendererExtensions
{
    public static Transform GetDominantBone(SkinnedMeshRenderer renderer, RaycastHit hit) // TODO: Try to understand how this works more when im medicated
    {
        Mesh mesh = renderer.sharedMesh;

        if (mesh == null)
            return null;

        int[] triangles = mesh.triangles;
        int triangleStart = hit.triangleIndex * 3;

        if (triangleStart < 0 || triangleStart + 2 >= triangles.Length)
            return null;

        int[] vertices =
        {
            triangles[triangleStart],
            triangles[triangleStart + 1],
            triangles[triangleStart + 2]
        };

        Vector3 barycentric = hit.barycentricCoordinate;

        float[] vertexContributions =
        {
            barycentric.x,
            barycentric.y,
            barycentric.z
        };

        NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex();
        NativeArray<BoneWeight1> allBoneWeights = mesh.GetAllBoneWeights();

        try
        {
            Dictionary<int, float> boneInfluences = new();

            for (int v = 0; v < 3; v++)
            {
                int vertexIndex = vertices[v];

                if (vertexIndex >= bonesPerVertex.Length)
                    continue;

                int weightStart = 0;

                for (int i = 0; i < vertexIndex; i++)
                    weightStart += bonesPerVertex[i];

                int weightCount = bonesPerVertex[vertexIndex];

                for (int i = 0; i < weightCount; i++)
                {
                    BoneWeight1 weight = allBoneWeights[weightStart + i];

                    float contribution = weight.weight * vertexContributions[v];

                    if (boneInfluences.ContainsKey(weight.boneIndex))
                    {
                        boneInfluences[weight.boneIndex] += contribution;
                    }
                    else
                    {
                        boneInfluences.Add(weight.boneIndex, contribution);
                    }
                }
            }

            int dominantBoneIndex = -1;
            float highestInfluence = 0f;

            foreach (var influence in boneInfluences)
            {
                if (influence.Value > highestInfluence)
                {
                    highestInfluence = influence.Value;
                    dominantBoneIndex = influence.Key;
                }
            }

            if (dominantBoneIndex < 0 || dominantBoneIndex >= renderer.bones.Length)
                return null;

            return renderer.bones[dominantBoneIndex];
        }
        finally
        {
            bonesPerVertex.Dispose();
            allBoneWeights.Dispose();
        }
    }
}