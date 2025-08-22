using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PlanetEditor : Singleton<PlanetEditor>
{
    protected override bool Persistent => false;

    [Header("References")]
    public Planet planet;
    public Camera editorCamera;
    public Canvas uiCanvas;

    [Header("UI Elements")]
    public GameObject categoryPanelPrefab;
    public GameObject itemButtonPrefab;
    public Transform categoriesContainer;
    public Slider planetSizeSlider;
    public TextMeshProUGUI planetSizeText;
    public Button saveButton;
    public Button loadButton;

    [Header("Object Manipulation UI")]
    public GameObject manipulationPanel;
    public Button deleteButton;
    public Button rotateXButton;
    public Button rotateYButton;
    public Button rotateZButton;
    public Slider rotationSpeedSlider;
    public TextMeshProUGUI selectedObjectText;

    [Header("Categories and Prefabs")]
    public List<Category> categories = new List<Category>();

    [System.Serializable]
    public class Category
    {
        public string name;
        public List<GameObject> prefabs = new List<GameObject>();
        public GameObject associatedCatPrefab;
    }

    [Header("Placement Settings")]
    public float minPlacementDistance = 1f;
    public Material previewMaterialValid;
    public Material previewMaterialInvalid;
    public LayerMask placementLayer;
    public float surfaceOffset = 0.1f;
    public bool stackingAllowed = true;

    private GameObject currentPreview;
    private GameObject selectedPrefab;
    private Category selectedCategory;
    private List<GameObject> placedObjects = new List<GameObject>();
    private bool isInPlacementMode = false;

    private GameObject selectedObject;
    private bool isDragging = false;
    private bool isRotating = false;
    private Vector3 dragOffset;
    private float rotationSpeed = 45f;

    void Start()
    {
        SetupUI();
        planetSizeSlider.onValueChanged.AddListener(OnPlanetSizeChanged);
        planetSizeSlider.value = planet.radius;
        UpdatePlanetSizeText(planet.radius);

        if (saveButton) saveButton.onClick.AddListener(SaveToPlanetData);
        if (loadButton) loadButton.onClick.AddListener(LoadFromPlanetData);

        SetupManipulationUI();

        if (manipulationPanel) manipulationPanel.SetActive(false);
    }

    private void SetupManipulationUI()
    {
        if (deleteButton) deleteButton.onClick.AddListener(DeleteSelectedObject);
        if (rotateXButton) rotateXButton.onClick.AddListener(() => RotateSelectedObject(Vector3.right));
        if (rotateYButton) rotateYButton.onClick.AddListener(() => RotateSelectedObject(Vector3.up));
        if (rotateZButton) rotateZButton.onClick.AddListener(() => RotateSelectedObject(Vector3.forward));
        if (rotationSpeedSlider)
        {
            rotationSpeedSlider.value = rotationSpeed;
            rotationSpeedSlider.onValueChanged.AddListener(value => rotationSpeed = value);
        }
    }

    private void SetupUI()
    {
        foreach (var category in categories)
        {
            GameObject panel = Instantiate(categoryPanelPrefab, categoriesContainer);
            panel.name = category.name + "Panel";
            TextMeshProUGUI title = panel.GetComponentInChildren<TextMeshProUGUI>();
            if (title) title.text = category.name;

            Transform content = panel.transform.Find($"Viewport/Content");
            foreach (var prefab in category.prefabs)
            {
                GameObject button = Instantiate(itemButtonPrefab, content);
                button.name = prefab.name + "Button";
                Image img = button.GetComponentInChildren<Image>();
                if (img && PrefabPreviewGenerator.Instance != null)
                {
                    Sprite previewSprite = PrefabPreviewGenerator.Instance.GeneratePreview(prefab);
                    img.sprite = previewSprite;
                }
                TextMeshProUGUI btnText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText) btnText.text = prefab.name;
                EventTrigger trigger = button.AddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
                entry.callback.AddListener((data) => { StartDrag(prefab, category); });
                trigger.triggers.Add(entry);
            }
        }
    }

    private void StartDrag(GameObject prefab, Category category)
    {
        DeselectObject();

        selectedPrefab = prefab;
        selectedCategory = category;
        isInPlacementMode = true;

        if (PlanetController.Instance != null)
            PlanetController.Instance.enabled = false;

        currentPreview = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        currentPreview.SetActive(true);

        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
            rend.material = previewMaterialValid;

        foreach (var col in currentPreview.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var script in currentPreview.GetComponentsInChildren<MonoBehaviour>())
            script.enabled = false;
    }

    void Update()
    {
        HandleObjectSelection();
        HandleObjectDragging();
        HandlePlacement();

        if (Input.GetMouseButtonUp(0) && currentPreview != null)
            PlaceObjectIfValid();

        if ((Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) && currentPreview != null)
            CancelPlacement();

        if (Input.GetKeyDown(KeyCode.Escape) && selectedObject != null)
            DeselectObject();
    }

    private void HandleObjectSelection()
    {
        if (Input.GetMouseButtonDown(0) && !isInPlacementMode && !isDragging)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;

                GameObject placedObject = placedObjects.FirstOrDefault(obj =>
                    obj == hitObject || (obj != null && hitObject.transform.IsChildOf(obj.transform)));

                if (placedObject != null)
                {
                    SelectObject(placedObject);
                    isInPlacementMode = true;
                }
                else
                    DeselectObject();
            }
            else
                DeselectObject();
        }
    }

    private void HandleObjectDragging()
    {
        if (selectedObject == null) return;

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
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
            Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
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
            bool isPlacedObject = placedObjects.Contains(hitObject) ||
                                 placedObjects.Any(obj => obj != null && hit.collider.transform.IsChildOf(obj.transform));
            bool isPlanet = hitObject == planet.gameObject ||
                           hit.collider.transform.IsChildOf(planet.transform);

            if ((isPlacedObject && stackingAllowed) || isPlanet)
            {
                position = hit.point;
                normal = hit.normal;

                if (isPlanet && !isPlacedObject)
                {
                    Vector3 planetCenter = planet.transform.position;
                    normal = (hit.point - planetCenter).normalized;
                }

                position += normal * surfaceOffset;
                return true;
            }
        }

        RaycastHit planetHit;
        Collider planetCollider = planet.GetComponent<Collider>();
        if (planetCollider != null && planetCollider.Raycast(ray, out planetHit, Mathf.Infinity))
        {
            Vector3 planetCenter = planet.transform.position;
            position = planetHit.point;
            normal = (planetHit.point - planetCenter).normalized;
            position += normal * surfaceOffset;
            return true;
        }

        return false;
    }

    private void SelectObject(GameObject obj)
    {
        DeselectObject();

        selectedObject = obj;

        AddSelectionVisual(obj);

        if (manipulationPanel)
        {
            manipulationPanel.SetActive(true);
            if (selectedObjectText)
                selectedObjectText.text = $"Selected: {obj.name}";
        }
    }

    private void DeselectObject()
    {
        if (selectedObject != null)
        {
            RemoveSelectionVisual(selectedObject);
            selectedObject = null;
            isInPlacementMode = false;
        }

        if (manipulationPanel)
            manipulationPanel.SetActive(false);

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

            DestroyImmediate(selectionIndicator.GetComponent<Collider>());
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
            DestroyImmediate(selectionIndicator);
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

    private void DeleteSelectedObject()
    {
        if (selectedObject != null)
        {
            placedObjects.Remove(selectedObject);
            DestroyImmediate(selectedObject);
            DeselectObject();
        }
    }

    private void RotateSelectedObject(Vector3 axis)
    {
        if (selectedObject != null)
        {
            selectedObject.transform.Rotate(axis, rotationSpeed * Time.deltaTime * 10f, Space.Self);
        }
    }

    private void HandlePlacement()
    {
        if (selectedPrefab == null || editorCamera == null || currentPreview == null) return;

        Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 placementPosition;
        Vector3 placementNormal;

        if (GetSurfacePositionAndNormal(ray, out placementPosition, out placementNormal))
        {
            currentPreview.SetActive(true);
            currentPreview.transform.position = placementPosition;
            currentPreview.transform.up = placementNormal;

            bool isValid = stackingAllowed || IsValidPlacement(placementPosition);
            SetPreviewMaterial(isValid ? previewMaterialValid : previewMaterialInvalid);
        }
        else
            currentPreview.SetActive(false);
    }

    private bool IsValidPlacement(Vector3 position)
    {
        if (stackingAllowed) return true;

        foreach (var obj in placedObjects)
        {
            if (obj == null) continue;
            if (Vector3.Distance(position, obj.transform.position) < minPlacementDistance)
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

    private void PlaceObjectIfValid()
    {
        if (currentPreview.activeSelf && (stackingAllowed || IsValidPlacement(currentPreview.transform.position)))
        {
            GameObject instance = Instantiate(selectedPrefab, currentPreview.transform.position, currentPreview.transform.rotation, planet.transform);
            EditorPlacedItem epi = instance.AddComponent<EditorPlacedItem>();
            epi.originalPrefab = selectedPrefab;

            bool isCat = selectedCategory.name == "Cats";
            bool isCatted = selectedCategory.associatedCatPrefab != null;

            if (isCatted)
            {
                CattedObject co = instance.GetComponent<CattedObject>() ?? instance.AddComponent<CattedObject>();
                AttachCat(co, selectedCategory.associatedCatPrefab);
                co.Initialize(planet);
                epi.originalCatPrefab = selectedCategory.associatedCatPrefab;
            }
            else if (isCat)
            {
                Cat cat = instance.GetComponent<Cat>() ?? instance.AddComponent<Cat>();
                cat.Initialize(planet);
            }
            else
            {
                PlanetObject po = instance.GetComponent<PlanetObject>() ?? instance.AddComponent<PlanetObject>();
                po.Initialize(planet);
            }

            instance.transform.localScale = Planet.Instance.ScaleDownObj(instance.transform.localScale);
            placedObjects.Add(instance);
        }

        CancelPlacement();
    }

    private void CancelPlacement()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        selectedPrefab = null;
        selectedCategory = null;
        isInPlacementMode = false;

        if (PlanetController.Instance != null)
            PlanetController.Instance.enabled = true;
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
        cat.Initialize(planet);
    }

    private void OnPlanetSizeChanged(float value)
    {
        planet.radius = value;
        UpdatePlanetSizeText(value);

        if (planet.meshBuilder != null) planet.meshBuilder.SetupPlanetMesh();

        foreach (var obj in placedObjects)
        {
            if (obj == null) continue;
            Vector3 dir = obj.transform.localPosition.normalized;
            obj.transform.localPosition = dir * value;
            obj.transform.up = dir;
        }
    }

    private void UpdatePlanetSizeText(float value)
    {
        if (planetSizeText) planetSizeText.text = $"Planet Radius: {value:F1}";
    }

    public void SaveToPlanetData()
    {
        planet.planetData.planetRadius = planet.radius;
        planet.planetData.placedObjectsToLoad.Clear();
        planet.planetData.placedCatsToLoad.Clear();
        planet.planetData.placedCattedToLoad.Clear();

        foreach (var instance in placedObjects)
        {
            if (instance == null) continue;
            var epi = instance.GetComponent<EditorPlacedItem>();
            if (epi == null) continue;

            var localPos = instance.transform.localPosition;
            var localRot = instance.transform.localRotation;

            if (instance.GetComponent<CattedObject>())
            {
                var data = new PlacedCattedData
                {
                    containerPrefab = epi.originalPrefab,
                    catPrefab = epi.originalCatPrefab,
                    localPosition = localPos,
                    localRotation = localRot
                };
                planet.planetData.placedCattedToLoad.Add(data);
            }
            else if (instance.GetComponent<Cat>())
            {
                var data = new PlacedCatData
                {
                    catPrefab = epi.originalPrefab,
                    localPosition = localPos,
                    localRotation = localRot
                };
                planet.planetData.placedCatsToLoad.Add(data);
            }
            else
            {
                var data = new PlacedObjectData
                {
                    prefab = epi.originalPrefab,
                    localPosition = localPos,
                    localRotation = localRot
                };
                planet.planetData.placedObjectsToLoad.Add(data);
            }
        }
    }

    public void LoadFromPlanetData()
    {
        CancelPlacement();
        DeselectObject();

        foreach (var obj in placedObjects.ToArray())
            if (obj != null)
                Destroy(obj);
        placedObjects.Clear();

        planet.radius = planet.planetData.planetRadius;
        planet.meshBuilder.SetupPlanetMesh();
        planetSizeSlider.value = planet.radius;
        UpdatePlanetSizeText(planet.radius);

        foreach (var data in planet.planetData.placedObjectsToLoad)
        {
            var instance = Instantiate(data.prefab, data.localPosition, data.localRotation, planet.transform);
            var po = instance.GetComponent<PlanetObject>() ?? instance.AddComponent<PlanetObject>();
            po.Initialize(planet);
            var epi = instance.AddComponent<EditorPlacedItem>();
            epi.originalPrefab = data.prefab;
            placedObjects.Add(instance);
        }

        foreach (var data in planet.planetData.placedCatsToLoad)
        {
            var instance = Instantiate(data.catPrefab, data.localPosition, data.localRotation, planet.transform);
            var cat = instance.GetComponent<Cat>() ?? instance.AddComponent<Cat>();
            cat.Initialize(planet);
            var epi = instance.AddComponent<EditorPlacedItem>();
            epi.originalPrefab = data.catPrefab;
            placedObjects.Add(instance);
        }

        foreach (var data in planet.planetData.placedCattedToLoad)
        {
            var instance = Instantiate(data.containerPrefab, data.localPosition, data.localRotation, planet.transform);
            var co = instance.GetComponent<CattedObject>() ?? instance.AddComponent<CattedObject>();
            AttachCat(co, data.catPrefab);
            co.Initialize(planet);
            var epi = instance.AddComponent<EditorPlacedItem>();
            epi.originalPrefab = data.containerPrefab;
            epi.originalCatPrefab = data.catPrefab;
            placedObjects.Add(instance);
        }
    }

    public bool IsInPlacementMode()
    {
        return isInPlacementMode;
    }
}