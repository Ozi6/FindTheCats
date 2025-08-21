using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform target;
    public float distance = 10f;
    public float minDistance = 5f;
    public float maxDistance = 20f;

    [Header("Auto Orbit")]
    public bool autoOrbit = false;
    public float autoOrbitSpeed = 10f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public bool enableZoom = true;

    private Vector3 currentRotation;

    void Start()
    {
        if (target == null)
        {
            Planet planet = Planet.Instance;
            if (planet != null)
                target = planet.transform;
        }
        if (target != null)
        {
            transform.position = target.position + Vector3.back * distance;
            transform.LookAt(target);
            currentRotation = transform.eulerAngles;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        HandleZoom();
        if (autoOrbit)
            AutoOrbitUpdate();
        UpdateCameraPosition();
    }

    void HandleZoom()
    {
        if (!enableZoom) return;
        float scroll = 0f;
        scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);
            Vector2 currentDistance = touch1.position - touch2.position;
            Vector2 prevDistance = (touch1.position - touch1.deltaPosition) - (touch2.position - touch2.deltaPosition);
            float deltaDistance = currentDistance.magnitude - prevDistance.magnitude;
            scroll = deltaDistance * 0.01f;
        }
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    void AutoOrbitUpdate()
    {
        currentRotation.y += autoOrbitSpeed * Time.deltaTime;
    }

    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);
        Vector3 position = target.position + rotation * Vector3.back * distance;
        transform.position = position;
        transform.LookAt(target);
    }

    public void SetAutoOrbit(bool enabled)
    {
        autoOrbit = enabled;
    }

    public void SetDistance(float newDistance)
    {
        distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
    }
}