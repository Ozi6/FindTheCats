using UnityEngine;

public class WalkingPerson : AnimatedPlanetObject
{
    [Header("Walking Settings")]
    public float walkSpeed = 2f;
    public float walkRadius = 1f;
    public bool walkInCircles = true;

    private Vector3 spawnLocalPosition;
    private float walkAngle = 0f;

    public override void Initialize(Planet planet)
    {
        base.Initialize(planet);
        spawnLocalPosition = transform.localPosition;
        blocksCats = false;
        walkAngle = Random.Range(0f, 360f);
    }

    protected override void UpdateAnimation()
    {
        if (parentPlanet == null) return;

        if (walkInCircles)
        {
            walkAngle += walkSpeed * Time.deltaTime * 50f;
            Vector3 localUp = spawnLocalPosition.normalized;
            Vector3 localRight = Vector3.Cross(localUp, Vector3.forward).normalized;
            if (localRight.magnitude < 0.1f)
                localRight = Vector3.Cross(localUp, Vector3.right).normalized;
            Vector3 localForward = Vector3.Cross(localRight, localUp).normalized;
            Vector3 localOffset = (localRight * Mathf.Sin(walkAngle * Mathf.Deg2Rad) +
                                  localForward * Mathf.Cos(walkAngle * Mathf.Deg2Rad)) *
                                  (walkRadius / parentPlanet.radius);
            Vector3 newLocalPosition = (spawnLocalPosition + localOffset).normalized;
            float surfaceDistance = 0.5f + (radiusFromCenter / parentPlanet.radius);
            transform.localPosition = newLocalPosition * surfaceDistance;
            transform.up = transform.position.normalized;
            Vector3 worldRight = transform.TransformDirection(localRight);
            Vector3 walkDirection = Vector3.Cross(transform.up, worldRight).normalized;
            if (walkDirection != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(walkDirection, transform.up);
        }
        else
            transform.localPosition += transform.localPosition.normalized * Mathf.Sin(Time.time * animationSpeed * 2f) * 0.02f / parentPlanet.radius;
    }
}