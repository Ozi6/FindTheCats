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

    [Header("Placement Settings")]
    public float minPlacementDistance = 1f;
    public Material previewMaterialValid;
    public Material previewMaterialInvalid;
    public LayerMask placementLayer;
    public float surfaceOffset = 0.1f;
    public bool stackingAllowed = true;

    private PlacementManager placementManager;
    private SelectionManager selectionManager;
    private EditorUIManager uiManager;
    private EditorDataManager dataManager;

    private List<GameObject> placedObjects = new List<GameObject>();

    void Start()
    {
        InitializeManagers();
        SetupUI();
        SetupEventListeners();

        if (manipulationPanel) manipulationPanel.SetActive(false);
    }

    private void InitializeManagers()
    {
        placementManager = new PlacementManager(this);
        selectionManager = new SelectionManager(this);
        uiManager = new EditorUIManager(this);
        dataManager = new EditorDataManager(this);
    }

    private void SetupUI()
    {
        uiManager.SetupCategoryUI();
    }

    private void SetupEventListeners()
    {
        planetSizeSlider.onValueChanged.AddListener(OnPlanetSizeChanged);
        planetSizeSlider.value = planet.radius;
        UpdatePlanetSizeText(planet.radius);

        if (saveButton) saveButton.onClick.AddListener(SaveToPlanetData);
        if (loadButton) loadButton.onClick.AddListener(LoadFromPlanetData);

        uiManager.SetupManipulationUI();
    }

    void Update()
    {
        selectionManager.HandleObjectSelection();
        selectionManager.HandleObjectDragging();
        placementManager.HandlePlacement();

        if (Input.GetMouseButtonUp(0) && placementManager.HasActivePreview)
            placementManager.PlaceObjectIfValid();

        if ((Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) && placementManager.HasActivePreview)
            placementManager.CancelPlacement();

        if (Input.GetKeyDown(KeyCode.Escape) && selectionManager.HasSelectedObject)
            selectionManager.DeselectObject();
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

    public void UpdatePlanetSizeText(float value)
    {
        if (planetSizeText) planetSizeText.text = $"Planet Radius: {value:F1}";
    }

    public void SaveToPlanetData()
    {
        dataManager.SaveToPlanetData(placedObjects);
    }

    public void LoadFromPlanetData()
    {
        placementManager.CancelPlacement();
        selectionManager.DeselectObject();
        dataManager.LoadFromPlanetData(ref placedObjects);
    }

    public bool IsInPlacementMode()
    {
        return placementManager.IsInPlacementMode;
    }
    public List<GameObject> PlacedObjects => placedObjects;
    public PlacementManager PlacementManager => placementManager;
    public SelectionManager SelectionManager => selectionManager;
}