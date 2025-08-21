using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlanetSpawner
{
    private readonly Planet planet;
    private readonly float radius;
    private readonly List<PlanetObject> spawnedObjects;
    private readonly List<Cat> spawnedCats;

    public PlanetSpawner(Planet planet, float radius, List<PlanetObject> objects, List<Cat> cats)
    {
        this.planet = planet;
        this.radius = radius;
        spawnedObjects = objects;
        spawnedCats = cats;
    }

    public void SpawnObjects(ObjectSpawnData data)
    {
        int count = Random.Range(data.minCount, data.maxCount + 1);
        for (int i = 0; i < count; i++)
        {
            if (!TrySpawn(data.prefab, data.minDistanceFromOthers, spawnedObjects, out var obj)) continue;
            var planetObj = obj.GetComponent<PlanetObject>() ?? obj.AddComponent<PlanetObject>();
            planetObj.Initialize(planet);
            spawnedObjects.Add(planetObj);
        }
    }

    public void SpawnSpecial<T>(
        GameObject containerPrefab,
        GameObject catPrefab,
        int count,
        float minDistance,
        List<T> list) where T : PlanetObject
    {
        for (int i = 0; i < count; i++)
        {
            if (!TrySpawn(containerPrefab, minDistance, spawnedObjects, out var obj)) continue;

            var special = obj.GetComponent<T>() ?? obj.AddComponent<T>();
            AttachCat(special, catPrefab);
            special.Initialize(planet);
            list.Add(special);
            spawnedObjects.Add(special);
        }
    }

    public void SpawnCats(CatSpawnData data)
    {
        for (int i = 0; i < data.count; i++)
        {
            if (!TrySpawnWithRetries(data, out var obj)) continue;
            var cat = obj.GetComponent<Cat>() ?? obj.AddComponent<Cat>();
            cat.Initialize(planet);
            spawnedCats.Add(cat);
        }
    }

    private void AttachCat(PlanetObject obj, GameObject catPrefab)
    {
        if (catPrefab == null) return;
        var catObj = Object.Instantiate(catPrefab);
        var cat = catObj.GetComponent<Cat>() ?? catObj.AddComponent<Cat>();
        if (obj is CattedObject c) c.associatedCat = cat;
        if (obj is CatHidingObject h) h.hiddenCat = cat;
    }

    private bool TrySpawn(GameObject prefab, float minDistance, IEnumerable<PlanetObject> blockers, out GameObject spawned)
    {
        var pos = Random.onUnitSphere * radius;
        if (blockers.All(o => o == null || Vector3.Distance(pos, o.transform.position) >= minDistance))
        {
            spawned = Object.Instantiate(prefab, pos, Quaternion.identity, planet.transform);
            return true;
        }
        spawned = null;
        return false;
    }

    private bool TrySpawnWithRetries(CatSpawnData data, out GameObject spawned)
    {
        for (int attempts = 0; attempts < 5; attempts++)
        {
            var pos = Random.onUnitSphere * radius;
            bool valid =
                spawnedObjects.Where(o => o.blocksCats)
                    .All(o => o == null || Vector3.Distance(pos, o.transform.position) >= data.minDistanceFromObjects) &&
                spawnedCats.All(c => Vector3.Distance(pos, c.transform.position) >= data.minDistanceFromOtherCats);

            if (valid)
            {
                spawned = Object.Instantiate(data.catPrefab, pos, Quaternion.identity, planet.transform);
                return true;
            }
        }
        spawned = null;
        return false;
    }
}