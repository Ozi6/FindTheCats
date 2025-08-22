using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class Planet : Singleton<Planet>
{
    protected override bool Persistent => false;
    [Header("Planet Settings")]
    public PlanetData planetData;
    public float radius = 5f;
    public PlanetMeshBuilder meshBuilder;
    public readonly List<PlanetObject> spawnedObjects = new();
    public readonly List<Cat> spawnedCats = new();
    public readonly List<CattedObject> spawnedCattedObjects = new();
    void Start()
    {
        GeneratePlanet();
    }
    public void GeneratePlanet()
    {
        ClearPlanet();
        radius = planetData.planetRadius;
        transform.localScale = Vector3.one;
        meshBuilder = new PlanetMeshBuilder(gameObject, planetData, radius);
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
        foreach (var data in planetData.placedObjectsToLoad)
        {
            var obj = Object.Instantiate(data.prefab, data.localPosition, data.localRotation, transform);
            var planetObj = obj.GetComponent<PlanetObject>() ?? obj.AddComponent<PlanetObject>();
            planetObj.Initialize(this);
            spawnedObjects.Add(planetObj);
        }
        foreach (var data in planetData.placedCatsToLoad)
        {
            var obj = Object.Instantiate(data.catPrefab, data.localPosition, data.localRotation, transform);
            var cat = obj.GetComponent<Cat>() ?? obj.AddComponent<Cat>();
            cat.Initialize(this);
            spawnedCats.Add(cat);
        }
        foreach (var data in planetData.placedCattedToLoad)
        {
            var obj = Object.Instantiate(data.containerPrefab, data.localPosition, data.localRotation, transform);
            var special = obj.GetComponent<CattedObject>() ?? obj.AddComponent<CattedObject>();
            AttachCat(special, data.catPrefab);
            special.Initialize(this);
            spawnedCattedObjects.Add(special);
            spawnedObjects.Add(special);
        }
    }
    private void AttachCat(CattedObject obj, GameObject catPrefab)
    {
        if (catPrefab == null) return;
        var catObj = Object.Instantiate(catPrefab);
        var cat = catObj.GetComponent<Cat>() ?? catObj.AddComponent<Cat>();
        if (obj is CattedObject c) c.associatedCat = cat;
        catObj.transform.SetParent(obj.transform);
        catObj.transform.localPosition = Vector3.zero;
        catObj.transform.localRotation = Quaternion.identity;
        cat.Initialize(this);
    }
    public List<Cat> GetAllCats() =>
        spawnedCats.Concat(spawnedCattedObjects.Select(c => c.associatedCat).Where(c => c != null)).ToList();
}