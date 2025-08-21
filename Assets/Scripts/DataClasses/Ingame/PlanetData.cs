using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Planet Data", menuName = "Planet Creation/Planet Data")]
public class PlanetData : ScriptableObject
{
    [Header("Planet Properties")]
    public float planetRadius = 30f;
    public Material planetMaterial;
    public bool hasSplineRoads = false;

    [Header("Object Spawning")]
    public List<ObjectSpawnData> objectsToSpawn = new List<ObjectSpawnData>();
    public List<CatSpawnData> catsToSpawn = new List<CatSpawnData>();
    public List<CattedObjectSpawnData> cattedObjectsToSpawn = new List<CattedObjectSpawnData>();
}