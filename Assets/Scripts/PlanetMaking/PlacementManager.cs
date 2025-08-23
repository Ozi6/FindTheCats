using System.Linq;
using UnityEngine;

public class PlacementManager
{
    private PlanetEditor editor;
    private GameObject currentPreview;
    private GameObject selectedPrefab;
    private Category selectedCategory;
    private bool isInPlacementMode = false;

    public PlacementManager(PlanetEditor editor)
    {
        this.editor = editor;
    }

    public bool IsInPlacementMode => isInPlacementMode;
    public bool HasActivePreview => currentPreview != null;

    public void StartDrag(GameObject prefab, Category category)
    {
        editor.SelectionManager.DeselectObject();

        selectedPrefab = prefab;
        selectedCategory = category;
        isInPlacementMode = true;

        if (PlanetController.Instance != null)
            PlanetController.Instance.enabled = false;

        currentPreview = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        currentPreview.SetActive(true);

        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
            rend.material = editor.previewMaterialValid;

        foreach (var col in currentPreview.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var script in currentPreview.GetComponentsInChildren<MonoBehaviour>())
            script.enabled = false;
    }

    public void HandlePlacement()
    {
        if (selectedPrefab == null || editor.editorCamera == null || currentPreview == null) return;

        Ray ray = editor.editorCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 placementPosition;
        Vector3 placementNormal;

        if (GetSurfacePositionAndNormal(ray, out placementPosition, out placementNormal))
        {
            currentPreview.SetActive(true);
            currentPreview.transform.position = placementPosition;
            currentPreview.transform.up = placementNormal;

            bool isValid = editor.stackingAllowed || IsValidPlacement(placementPosition);
            SetPreviewMaterial(isValid ? editor.previewMaterialValid : editor.previewMaterialInvalid);
        }
        else
            currentPreview.SetActive(false);
    }

    private bool GetSurfacePositionAndNormal(Ray ray, out Vector3 position, out Vector3 normal)
    {
        position = Vector3.zero;
        normal = Vector3.up;

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
        System.Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

        foreach (var hit in hits)
        {
            if (editor.SelectionManager.HasSelectedObject &&
                (hit.collider.gameObject == editor.SelectionManager.SelectedObject ||
                hit.collider.transform.IsChildOf(editor.SelectionManager.SelectedObject.transform)))
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

    private bool IsValidPlacement(Vector3 position)
    {
        if (editor.stackingAllowed) return true;

        foreach (var obj in editor.PlacedObjects)
        {
            if (obj == null) continue;
            if (Vector3.Distance(position, obj.transform.position) < editor.minPlacementDistance)
                return false;
        }
        return true;
    }

    private void SetPreviewMaterial(Material material)
    {
        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
            rend.material = material;
    }

    public void PlaceObjectIfValid()
    {
        if (currentPreview.activeSelf && (editor.stackingAllowed || IsValidPlacement(currentPreview.transform.position)))
        {
            GameObject instance = Object.Instantiate(selectedPrefab, currentPreview.transform.position, currentPreview.transform.rotation, editor.planet.transform);
            EditorPlacedItem epi = instance.AddComponent<EditorPlacedItem>();
            epi.originalPrefab = selectedPrefab;

            bool isCat = selectedCategory.name == "Cats";
            bool isCatted = selectedCategory.associatedCatPrefab != null;

            if (isCatted)
            {
                CattedObject co = instance.GetComponent<CattedObject>() ?? instance.AddComponent<CattedObject>();
                AttachCat(co, selectedCategory.associatedCatPrefab);
                co.Initialize(editor.planet);
                epi.originalCatPrefab = selectedCategory.associatedCatPrefab;
            }
            else if (isCat)
            {
                Cat cat = instance.GetComponent<Cat>() ?? instance.AddComponent<Cat>();
                cat.Initialize(editor.planet);
            }
            else
            {
                PlanetObject po = instance.GetComponent<PlanetObject>() ?? instance.AddComponent<PlanetObject>();
                po.Initialize(editor.planet);
            }

            instance.transform.localScale = Planet.Instance.ScaleDownObj(instance.transform.localScale);
            editor.PlacedObjects.Add(instance);
        }

        CancelPlacement();
    }

    private void AttachCat(CattedObject obj, GameObject catPrefab)
    {
        if (catPrefab == null) return;
        var catObj = Object.Instantiate(catPrefab);
        var cat = catObj.GetComponent<Cat>() ?? catObj.AddComponent<Cat>();
        obj.associatedCat = cat;
        catObj.transform.SetParent(obj.transform);
        catObj.transform.localPosition = Vector3.zero;
        catObj.transform.localRotation = Quaternion.identity;
        cat.Initialize(editor.planet);
    }

    public void CancelPlacement()
    {
        if (currentPreview != null)
        {
            Object.Destroy(currentPreview);
            currentPreview = null;
        }

        selectedPrefab = null;
        selectedCategory = null;
        isInPlacementMode = false;

        if (PlanetController.Instance != null)
            PlanetController.Instance.enabled = true;
    }
}