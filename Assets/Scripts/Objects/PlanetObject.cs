using UnityEngine;

public abstract class PlanetObject : MonoBehaviour
{
    [Header("Planet Object Settings")]
    public float radiusFromCenter = 1f;
    public bool blocksCats = true;
    public float blockingRadius = 0.5f;

    //protected Planet parentPlanet;

    public virtual void Initialize(Planet planet)
    {
        //parentPlanet = planet;
        PositionOnPlanet();
    }

    protected virtual void PositionOnPlanet()
    {
        /*if (parentPlanet != null)
        {
            Vector3 direction = transform.position.normalized;
            transform.position = direction * (parentPlanet.radius + radiusFromCenter);
            transform.up = direction;
        }*/
    }

    public virtual bool CanPlaceAtPosition(Vector3 worldPosition, float minDistance = 1f)
    {
        if (!blocksCats) return true;
        return Vector3.Distance(transform.position, worldPosition) >= minDistance;
    }
}