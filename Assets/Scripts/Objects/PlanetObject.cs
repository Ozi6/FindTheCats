using UnityEngine;

public class PlanetObject : MonoBehaviour
{
    [Header("Planet Object Settings")]
    public float radiusFromCenter = 1f;
    public bool blocksCats = true;
    public float blockingRadius = 0.5f;

    protected Planet parentPlanet;
    private Vector3 originalScale;

    public virtual void Initialize(Planet planet)
    {
        parentPlanet = planet;
        originalScale = transform.localScale;
        transform.localScale = originalScale;
    }

    protected virtual void PositionOnPlanet()
    {
        if (parentPlanet != null)
        {
            Vector3 direction = transform.localPosition.normalized;
            float surfaceDistance = 0.5f + (radiusFromCenter / parentPlanet.radius);
            transform.localPosition = direction * surfaceDistance;
            transform.up = transform.position.normalized;
        }
    }

    public virtual bool CanPlaceAtPosition(Vector3 worldPosition, float minDistance = 1f)
    {
        if (!blocksCats) return true;
        return Vector3.Distance(transform.position, worldPosition) >= minDistance;
    }
}