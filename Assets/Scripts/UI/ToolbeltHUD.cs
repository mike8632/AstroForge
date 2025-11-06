using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    [Header("Tooltip")]
    [SerializeField] private RectTransform tooltipPanel; // a small panel with background
    [SerializeField] private TMPro.TMP_Text tooltipText; // text element inside the panel
    [SerializeField] private Vector2 tooltipOffset = new Vector2(12f, -12f);

    private readonly List<Button> _buttons = new();
    private int _hoverIndex = -1;

    private void Awake()
    {
        if (!buildSystem) buildSystem = FindObjectOfType<BuildSystem>();
        if (tooltipPanel) tooltipPanel.gameObject.SetActive(false);
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
        HideTooltip();
    }

    private void Update()
    {
        if (!tooltipPanel || !tooltipPanel.gameObject.activeSelf) return;
        // follow mouse
#if ENABLE_INPUT_SYSTEM
        Vector3 m = UnityEngine.InputSystem.Mouse.current != null ? (Vector3)UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector3.zero;
#else
        Vector3 m = Input.mousePosition;
#endif
        tooltipPanel.position = m + (Vector3)tooltipOffset;
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
                    WireHover(btn, index);
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
                WireHover(btn, index);
                _buttons.Add(btn);
            }
        }
    }

    private void WireHover(Button btn, int index)
    {
        var trig = btn.GetComponent<EventTrigger>();
        if (!trig) trig = btn.gameObject.AddComponent<EventTrigger>();
        trig.triggers ??= new List<EventTrigger.Entry>();
        trig.triggers.Clear();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowTooltipFor(index));
        trig.triggers.Add(enter);
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideTooltip());
        trig.triggers.Add(exit);
    }

    private void ShowTooltipFor(int index)
    {
        _hoverIndex = index;
        if (!tooltipPanel || !tooltipText) return;
        tooltipText.text = BuildTooltipText(index);
        tooltipPanel.gameObject.SetActive(true);
    }

    private void HideTooltip()
    {
        _hoverIndex = -1;
        if (tooltipPanel) tooltipPanel.gameObject.SetActive(false);
    }

    private string BuildTooltipText(int index)
    {
        if (!buildSystem) return string.Empty;
        // Bulldozer slot
        if (index == buildSystem.BulldozerIndex)
        {
            return "Bulldozer\n<color=#FF5555>Refunds buildings cost</color>";
        }
        var def = buildSystem.GetDefinition(index);
        if (!def) return string.Empty;
        var name = string.IsNullOrEmpty(def.displayName) ? "(Unnamed)" : def.displayName;
        // Costs
        if (def.cost == null || def.cost.Length == 0)
            return name + "\n<color=#FF5555>Cost: Free</color>";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(name).Append("\n<color=#FF5555>Cost: ");
        for (int i = 0; i < def.cost.Length; i++)
        {
            var c = def.cost[i];
            sb.Append(c.amount).Append(" ").Append(c.type);
            if (i < def.cost.Length - 1) sb.Append(", ");
        }
        sb.Append("</color>");
        return sb.ToString();
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
