using UnityEngine;

public class PlanetMeshBuilder
{
    private readonly GameObject planet;
    private readonly PlanetData data;
    private readonly float radius;

    public PlanetMeshBuilder(GameObject planet, PlanetData data, float radius)
    {
        this.planet = planet;
        this.data = data;
        this.radius = radius;
    }

    public void SetupPlanetMesh()
    {
        var meshFilter = GetOrAdd<MeshFilter>();
        var renderer = GetOrAdd<MeshRenderer>();

        if (meshFilter.sharedMesh == null)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            meshFilter.mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(sphere);
        }

        planet.transform.localScale = Vector3.one * radius * 2f;

        if (data.planetMaterial != null)
            renderer.material = data.planetMaterial;

        var collider = GetOrAdd<SphereCollider>();
        collider.radius = 0.5f;
        collider.isTrigger = false;
        if (planet.layer == 0)
            planet.layer = LayerMask.NameToLayer("Default"); 
    }

    private T GetOrAdd<T>() where T : Component =>
        planet.GetComponent<T>() ?? planet.AddComponent<T>();
}