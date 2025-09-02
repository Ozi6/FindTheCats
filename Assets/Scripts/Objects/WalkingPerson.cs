using Dreamteck.Splines;
using UnityEngine;

public class WalkingPerson : AnimatedPlanetObject
{
    [Header("Walking Settings")]
    public float walkSpeed = 2f;
    public float walkRadius = 1f;
    public bool walkInCircles = true;
    private Vector3 spawnLocalPosition;
    private float walkAngle = 0f;

    public SplineComputer assignedSpline;
    private double currentPercent = 0.0;
    private float splineLength;
    private bool directionForward = true;

    public override void Initialize(Planet planet)
    {
        base.Initialize(planet);
        spawnLocalPosition = transform.localPosition;
        blocksCats = false;
        walkAngle = Random.Range(0f, 360f);
        if (assignedSpline != null)
        {
            SplineSample projSample = assignedSpline.Project(transform.position);
            currentPercent = projSample.percent;
            splineLength = assignedSpline.CalculateLength();
        }
    }

    protected override void UpdateAnimation()
    {
        if (parentPlanet == null) return;
        if (assignedSpline != null)
        {
            float delta = (walkSpeed * Time.deltaTime) / 20;
            if (directionForward)
                currentPercent += delta;
            else
                currentPercent -= delta;
            if (currentPercent > 1.0)
            {
                if (assignedSpline.isClosed)
                    currentPercent -= 1.0;
                else
                {
                    currentPercent = 1.0;
                    directionForward = false;
                }
            }
            else if (currentPercent < 0.0)
            {
                currentPercent = 0.0;
                directionForward = true;
            }
            SplineSample sample = assignedSpline.Evaluate(currentPercent);
            Vector3 pos = sample.position;
            Vector3 normal = sample.up;
            Vector3 tangent = sample.forward;
            if (!directionForward)
                tangent = -tangent;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(tangent, normal);
        }
        else if (walkInCircles)
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