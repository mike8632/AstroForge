using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolbeltHUD : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BuildSystem buildSystem;
    [SerializeField] private RectTransform container;
    [SerializeField] private Button buttonTemplate; // optional: used when not providing fixed slots

    [Header("Fixed Slots")]
    [SerializeField, Tooltip("Exact number of slots to show (e.g.,5).")]
    private int maxSlots = 5;
    [SerializeField, Tooltip("Provide existing UI Buttons in the scene. If empty, buttons are instantiated from the template.")]
    private Button[] slotButtons;

    [Header("Visuals")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.6f, 0.9f, 1f, 1f);
    [SerializeField] private bool showHotkeyNumbers = true;

    private readonly List<Button> _buttons = new();

    private void Awake()
    {
        if (!buildSystem) buildSystem = FindObjectOfType<BuildSystem>();
    }

    private void OnEnable()
    {
        Rebuild();
        if (buildSystem != null)
            buildSystem.SelectionChanged += OnSelectionChanged;
        OnSelectionChanged(buildSystem != null ? buildSystem.CurrentIndex : -1);
    }

    private void OnDisable()
    {
        if (buildSystem != null)
            buildSystem.SelectionChanged -= OnSelectionChanged;
    }

    public void Rebuild()
    {
        if (!buildSystem) return;
        _buttons.Clear();

        if (slotButtons != null && slotButtons.Length > 0)
        {
            // Use provided fixed buttons
            int n = Mathf.Clamp(maxSlots, 0, slotButtons.Length);
            for (int i = 0; i < slotButtons.Length; i++)
            {
                var btn = slotButtons[i];
                if (!btn) continue;
                int index = i;
                btn.onClick.RemoveAllListeners();
                if (i < n)
                {
                    btn.onClick.AddListener(() => buildSystem.SetSelectedIndex(index));
                    SetupButtonVisuals(btn, index);
                    btn.gameObject.SetActive(true);
                    _buttons.Add(btn);
                }
                else
                {
                    btn.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // Instantiate up to maxSlots from template
            if (!container || !buttonTemplate) return;
            // Clear existing children from previous builds
            for (int i = container.childCount - 1; i >= 0; i--)
                DestroyImmediate(container.GetChild(i).gameObject);
            for (int i = 0; i < maxSlots; i++)
            {
                var btn = Instantiate(buttonTemplate, container);
                btn.gameObject.SetActive(true);
                int index = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => buildSystem.SetSelectedIndex(index));
                SetupButtonVisuals(btn, index);
                _buttons.Add(btn);
            }
        }
    }

    private void SetupButtonVisuals(Button btn, int slotIndex)
    {
        // Icon from buildSystem (null if out of range)
        var img = btn.GetComponent<Image>();
        if (img)
        {
            img.sprite = buildSystem.GetIconSprite(slotIndex);
            img.color = normalColor;
            img.preserveAspect = true;
            img.enabled = img.sprite != null; // hide if no sprite available
        }

        if (showHotkeyNumbers)
        {
            var text = btn.GetComponentInChildren<TMPro.TMP_Text>();
            if (text) text.text = (slotIndex < 9 ? (slotIndex + 1).ToString() : "");
        }
    }

    private void OnSelectionChanged(int selectedIndex)
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            var btn = _buttons[i];
            if (!btn) continue;
            var img = btn.GetComponent<Image>();
            if (!img) continue;
            img.color = (i == selectedIndex) ? selectedColor : normalColor;
        }
    }
}
