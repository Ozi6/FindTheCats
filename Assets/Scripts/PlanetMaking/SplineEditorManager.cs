using UnityEngine;
using UnityEngine.UI;
using Dreamteck.Splines;
using System.Collections.Generic;
using TMPro;

public class SplineEditorManager
{
    private PlanetEditor editor;
    private bool isSplineMode = false;
    private SplineComputer currentSpline;
    private List<SplinePoint> currentPoints = new List<SplinePoint>();
    private bool isPlacingSpline = false;
    private float loopDetectionDistance = 2f;

    private Button splineModeButton;
    private Button finishSplineButton;
    private Button cancelSplineButton;
    private GameObject splineModePanel;

    public SplineEditorManager(PlanetEditor editor)
    {
        this.editor = editor;
    }

    public bool IsSplineMode => isSplineMode;
    public bool IsPlacingSpline => isPlacingSpline;

    public void SetupSplineUI()
    {
        if (splineModePanel == null)
            CreateSplineModePanel();
    }

    private void CreateSplineModePanel()
    {
        GameObject panel = new GameObject("SplineModePanel");
        panel.transform.SetParent(editor.uiCanvas.transform, false);

        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(10, -10);
        rectTransform.sizeDelta = new Vector2(200, 120);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        splineModePanel = panel;

        CreateSplineModeButton();
        CreateFinishSplineButton();
        CreateCancelSplineButton();

        splineModePanel.SetActive(true);
    }

    private void CreateSplineModeButton()
    {
        GameObject buttonObj = new GameObject("SplineModeButton");
        buttonObj.transform.SetParent(splineModePanel.transform, false);

        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(0, -25);
        rectTransform.sizeDelta = new Vector2(0, 30);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = Color.gray;

        splineModeButton = buttonObj.AddComponent<Button>();
        splineModeButton.targetGraphic = buttonImage;
        splineModeButton.onClick.AddListener(ToggleSplineMode);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Spline Mode";
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontSize = 14;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void CreateFinishSplineButton()
    {
        GameObject buttonObj = new GameObject("FinishSplineButton");
        buttonObj.transform.SetParent(splineModePanel.transform, false);

        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(0, -60);
        rectTransform.sizeDelta = new Vector2(0, 30);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = Color.green;

        finishSplineButton = buttonObj.AddComponent<Button>();
        finishSplineButton.targetGraphic = buttonImage;
        finishSplineButton.onClick.AddListener(FinishCurrentSpline);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Finish Spline";
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontSize = 14;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        buttonObj.SetActive(false);
    }

    private void CreateCancelSplineButton()
    {
        GameObject buttonObj = new GameObject("CancelSplineButton");
        buttonObj.transform.SetParent(splineModePanel.transform, false);

        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(0, -95);
        rectTransform.sizeDelta = new Vector2(0, 30);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = Color.red;

        cancelSplineButton = buttonObj.AddComponent<Button>();
        cancelSplineButton.targetGraphic = buttonImage;
        cancelSplineButton.onClick.AddListener(CancelCurrentSpline);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Cancel Spline";
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontSize = 14;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        buttonObj.SetActive(false);
    }

    public void HandleSplineInput()
    {
        if (!isSplineMode) return;
        if (Input.GetMouseButtonDown(0))
            PlaceSplinePoint();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPlacingSpline)
                CancelCurrentSpline();
            else
                ExitSplineMode();
        }
    }

    private void PlaceSplinePoint()
    {
        if (editor.editorCamera == null) return;

        Ray ray = editor.editorCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 placementPosition;
        Vector3 placementNormal;

        if (GetPlanetSurfacePosition(ray, out placementPosition, out placementNormal))
        {
            if (currentPoints.Count >= 3 && IsCloseToFirstPoint(placementPosition))
            {
                CreateLoopedSpline();
                return;
            }
            if (!isPlacingSpline)
                StartNewSpline(placementPosition, placementNormal);
            else
                AddPointToSpline(placementPosition, placementNormal);
        }
    }

    private bool GetPlanetSurfacePosition(Ray ray, out Vector3 position, out Vector3 normal)
    {
        position = Vector3.zero;
        normal = Vector3.up;

        RaycastHit hit;
        Collider planetCollider = editor.planet.GetComponent<Collider>();

        if (planetCollider != null && planetCollider.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 planetCenter = editor.planet.transform.position;
            position = hit.point;
            normal = (hit.point - planetCenter).normalized;
            position += normal * editor.surfaceOffset;
            return true;
        }

        return false;
    }

    private void StartNewSpline(Vector3 position, Vector3 normal)
    {
        GameObject splineObj = new GameObject("PlanetSpline");
        splineObj.transform.SetParent(editor.planet.transform);
        currentSpline = splineObj.AddComponent<SplineComputer>();
        currentSpline.type = Spline.Type.Linear;
        SplineRenderer renderer = splineObj.AddComponent<SplineRenderer>();
        renderer.spline = currentSpline;
        renderer.size = 0.2f;
        SplinePoint firstPoint = new SplinePoint(position);
        firstPoint.normal = normal;
        firstPoint.size = 1f;
        firstPoint.color = Color.yellow;
        currentPoints.Clear();
        currentPoints.Add(firstPoint);
        currentSpline.SetPoints(currentPoints.ToArray());
        isPlacingSpline = true;
        UpdateSplineUI();
    }

    private void AddPointToSpline(Vector3 position, Vector3 normal)
    {
        SplinePoint newPoint = new SplinePoint(position);
        newPoint.normal = normal;
        newPoint.size = 1f;
        newPoint.color = Color.yellow;

        currentPoints.Add(newPoint);
        currentSpline.SetPoints(currentPoints.ToArray());
    }

    private bool IsCloseToFirstPoint(Vector3 position)
    {
        if (currentPoints.Count < 3) return false;

        float distance = Vector3.Distance(position, currentPoints[0].position);
        return distance <= loopDetectionDistance;
    }

    private void CreateLoopedSpline()
    {
        if (currentSpline != null && currentPoints.Count >= 3)
        {
            currentSpline.Close();
            currentSpline.SetPoints(currentPoints.ToArray());
            FinishCurrentSpline();
        }
    }



    public void ToggleSplineMode()
    {
        if (isSplineMode)
            ExitSplineMode();
        else
            EnterSplineMode();
    }

    private void EnterSplineMode()
    {
        isSplineMode = true;
        if (editor.PlacementManager.IsInPlacementMode)
            editor.PlacementManager.CancelPlacement();
        if (editor.SelectionManager.HasSelectedObject)
            editor.SelectionManager.DeselectObject();
        if (PlanetController.Instance != null)
            PlanetController.Instance.enabled = false;
        UpdateSplineUI();
    }

    private void ExitSplineMode()
    {
        if (isPlacingSpline)
            CancelCurrentSpline();
        isSplineMode = false;
        if (PlanetController.Instance != null)
            PlanetController.Instance.enabled = true;
        UpdateSplineUI();
    }

    public void FinishCurrentSpline()
    {
        if (currentSpline != null && currentPoints.Count >= 2)
            currentSpline.SetPoints(currentPoints.ToArray());
        ResetCurrentSpline();
    }

    public void CancelCurrentSpline()
    {
        if (currentSpline != null)
            Object.Destroy(currentSpline.gameObject);
        ResetCurrentSpline();
    }

    private void ResetCurrentSpline()
    {
        currentSpline = null;
        currentPoints.Clear();
        isPlacingSpline = false;

        UpdateSplineUI();
    }

    private void UpdateSplineUI()
    {
        if (splineModeButton != null)
        {
            Image buttonImage = splineModeButton.GetComponent<Image>();
            buttonImage.color = isSplineMode ? Color.cyan : Color.gray;

            TextMeshProUGUI buttonText = splineModeButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = isSplineMode ? "Exit Spline" : "Spline Mode";
        }

        if (finishSplineButton != null)
            finishSplineButton.gameObject.SetActive(isPlacingSpline);

        if (cancelSplineButton != null)
            cancelSplineButton.gameObject.SetActive(isPlacingSpline);
    }
}