using UnityEngine;

public class PrefabPreviewGenerator : Singleton<PrefabPreviewGenerator>
{
    protected override bool Persistent => false;

    [Header("Preview Settings")]
    public Camera previewCamera;
    public int previewSize = 128;
    public int previewLayerIndex = 8;
    public Vector3 cameraOffset = new Vector3(0, 0, -5);
    public float cameraDistance = 5f;

    private void Start()
    {
        if (previewCamera == null)
        {
            GameObject cameraObj = new GameObject("PreviewCamera");
            previewCamera = cameraObj.AddComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.cullingMask = 1 << previewLayerIndex;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;
        }
    }

    public Sprite GeneratePreview(GameObject prefab)
    {
        GameObject tempInstance = Instantiate(prefab);

        SetLayerRecursively(tempInstance, previewLayerIndex);
        PositionCameraForObject(tempInstance);

        RenderTexture renderTexture = new RenderTexture(previewSize, previewSize, 24);
        previewCamera.targetTexture = renderTexture;

        previewCamera.Render();
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(previewSize, previewSize, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, previewSize, previewSize), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, previewSize, previewSize), Vector2.one * 0.5f);
        previewCamera.targetTexture = null;
        DestroyImmediate(renderTexture);
        DestroyImmediate(tempInstance);
        return sprite;
    }

    private void PositionCameraForObject(GameObject obj)
    {
        Bounds bounds = GetObjectBounds(obj);
        Vector3 center = bounds.center;
        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        Vector3 cameraPos = center + cameraOffset.normalized * (maxSize * cameraDistance);
        previewCamera.transform.position = cameraPos;
        previewCamera.transform.LookAt(center);
        if (previewCamera.orthographic)
            previewCamera.orthographicSize = maxSize * 0.6f;
    }

    private Bounds GetObjectBounds(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}