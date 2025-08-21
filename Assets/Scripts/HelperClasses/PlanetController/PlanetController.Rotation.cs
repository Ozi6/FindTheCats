using UnityEngine;

public partial class PlanetController
{
    void ApplyInertia()
    {
        if (!isDragging)
        {
            rotationVelocity *= inertia;
            if (rotationVelocity.magnitude < 0.1f)
                rotationVelocity = Vector3.zero;
        }
    }

    public void StopRotation()
    {
        rotationVelocity = Vector3.zero;
        isDragging = false;
    }

    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = newSpeed;
    }
}
