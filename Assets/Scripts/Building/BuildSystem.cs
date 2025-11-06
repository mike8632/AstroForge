using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BuildSystem : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap resourceMap; // used to detect ResourceNode (placed as prefabs on this layer)
    [SerializeField] private Camera worldCamera;

    [Header("Buildables")]
    [SerializeField] private List<BuildableDefinition> buildables;
    [SerializeField] private int selectedIndex = -1;

    [Header("Ghost")]
    [SerializeField] private Color okColor = new Color(0, 1, 0, 0.35f);
    [SerializeField] private Color badColor = new Color(1, 0, 0, 0.35f);

    [Header("Placement")]
    [SerializeField] private LayerMask blockingLayers;

    private GameObject _ghost;
    private SpriteRenderer[] _ghostRenderers;
    private bool _shuttingDown;

    public System.Action<int> SelectionChanged;

    private ResourceBank Bank => ResourceBank.Instance;
    private BuildableDefinition Sel => (selectedIndex >= 0 && selectedIndex < (buildables?.Count ?? 0)) ? buildables[selectedIndex] : null;

    public int CurrentIndex => selectedIndex;
    public int Count => buildables?.Count ?? 0;

    public void SetSelectedIndex(int i)
    {
        // Toggle off if pressing the same slot
        if (i == selectedIndex)
        {
            selectedIndex = -1;
            HideGhost();
            SelectionChanged?.Invoke(selectedIndex);
            return;
        }

        if (buildables == null || i < 0 || i >= buildables.Count)
        {
            selectedIndex = -1;
            HideGhost();
            SelectionChanged?.Invoke(selectedIndex);
            return;
        }
        selectedIndex = i;
        CreateGhost();
        SelectionChanged?.Invoke(selectedIndex);
    }

    public Sprite GetIconSprite(int index)
    {
        if (buildables == null || index < 0 || index >= buildables.Count) return null;
        var def = buildables[index];
        if (!def || !def.prefab) return null;
        var sr = def.prefab.GetComponentInChildren<SpriteRenderer>();
        return sr ? sr.sprite : null;
    }

    private void OnEnable()
    {
        _shuttingDown = false;
    }

    private void OnDisable()
    {
        _shuttingDown = true;
        if (_ghost) { Destroy(_ghost); _ghost = null; _ghostRenderers = null; }
    }

    private void OnApplicationQuit()
    {
        _shuttingDown = true;
    }

    private void Start()
    {
        if (!worldCamera) worldCamera = Camera.main;
        if (selectedIndex >= 0) CreateGhost(); else HideGhost();
    }

    private bool IsPointerOverUI()
    {
        var es = EventSystem.current;
        return es != null && es.IsPointerOverGameObject();
    }

    private void Update()
    {
        if (_shuttingDown) return;
        if (grid == null) return;
        if (worldCamera == null)
            worldCamera = Camera.main;
        if (worldCamera == null) return; // scene changed and no camera yet

        // Block while paused or hovering UI
        if (Time.timeScale == 0f) return;
        if (IsPointerOverUI()) return;

        HandleHotkeys();
        if (!Sel) return;

        var cell = GetHoverCell();
        var pos = CellCenterWorld(cell);
        UpdateGhost(pos);

        bool canPlace = CanPlaceAt(cell, Sel);
        TintGhost(canPlace ? okColor : badColor);

        if (canPlace && LeftClickDown())
            TryPlace(Sel, cell);

        if (RightClickDown())
            CancelGhostRotation();

        if (RotatePressed())
            RotateGhost();
    }

    private void HandleHotkeys()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return;
        if (k.digit1Key.wasPressedThisFrame) { SetSelectedIndex(0); return; }
        if (k.digit2Key.wasPressedThisFrame) { SetSelectedIndex(1); return; }
        if (k.digit3Key.wasPressedThisFrame) { SetSelectedIndex(2); return; }
        if (k.digit4Key.wasPressedThisFrame) { SetSelectedIndex(3); return; }
        if (k.digit5Key.wasPressedThisFrame) { SetSelectedIndex(4); return; }
        if (k.digit6Key.wasPressedThisFrame) { SetSelectedIndex(5); return; }
        if (k.digit7Key.wasPressedThisFrame) { SetSelectedIndex(6); return; }
        if (k.digit8Key.wasPressedThisFrame) { SetSelectedIndex(7); return; }
        if (k.digit9Key.wasPressedThisFrame) { SetSelectedIndex(8); return; }
#else
        for (int i = 0; i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SetSelectedIndex(i);
                break;
            }
#endif
    }

    private Vector3Int GetHoverCell()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 m = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        Vector2 m = Input.mousePosition;
#endif
        if (worldCamera == null) return grid.WorldToCell(Vector3.zero);
        var w = worldCamera.ScreenToWorldPoint(new Vector3(m.x, m.y, 0f));
        w.z = 0;
        return grid.WorldToCell(w);
    }

    private Vector3 CellCenterWorld(Vector3Int cell)
        => grid.GetCellCenterWorld(cell);

    private void CreateGhost()
    {
        if (_ghost) Destroy(_ghost);
        if (!Sel || !Sel.prefab) { HideGhost(); return; }
        _ghost = Instantiate(Sel.prefab);
        _ghost.name = "[ghost] " + Sel.displayName;
        _ghost.layer = gameObject.layer;
        foreach (var c in _ghost.GetComponentsInChildren<Collider2D>()) c.enabled = false;
        foreach (var b in _ghost.GetComponentsInChildren<Behaviour>()) b.enabled = false;
        _ghostRenderers = _ghost.GetComponentsInChildren<SpriteRenderer>(true);
        TintGhost(badColor);
        ShowGhost();
    }

    private void ShowGhost()
    {
        if (_ghost) _ghost.SetActive(true);
    }

    private void HideGhost()
    {
        if (_ghost) _ghost.SetActive(false);
    }

    private void UpdateGhost(Vector3 worldPos)
    {
        if (!_ghost) return;
        _ghost.transform.position = worldPos;
    }

    private void TintGhost(Color c)
    {
        if (_ghostRenderers == null) return;
        foreach (var r in _ghostRenderers)
        {
            var col = r.color; col = new Color(c.r, c.g, c.b, c.a); r.color = col;
        }
    }

    private void RotateGhost()
    {
        if (_ghost) _ghost.transform.Rotate(0, 0, 90f);
    }

    private void CancelGhostRotation()
    {
        if (_ghost) _ghost.transform.rotation = Quaternion.identity;
    }

    private bool CanPlaceAt(Vector3Int baseCell, BuildableDefinition def)
    {
        if (!def || !def.prefab) return false;
        if (!CostUtils.CanAfford(Bank, def.cost)) return false;

        int defaultMask = LayerMask.GetMask("Default", "Buildings");
        int maskValue = blockingLayers.value != 0 ? blockingLayers.value : defaultMask;
        var mask = (LayerMask)maskValue;

        bool requiresResource = def.placementRule == PlacementRule.OnResourceOnly;
        bool disallowResource = def.placementRule == PlacementRule.OnGroundOnly;
        bool foundResourceUnderFootprint = false;

        for (int y = 0; y < def.footprint.y; y++)
        for (int x = 0; x < def.footprint.x; x++)
        {
            var c = baseCell + new Vector3Int(x, y, 0);
            var center = CellCenterWorld(c);

            var hit = Physics2D.OverlapBox(center, grid.cellSize * 0.9f, 0f, mask);
            if (hit) return false;

            if (requiresResource && !foundResourceUnderFootprint)
            {
                if (HasAllowedResourceNodeAt(c, def.allowedResourceTypes))
                    foundResourceUnderFootprint = true;
            }

            if (disallowResource)
            {
                // If any resource node exists under footprint, block placement
                if (HasAllowedResourceNodeAt(c, null))
                    return false;
            }
        }

        if (requiresResource && !foundResourceUnderFootprint)
            return false;

        return true;
    }

    private bool HasAllowedResourceNodeAt(Vector3Int cell, ResourceType[] allowed)
    {
        var p = CellCenterWorld(cell);
        var nodes = Physics2D.OverlapPointAll(p);
        foreach (var c in nodes)
        {
            var rn = c.GetComponent<ResourceNode>() ?? c.GetComponentInParent<ResourceNode>();
            if (rn != null)
            {
                if (allowed == null || allowed.Length == 0) return true;
                foreach (var t in allowed) if (t == rn.type) return true;
            }
        }
        return false;
    }

    private void TryPlace(BuildableDefinition def, Vector3Int baseCell)
    {
        var worldPos = CellCenterWorld(baseCell);
        var go = Instantiate(def.prefab, worldPos, _ghost ? _ghost.transform.rotation : Quaternion.identity);
        var building = go.GetComponent<Building>();
        if (building) building.ConfigureFrom(def);

        CostUtils.Spend(Bank, def.cost);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Sel || grid == null) return;
        Gizmos.color = Color.cyan;
        var cell = Application.isPlaying ? GetHoverCell() : grid.WorldToCell(Vector3.zero);
        for (int y = 0; y < Sel.footprint.y; y++)
        for (int x = 0; x < Sel.footprint.x; x++)
        {
            var c = cell + new Vector3Int(x, y, 0);
            Gizmos.DrawWireCube(CellCenterWorld(c), grid.cellSize * 0.95f);
        }
    }

    private bool LeftClickDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private bool RightClickDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(1);
#endif
    }

    private bool RotatePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }
}