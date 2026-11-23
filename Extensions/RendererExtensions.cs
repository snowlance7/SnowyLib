using UnityEngine;

namespace SnowyLib;

public static class RendererExtensions
{
    public static bool TryGetMeshHit(this Renderer renderer, Ray ray, out RaycastHit hitInfo, float maxDistance) // TODO: USE THIS IN ATTACHABLEOBJECT?
    {
        hitInfo = default;
        Mesh mesh = new Mesh();

        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            skinnedMeshRenderer.BakeMesh(mesh);
        }
        else if (renderer is MeshRenderer)
        {
            MeshFilter? meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null) { return false; }
            mesh = meshFilter.sharedMesh;
        }

        GameObject meshObject = new GameObject("TemporaryMesh");
        meshObject.transform.SetParent(renderer.transform, false);

        MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        bool hit = meshCollider.Raycast(ray, out hitInfo, maxDistance);

        meshCollider.sharedMesh = null;
        GameObject.Destroy(meshObject);
        GameObject.Destroy(mesh);

        return hit;
    }

    public static bool TryGetMeshHit(this Renderer renderer, Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
    {
        return TryGetMeshHit(renderer, new Ray(origin, direction), out hitInfo, maxDistance);
    }
}