using UnityEngine;

[System.Serializable]
public class CatSpawnData
{
    public GameObject catPrefab;
    public int count = 1;
    public float minDistanceFromObjects = 1f;
    public float minDistanceFromOtherCats = 0.5f;
}