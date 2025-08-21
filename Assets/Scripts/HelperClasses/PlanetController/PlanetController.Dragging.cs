using UnityEngine;

public partial class PlanetController
{
    void StartDragging(Vector3 inputPosition)
    {
        isDragging = true;
        lastMousePosition = inputPosition;
        rotationVelocity = Vector3.zero;
    }

    void ContinueDragging(Vector3 inputPosition)
    {
        if (!isDragging) return;

        Vector3 deltaPosition = inputPosition - lastMousePosition;
        rotationVelocity.x = deltaPosition.x * rotationSpeed / Screen.width;
        rotationVelocity.y = deltaPosition.y * rotationSpeed / Screen.height;
        rotationVelocity = -Vector3.ClampMagnitude(rotationVelocity, maxRotationSpeed);
        lastMousePosition = inputPosition;
    }

    void StopDragging()
    {
        isDragging = false;
    }
}
