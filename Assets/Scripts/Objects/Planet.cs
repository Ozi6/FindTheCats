using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;

public class Planet : Singleton<Planet>
{
    protected override bool Persistent => false;

    [SerializeField] private bool editorMode = false;

    [Header("Planet Settings")]
    public PlanetData planetData;
    public float radius = 5f;
    public PlanetMeshBuilder meshBuilder;

    public readonly List<PlanetObject> spawnedObjects = new();
    public readonly List<Cat> spawnedCats = new();
    public readonly List<CattedObject> spawnedCattedObjects = new();

    void Start()
    {
        if (editorMode)
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
            if (comp != null)
                DestroyImmediate(comp.gameObject);

        foreach (var spline in GetComponentsInChildren<SplineComputer>())
            if (spline != null)
                DestroyImmediate(spline.gameObject);

        spawnedObjects.Clear();
        spawnedCats.Clear();
        spawnedCattedObjects.Clear();
    }

    public void GenerateFromData()
    {
        Vector3 planetScale = transform.localScale;

        List<SplineComputer> loadedSplines = new List<SplineComputer>();

        foreach (var splineData in planetData.placedSplinesToLoad)
        {
            GameObject splineObj = new GameObject("PlanetSpline");
            splineObj.transform.SetParent(transform);
            SplineComputer sc = splineObj.AddComponent<SplineComputer>();
            sc.type = Spline.Type.Bezier;
            SplinePoint[] points = splineData.points.Select(d => new SplinePoint
            {
                position = d.position,
                normal = d.normal,
                size = d.size,
                tangent = d.tangent,
                tangent2 = d.tangent2,
                color = d.color
            }).ToArray();
            sc.SetPoints(points);
            if (splineData.isClosed)
                sc.Close();
            sc.RebuildImmediate();
            SplineRenderer renderer = splineObj.AddComponent<SplineRenderer>();
            renderer.spline = sc;
            renderer.size = 0.2f;
            loadedSplines.Add(sc);
        }

        foreach (var data in planetData.placedObjectsToLoad)
        {
            Vector3 scaledPosition = ScalePosition(data.localPosition, planetScale);
            var obj = Object.Instantiate(data.prefab, scaledPosition, data.localRotation, transform);
            var planetObj = obj.GetComponent<PlanetObject>() ?? obj.AddComponent<PlanetObject>();
            planetObj.transform.localScale = ScaleDownObj(planetObj.transform.localScale);
            if (data.assignedSplineIndex >= 0 && data.assignedSplineIndex < loadedSplines.Count)
            {
                var wp = obj.GetComponent<WalkingPerson>();
                if (wp != null)
                    wp.assignedSpline = loadedSplines[data.assignedSplineIndex];
            }
            planetObj.Initialize(this);
            spawnedObjects.Add(planetObj);
        }

        foreach (var data in planetData.placedCatsToLoad)
        {
            Vector3 scaledPosition = ScalePosition(data.localPosition, planetScale);
            var obj = Object.Instantiate(data.catPrefab, scaledPosition, data.localRotation, transform);
            var cat = obj.GetComponent<Cat>() ?? obj.AddComponent<Cat>();
            cat.transform.localScale = ScaleDownObj(cat.transform.localScale);
            cat.Initialize(this);
            spawnedCats.Add(cat);
        }

        foreach (var data in planetData.placedCattedToLoad)
        {
            Vector3 scaledPosition = ScalePosition(data.localPosition, planetScale);
            var obj = Object.Instantiate(data.containerPrefab, scaledPosition, data.localRotation, transform);
            var special = obj.GetComponent<CattedObject>() ?? obj.AddComponent<CattedObject>();
            AttachCat(special, data.catPrefab);
            special.transform.localScale = ScaleDownObj(special.transform.localScale);
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

    public List<Cat> GetAllCats() => spawnedCats.Concat(spawnedCattedObjects.Select(c => c.associatedCat).Where(c => c != null)).ToList();

    public Vector3 ScaleDownObj(Vector3 firstScale)
    {
        return new Vector3(
            firstScale.x / transform.localScale.x,
            firstScale.y / transform.localScale.y,
            firstScale.z / transform.localScale.z
        );
    }

    public Vector3 ScalePosition(Vector3 originalPosition, Vector3 scale)
    {
        return new Vector3(
            originalPosition.x * scale.x,
            originalPosition.y * scale.y,
            originalPosition.z * scale.z
        );
    }
}