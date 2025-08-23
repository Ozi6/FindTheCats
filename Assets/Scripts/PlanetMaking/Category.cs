using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Category
{
    public string name;
    public List<GameObject> prefabs = new List<GameObject>();
    public GameObject associatedCatPrefab;
}