using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager
{
    private PlanetEditor editor;
    private GameObject selectedObject;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private float rotationSpeed = 45f;

    public SelectionManager(PlanetEditor editor)
    {
        this.editor = editor;
    }

    public bool HasSelectedObject => selectedObject != null;
    public GameObject SelectedObject => selectedObject;

    public void HandleObjectSelection()
    {
        if (Input.GetMouseButtonDown(0) && !editor.PlacementManager.IsInPlacementMode && !isDragging)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = editor.editorCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;

                GameObject placedObject = editor.PlacedObjects.FirstOrDefault(obj =>
                    obj == hitObject || (obj != null && hitObject.transform.IsChildOf(obj.transform)));

                if (placedObject != null)
                {
                    SelectObject(placedObject);
                }
                else
                {
                    DeselectObject();
                }
            }
            else
            {
                DeselectObject();
            }
        }
    }

    public void HandleObjectDragging()
    {
        if (selectedObject == null) return;

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = editor.editorCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                if (hitObject == selectedObject || hitObject.transform.IsChildOf(selectedObject.transform))
                {
                    isDragging = true;
                    dragOffset = selectedObject.transform.position - hit.point;
                }
            }
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Ray ray = editor.editorCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 newPosition;
            Vector3 surfaceNormal;

            if (GetSurfacePositionAndNormal(ray, out newPosition, out surfaceNormal))
            {
                selectedObject.transform.position = newPosition;
                selectedObject.transform.up = surfaceNormal;
            }
        }

        if (Input.GetMouseButtonUp(0))
            isDragging = false;
    }

    private bool GetSurfacePositionAndNormal(Ray ray, out Vector3 position, out Vector3 normal)
    {
        position = Vector3.zero;
        normal = Vector3.up;

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
        System.Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

        foreach (var hit in hits)
        {
            if (selectedObject != null && (hit.collider.gameObject == selectedObject ||
                hit.collider.transform.IsChildOf(selectedObject.transform)))
                continue;

            GameObject hitObject = hit.collider.gameObject;
            bool isPlacedObject = editor.PlacedObjects.Contains(hitObject) ||
                                 editor.PlacedObjects.Any(obj => obj != null && hit.collider.transform.IsChildOf(obj.transform));
            bool isPlanet = hitObject == editor.planet.gameObject ||
                           hit.collider.transform.IsChildOf(editor.planet.transform);

            if ((isPlacedObject && editor.stackingAllowed) || isPlanet)
            {
                position = hit.point;
                normal = hit.normal;

                if (isPlanet && !isPlacedObject)
                {
                    Vector3 planetCenter = editor.planet.transform.position;
                    normal = (hit.point - planetCenter).normalized;
                }

                position += normal * editor.surfaceOffset;
                return true;
            }
        }

        RaycastHit planetHit;
        Collider planetCollider = editor.planet.GetComponent<Collider>();
        if (planetCollider != null && planetCollider.Raycast(ray, out planetHit, Mathf.Infinity))
        {
            Vector3 planetCenter = editor.planet.transform.position;
            position = planetHit.point;
            normal = (planetHit.point - planetCenter).normalized;
            position += normal * editor.surfaceOffset;
            return true;
        }

        return false;
    }

    public void SelectObject(GameObject obj)
    {
        DeselectObject();

        selectedObject = obj;

        AddSelectionVisual(obj);

        if (editor.manipulationPanel)
        {
            editor.manipulationPanel.SetActive(true);
            if (editor.selectedObjectText)
                editor.selectedObjectText.text = $"Selected: {obj.name}";
        }
    }

    public void DeselectObject()
    {
        if (selectedObject != null)
        {
            RemoveSelectionVisual(selectedObject);
            selectedObject = null;
        }

        if (editor.manipulationPanel)
            editor.manipulationPanel.SetActive(false);

        isDragging = false;
    }

    private void AddSelectionVisual(GameObject obj)
    {
        GameObject selectionIndicator = obj.transform.Find("SelectionIndicator")?.gameObject;
        if (selectionIndicator == null)
        {
            selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            selectionIndicator.name = "SelectionIndicator";
            selectionIndicator.transform.SetParent(obj.transform);
            selectionIndicator.transform.localPosition = Vector3.zero;
            selectionIndicator.transform.localRotation = Quaternion.identity;

            Bounds bounds = GetObjectBounds(obj);
            selectionIndicator.transform.localScale = bounds.size * 1.1f;

            Object.DestroyImmediate(selectionIndicator.GetComponent<Collider>());
            Renderer renderer = selectionIndicator.GetComponent<Renderer>();

            Material wireframeMat = new Material(Shader.Find("Standard"));
            wireframeMat.color = Color.yellow;
            wireframeMat.SetFloat("_Mode", 1);
            wireframeMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            wireframeMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            wireframeMat.SetInt("_ZWrite", 0);
            wireframeMat.DisableKeyword("_ALPHATEST_ON");
            wireframeMat.EnableKeyword("_ALPHABLEND_ON");
            wireframeMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            wireframeMat.renderQueue = 3000;
            wireframeMat.color = new Color(1, 1, 0, 0.3f);

            renderer.material = wireframeMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void RemoveSelectionVisual(GameObject obj)
    {
        GameObject selectionIndicator = obj.transform.Find("SelectionIndicator")?.gameObject;
        if (selectionIndicator != null)
            Object.DestroyImmediate(selectionIndicator);
    }

    private Bounds GetObjectBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            if (renderer.gameObject.name != "SelectionIndicator")
                bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    public void DeleteSelectedObject()
    {
        if (selectedObject != null)
        {
            editor.PlacedObjects.Remove(selectedObject);
            Object.DestroyImmediate(selectedObject);
            DeselectObject();
        }
    }

    public void RotateSelectedObject(Vector3 axis)
    {
        if (selectedObject != null)
        {
            selectedObject.transform.Rotate(axis, rotationSpeed * Time.deltaTime * 10f, Space.Self);
        }
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
}