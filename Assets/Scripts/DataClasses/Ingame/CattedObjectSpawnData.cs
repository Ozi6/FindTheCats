using UnityEngine;

[System.Serializable]
public class CattedObjectSpawnData
{
    public GameObject containerPrefab;
    public GameObject catPrefab;
    public int count = 1;
    public float minDistanceFromObjects = 1f;
}