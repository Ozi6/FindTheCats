using UnityEngine;

public partial class PlanetController : Singleton<PlanetController>
{
    protected override bool Persistent => false;

    [Header("Rotation Settings")]
    public float rotationSpeed = 50f;
    public float inertia = 0.95f;
    public float maxRotationSpeed = 200f;

    [Header("Input Settings")]
    public bool enableMouseInput = true;
    public bool enableTouchInput = true;

    private Camera mainCamera;
    private Vector3 lastMousePosition;
    private Vector3 rotationVelocity;
    private bool isDragging = false;
    private Planet planet;

    void Start()
    {
        planet = Planet.Instance;
    }

    void Update()
    {
        HandleInput();
        ApplyInertia();
        if (rotationVelocity.magnitude > 0.1f)
        {
            transform.Rotate(mainCamera.transform.up, rotationVelocity.x * Time.deltaTime, Space.World);
            transform.Rotate(mainCamera.transform.right, -rotationVelocity.y * Time.deltaTime, Space.World);
        }
    }
}
