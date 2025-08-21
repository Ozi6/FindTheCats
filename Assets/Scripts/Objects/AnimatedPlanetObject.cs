using UnityEngine;

public abstract class AnimatedPlanetObject : PlanetObject
{
    [Header("Animation Settings")]
    public bool isAnimated = true;
    public float animationSpeed = 1f;

    protected virtual void Update()
    {
        if (isAnimated)
            UpdateAnimation();
    }

    protected abstract void UpdateAnimation();
}