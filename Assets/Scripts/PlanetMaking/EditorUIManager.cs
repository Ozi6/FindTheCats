using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EditorUIManager
{
    private PlanetEditor editor;

    public EditorUIManager(PlanetEditor editor)
    {
        this.editor = editor;
    }

    public void SetupCategoryUI()
    {
        foreach (var category in editor.categories)
        {
            GameObject panel = Object.Instantiate(editor.categoryPanelPrefab, editor.categoriesContainer);
            panel.name = category.name + "Panel";
            TextMeshProUGUI title = panel.GetComponentInChildren<TextMeshProUGUI>();
            if (title) title.text = category.name;

            Transform content = panel.transform.Find($"Viewport/Content");
            foreach (var prefab in category.prefabs)
            {
                GameObject button = Object.Instantiate(editor.itemButtonPrefab, content);
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
                entry.callback.AddListener((data) => { editor.PlacementManager.StartDrag(prefab, category); });
                trigger.triggers.Add(entry);
            }
        }
    }

    public void SetupManipulationUI()
    {
        if (editor.deleteButton) editor.deleteButton.onClick.AddListener(editor.SelectionManager.DeleteSelectedObject);
        if (editor.rotateXButton) editor.rotateXButton.onClick.AddListener(() => editor.SelectionManager.RotateSelectedObject(Vector3.right));
        if (editor.rotateYButton) editor.rotateYButton.onClick.AddListener(() => editor.SelectionManager.RotateSelectedObject(Vector3.up));
        if (editor.rotateZButton) editor.rotateZButton.onClick.AddListener(() => editor.SelectionManager.RotateSelectedObject(Vector3.forward));
        if (editor.rotationSpeedSlider)
        {
            editor.rotationSpeedSlider.value = 45f;
            editor.rotationSpeedSlider.onValueChanged.AddListener(editor.SelectionManager.SetRotationSpeed);
        }
    }
}