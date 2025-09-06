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
            if (currentPoints.Count >= 4 && IsCloseToFirstPoint(placementPosition))
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
        currentSpline.type = Spline.Type.Bezier;
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
        if (currentPoints.Count < 4) return false;

        float distance = Vector3.Distance(position, currentPoints[0].position);
        return distance <= loopDetectionDistance;
    }

    private void CreateLoopedSpline()
    {
        if (currentSpline != null && currentPoints.Count >= 4)
        {
            currentSpline.Close();
            FinishCurrentSpline();
        }
    }

    private void InterpolateSplinePoints(bool isClosed)
    {
        List<SplinePoint> newPoints = new List<SplinePoint>();
        int pointCount = currentPoints.Count;

        for (int i = 0; i < pointCount; i++)
        {
            newPoints.Add(currentPoints[i]);

            int nextIndex = (i + 1) % pointCount;
            if (!isClosed && nextIndex == 0) break;

            Vector3 p1 = currentPoints[i].position;
            Vector3 p2 = currentPoints[nextIndex].position;
            Vector3 n1 = p1.normalized;
            Vector3 n2 = p2.normalized;
            float dot = Vector3.Dot(n1, n2);
            dot = Mathf.Clamp(dot, -1f, 1f);
            float angle = Mathf.Acos(dot);
            int segments = Mathf.Max(1, Mathf.CeilToInt(angle * Mathf.Rad2Deg / 5f));

            for (int k = 1; k < segments; k++)
            {
                float t = k / (float)segments;
                Vector3 interpDir = Vector3.Slerp(n1, n2, t).normalized;
                float mag = Mathf.Lerp(p1.magnitude, p2.magnitude, t);
                Vector3 interpPos = interpDir * mag;
                SplinePoint sp = new SplinePoint(interpPos);
                sp.normal = interpDir;
                sp.size = 1f;
                sp.color = Color.yellow;
                newPoints.Add(sp);
            }
        }

        currentPoints = newPoints;
    }

    private void SetAutoTangents()
    {
        if (currentSpline.type != Spline.Type.Bezier) return;

        SplinePoint[] points = currentSpline.GetPoints();
        int length = points.Length;
        bool isClosed = currentSpline.isClosed;

        for (int i = 0; i < length; i++)
        {
            int prevIndex = (i - 1 + length) % length;
            int nextIndex = (i + 1) % length;

            if (!isClosed && (i == 0 || i == length - 1))
            {
                if (i == 0)
                {
                    Vector3 nextt = points[nextIndex].position - points[i].position;
                    float len = nextt.magnitude / 3f;
                    points[i].tangent = points[i].position - nextt.normalized * len;
                    points[i].tangent2 = points[i].position + nextt.normalized * len;
                }
                else if (i == length - 1)
                {
                    Vector3 prevv = points[prevIndex].position - points[i].position;
                    float len = prevv.magnitude / 3f;
                    points[i].tangent = points[i].position - prevv.normalized * len;
                    points[i].tangent2 = points[i].position + prevv.normalized * len;
                }
                continue;
            }

            Vector3 prev = points[i].position - points[prevIndex].position;
            Vector3 next = points[nextIndex].position - points[i].position;
            Vector3 dir = (prev.normalized + next.normalized).normalized;
            float lengthAvg = (prev.magnitude + next.magnitude) / 6f;

            points[i].tangent = points[i].position - dir * lengthAvg;
            points[i].tangent2 = points[i].position + dir * lengthAvg;
        }

        currentSpline.SetPoints(points);
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
        {
            bool isClosed = currentSpline.isClosed;
            InterpolateSplinePoints(isClosed);
            currentSpline.SetPoints(currentPoints.ToArray());
            if (isClosed)
                currentSpline.Close();
            SetAutoTangents();
        }
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