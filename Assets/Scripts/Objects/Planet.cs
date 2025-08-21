using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Planet : Singleton<Planet>
{
    protected override bool Persistent => false;

    [Header("Planet Settings")]
    public PlanetData planetData;
    public float radius = 5f;

    private PlanetMeshBuilder meshBuilder;
    private PlanetSpawner spawner;

    private readonly List<PlanetObject> spawnedObjects = new();
    private readonly List<Cat> spawnedCats = new();
    private readonly List<CattedObject> spawnedCattedObjects = new();

    void Start()
    {

    }

    public void GeneratePlanet()
    {
        ClearPlanet();
        radius = planetData.planetRadius;
        transform.localScale = Vector3.one;

        meshBuilder = new PlanetMeshBuilder(gameObject, planetData, radius);
        spawner = new PlanetSpawner(this, radius, spawnedObjects, spawnedCats);

        meshBuilder.SetupPlanetMesh();
        GenerateFromData();
    }

    void ClearPlanet()
    {
        foreach (var comp in spawnedObjects.Cast<Component>().Concat(spawnedCats))
            if (comp != null) DestroyImmediate(comp.gameObject);

        spawnedObjects.Clear();
        spawnedCats.Clear();
        spawnedCattedObjects.Clear();
    }

    public void GenerateFromData()
    {
        foreach (var obj in planetData.objectsToSpawn)
            spawner.SpawnObjects(obj);

        foreach (var catted in planetData.cattedObjectsToSpawn)
            spawner.SpawnSpecial<CattedObject>(
                catted.containerPrefab,
                catted.catPrefab,
                catted.count,
                catted.minDistanceFromObjects,
                spawnedCattedObjects);

        foreach (var cat in planetData.catsToSpawn)
            spawner.SpawnCats(cat);
    }

    public List<Cat> GetAllCats() =>
        spawnedCats.Concat(spawnedCattedObjects.Select(c => c.associatedCat).Where(c => c != null)).ToList();
}