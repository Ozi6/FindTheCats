using UnityEngine;

[System.Serializable]
public class ObjectSpawnData
{
    public GameObject prefab;
    public int minCount = 1;
    public int maxCount = 3;
    public float minDistanceFromOthers = 1f;
    public bool canSpawnNearCats = false;
}