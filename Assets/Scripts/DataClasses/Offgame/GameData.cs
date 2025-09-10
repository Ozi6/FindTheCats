using UnityEngine;

[System.Serializable]
public class GameData
{
    [Header("Player Progress")]
    public int currentPlanetIndex = 0;
    public int totalCatsFound = 0;
    public float totalPlayTime = 0f;

    [Header("Settings")]
    public float masterVolume = 1f;
    public float sfxVolume = 1f;
    public float musicVolume = 1f;

    public GameData()
    {

    }
}