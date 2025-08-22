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
}