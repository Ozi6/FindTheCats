using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Planet Database", menuName = "Planet Creation/Planet Database")]
public class PlanetDatabase : ScriptableObject
{
    [Header("Planet Configuration")]
    public List<PlanetData> planets = new List<PlanetData>();

    [Header("Progress Tracking")]
    public List<PlanetProgress> planetProgress = new List<PlanetProgress>();

    public int GetLatestUnfinishedPlanetIndex(int startFromIndex = 0)
    {
        for (int i = startFromIndex; i < planets.Count; i++)
        {
            var progress = GetPlanetProgress(i);
            if (!progress.isCompleted)
                return i;
        }
        return Mathf.Clamp(startFromIndex, 0, planets.Count - 1);
    }

    public PlanetData GetLatestUnfinishedPlanet(int startFromIndex = 0)
    {
        int index = GetLatestUnfinishedPlanetIndex(startFromIndex);
        return planets.Count > 0 ? planets[index] : null;
    }

    public PlanetProgress GetPlanetProgress(int planetIndex)
    {
        while (planetProgress.Count <= planetIndex)
            planetProgress.Add(new PlanetProgress());
        return planetProgress[planetIndex];
    }

    public void MarkPlanetCompleted(int planetIndex, float completionTime = 0f)
    {
        var progress = GetPlanetProgress(planetIndex);
        progress.isCompleted = true;
        progress.completionTime = completionTime;
        progress.lastPlayedTime = System.DateTime.Now.ToBinary();
    }

    public void MarkPlanetStarted(int planetIndex)
    {
        var progress = GetPlanetProgress(planetIndex);
        progress.hasStarted = true;
        progress.lastPlayedTime = System.DateTime.Now.ToBinary();
    }
}