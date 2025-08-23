using System.Collections.Generic;
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
                    localRotation = localRot
                };
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

        foreach (var data in editor.planet.planetData.placedObjectsToLoad)
        {
            var instance = Object.Instantiate(data.prefab, data.localPosition, data.localRotation, editor.planet.transform);
            var po = instance.GetComponent<PlanetObject>() ?? instance.AddComponent<PlanetObject>();
            po.Initialize(editor.planet);
            var epi = instance.AddComponent<EditorPlacedItem>();
            epi.originalPrefab = data.prefab;
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