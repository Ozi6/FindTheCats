using UnityEngine;

public class UpDownMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float amplitude = 2f;
    public float speed = 2f;
    public bool useLocalPosition = true;
    private Vector3 startPosition;
    private float timeOffset;

    void Start()
    {
        startPosition = useLocalPosition ? transform.localPosition : transform.position;
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    void Update()
    {
        float yOffset = Mathf.Sin((Time.time * speed) + timeOffset) * amplitude;
        Vector3 newPosition = startPosition + new Vector3(0, yOffset, 0);
        if (useLocalPosition)
            transform.localPosition = newPosition;
        else
            transform.position = newPosition;
    }
}