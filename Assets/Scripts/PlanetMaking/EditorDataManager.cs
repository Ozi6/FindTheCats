using Dreamteck.Splines;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EditorDataManager
{
    private PlanetEditor editor;

    public EditorDataManager(PlanetEditor editor)
    {
        this.editor = editor;
    }

    public void SaveToPlanetData(List<GameObject> placedObjects)
    {
        editor.planet.planetData.planetRadius = editor.planet.radius;
        editor.planet.planetData.placedObjectsToLoad.Clear();
        editor.planet.planetData.placedCatsToLoad.Clear();
        editor.planet.planetData.placedCattedToLoad.Clear();
        editor.planet.planetData.placedSplinesToLoad.Clear();

        List<SplineComputer> allSplines = editor.planet.GetComponentsInChildren<SplineComputer>().ToList();

        foreach (var spline in allSplines)
        {
            PlacedSplineData splineData = new PlacedSplineData();
            splineData.isClosed = spline.isClosed;
            splineData.points = spline.GetPoints().Select(p => new SplinePointData
            {
                position = p.position,
                normal = p.normal,
                size = p.size,
                color = p.color
            }).ToList();
            editor.planet.planetData.placedSplinesToLoad.Add(splineData);
        }

        foreach (var instance in placedObjects)
        {
            if (instance == null) continue;
            var epi = instance.GetComponent<EditorPlacedItem>();
            if (epi == null) continue;

            var localPos = instance.transform.localPosition;
            var localRot = instance.transform.localRotation;

            if (instance.GetComponent<CattedObject>())
            {
                var data = new PlacedCattedData
                {
                    containerPrefab = epi.originalPrefab,
                    catPrefab = epi.originalCatPrefab,
                    localPosition = localPos,
                    localRotation = localRot
                };
                editor.planet.planetData.placedCattedToLoad.Add(data);
            }
            else if (instance.GetComponent<Cat>())
            {
                var data = new PlacedCatData
                {
                    catPrefab = epi.originalPrefab,
                    localPosition = localPos,
                    localRotation = localRot
                };
                editor.planet.planetData.placedCatsToLoad.Add(data);
            }
            else
            {
                var data = new PlacedObjectData
                {
                    prefab = epi.originalPrefab,
                    localPosition = localPos,
                    localRotation = localRot,
                    assignedSplineIndex = -1
                };
                WalkingPerson wp = instance.GetComponent<WalkingPerson>();
                if (wp != null && wp.assignedSpline != null)
                {
                    int index = allSplines.IndexOf(wp.assignedSpline);
                    if (index >= 0)
                    {
                        data.assignedSplineIndex = index;
                    }
                }
                editor.planet.planetData.placedObjectsToLoad.Add(data);
            }
        }
    }

    public void LoadFromPlanetData(ref List<GameObject> placedObjects)
    {
        foreach (var obj in placedObjects.ToArray())
            if (obj != null)
                Object.Destroy(obj);
        placedObjects.Clear();

        editor.planet.radius = editor.planet.planetData.planetRadius;
        editor.planet.meshBuilder.SetupPlanetMesh();
        editor.planetSizeSlider.value = editor.planet.radius;
        editor.UpdatePlanetSizeText(editor.planet.radius);

        List<SplineComputer> loadedSplines = new List<SplineComputer>();
        foreach (var splineData in editor.planet.planetData.placedSplinesToLoad)
        {
            GameObject splineObj = new GameObject("PlanetSpline");
            splineObj.transform.SetParent(editor.planet.transform);
            SplineComputer sc = splineObj.AddComponent<SplineComputer>();
            sc.type = Spline.Type.Linear;
            SplinePoint[] points = splineData.points.Select(d => new SplinePoint
            {
                position = d.position,
                normal = d.normal,
                size = d.size,
                color = d.color
            }).ToArray();
            sc.SetPoints(points);
            if (splineData.isClosed)
                sc.Close();
            SplineRenderer renderer = splineObj.AddComponent<SplineRenderer>();
            renderer.spline = sc;
            renderer.size = 0.2f;
            loadedSplines.Add(sc);
        }

        foreach (var data in editor.planet.planetData.placedObjectsToLoad)
        {
            var instance = Object.Instantiate(data.prefab, data.localPosition, data.localRotation, editor.planet.transform);
            var po = instance.GetComponent<PlanetObject>() ?? instance.AddComponent<PlanetObject>();
            po.Initialize(editor.planet);
            var epi = instance.AddComponent<EditorPlacedItem>();
            epi.originalPrefab = data.prefab;
            if (data.assignedSplineIndex >= 0 && data.assignedSplineIndex < loadedSplines.Count)
            {
                WalkingPerson wp = instance.GetComponent<WalkingPerson>();
                if (wp != null)
                {
                    wp.assignedSpline = loadedSplines[data.assignedSplineIndex];
                }
            }
            placedObjects.Add(instance);
        }

        foreach (var data in editor.planet.planetData.placedCatsToLoad)
        {
            var instance = Object.Instantiate(data.catPrefab, data.localPosition, data.localRotation, editor.planet.transform);
            var cat = instance.GetComponent<Cat>() ?? instance.AddComponent<Cat>();
            cat.Initialize(editor.planet);
            var epi = instance.AddComponent<EditorPlacedItem>();
            epi.originalPrefab = data.catPrefab;
            placedObjects.Add(instance);
        }

        foreach (var data in editor.planet.planetData.placedCattedToLoad)
        {
            var instance = Object.Instantiate(data.containerPrefab, data.localPosition, data.localRotation, editor.planet.transform);
            var co = instance.GetComponent<CattedObject>() ?? instance.AddComponent<CattedObject>();
            AttachCat(co, data.catPrefab);
            co.Initialize(editor.planet);
            var epi = instance.AddComponent<EditorPlacedItem>();
            epi.originalPrefab = data.containerPrefab;
            epi.originalCatPrefab = data.catPrefab;
            placedObjects.Add(instance);
        }
    }

    private void AttachCat(CattedObject obj, GameObject catPrefab)
    {
        if (catPrefab == null) return;
        var catObj = Object.Instantiate(catPrefab);
        var cat = catObj.GetComponent<Cat>() ?? catObj.AddComponent<Cat>();
        obj.associatedCat = cat;
        catObj.transform.SetParent(obj.transform);
        catObj.transform.localPosition = Vector3.zero;
        catObj.transform.localRotation = Quaternion.identity;
        cat.Initialize(editor.planet);
    }
}