using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PlanetEditor : MonoBehaviour
{
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

    private GameObject currentPreview;
    private GameObject selectedPrefab;
    private Category selectedCategory;
    private List<GameObject> placedObjects = new List<GameObject>();

    void Start()
    {
        SetupUI();
        planetSizeSlider.onValueChanged.AddListener(OnPlanetSizeChanged);
        planetSizeSlider.value = planet.radius;
        UpdatePlanetSizeText(planet.radius);

        if (saveButton) saveButton.onClick.AddListener(SaveToPlanetData);
        if (loadButton) loadButton.onClick.AddListener(LoadFromPlanetData);
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
        selectedPrefab = prefab;
        selectedCategory = category;
        currentPreview = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        currentPreview.SetActive(true);
        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
            rend.material = previewMaterialValid;
        foreach (var col in currentPreview.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var script in currentPreview.GetComponentsInChildren<MonoBehaviour>())
            script.enabled = true;
    }

    void Update()
    {
        HandlePlacement();
        if (Input.GetMouseButtonUp(0) && currentPreview != null)
            PlaceObjectIfValid();
    }

    private void HandlePlacement()
    {
        if (selectedPrefab == null || editorCamera == null || currentPreview == null) return;

        Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);

        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 0.1f);

        RaycastHit hit = new RaycastHit();
        bool didHit = false;
        if (placementLayer != 0)
        {
            didHit = Physics.Raycast(ray, out hit, Mathf.Infinity, placementLayer);
        }
        if (!didHit && planet != null)
        {
            Collider planetCollider = planet.GetComponent<Collider>();
            if (planetCollider != null)
            {
                didHit = planetCollider.Raycast(ray, out hit, Mathf.Infinity);
            }
        }
        if (!didHit)
            didHit = Physics.Raycast(ray, out hit, Mathf.Infinity);

        if (didHit)
        {
            Vector3 hitPos = hit.point;
            Vector3 planetCenter = planet.transform.position;
            Vector3 normal = (hitPos - planetCenter).normalized;

            currentPreview.SetActive(true);
            currentPreview.transform.position = hitPos;
            currentPreview.transform.up = normal;

            bool isValid = IsValidPlacement(hitPos);
            SetPreviewMaterial(isValid ? previewMaterialValid : previewMaterialInvalid);
        }
        else
            currentPreview.SetActive(false);
    }

    private bool IsValidPlacement(Vector3 position)
    {
        return placedObjects.All(obj => obj == null || Vector3.Distance(position, obj.transform.position) >= minPlacementDistance);
    }

    private void SetPreviewMaterial(Material material)
    {
        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
            rend.material = material;
    }

    private void PlaceObjectIfValid()
    {
        if (currentPreview.activeSelf && IsValidPlacement(currentPreview.transform.position))
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
            Vector3 newScale = new()
            {
                x = instance.transform.localScale.x / planet.transform.localScale.x,
                y = instance.transform.localScale.y / planet.transform.localScale.y,
                z = instance.transform.localScale.z / planet.transform.localScale.z
            };
            instance.transform.localScale = newScale;
            placedObjects.Add(instance);
        }
        Destroy(currentPreview);
        currentPreview = null;
        selectedPrefab = null;
        selectedCategory = null;
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

        Debug.Log("Planet data saved.");
    }

    public void LoadFromPlanetData()
    {
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
}