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
    [SerializeField, Tooltip("Index reserved for bulldozer (demolish tool). Set to8 for key9 by default.")] private int bulldozerIndex =8;

    [Header("Ghost")]
    [SerializeField] private Color okColor = new Color(0,1,0,0.35f);
    [SerializeField] private Color badColor = new Color(1,0,0,0.35f);
    [SerializeField] private Color bulldozerColor = new Color(1f,0.5f,0f,0.35f);

    [Header("Placement")]
    [SerializeField] private LayerMask blockingLayers;
    [Header("Bulldozer Highlight")]
    [SerializeField] private Color bulldozerTargetColor = new Color(1f,0.3f,0.85f,0.85f);

    private GameObject _ghost;
    private SpriteRenderer[] _ghostRenderers;
    private bool _shuttingDown;
    private Building _currentBulldozerTarget;
    private readonly List<SpriteRenderer> _highlighted = new();
    private readonly Dictionary<SpriteRenderer, Color> _originalColors = new();

    public System.Action<int> SelectionChanged;

    private ResourceBank Bank => ResourceBank.Instance;
    private BuildableDefinition Sel => (selectedIndex >=0 && selectedIndex < (buildables?.Count ??0)) ? buildables[selectedIndex] : null;

    public int CurrentIndex => selectedIndex;
    public int Count => buildables?.Count ??0;
    public bool BulldozerActive => selectedIndex == bulldozerIndex;
    public int BulldozerIndex => bulldozerIndex;

    // Selection -------------------------------------------------
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

        if (buildables == null || i <0 || i >= buildables.Count)
        {
            // Special-case bulldozer so it can live outside list bounds
            if (i == bulldozerIndex)
            {
                selectedIndex = i;
                CreateGhost();
                SelectionChanged?.Invoke(selectedIndex);
                return;
            }
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
        if (index == bulldozerIndex)
        {
            // No dedicated icon by default (returns null). If you added a buildable as icon holder at this index, use its sprite.
            if (buildables != null && bulldozerIndex >=0 && bulldozerIndex < buildables.Count)
            {
                var bd = buildables[bulldozerIndex];
                if (bd && bd.prefab)
                {
                    var sr = bd.prefab.GetComponentInChildren<SpriteRenderer>();
                    return sr ? sr.sprite : null;
                }
            }
            return null;
        }
        if (buildables == null || index <0 || index >= buildables.Count) return null;
        var def = buildables[index];
        if (!def || !def.prefab) return null;
        var sr2 = def.prefab.GetComponentInChildren<SpriteRenderer>();
        return sr2 ? sr2.sprite : null;
    }

    private void OnEnable() { _shuttingDown = false; }
    private void OnDisable()
    { _shuttingDown = true; ClearHighlight(); if (_ghost) { Destroy(_ghost); _ghost = null; _ghostRenderers = null; } }
    private void OnApplicationQuit() { _shuttingDown = true; ClearHighlight(); }

    private void Start()
    {
        if (!worldCamera) worldCamera = Camera.main;
        if (selectedIndex >=0) CreateGhost(); else HideGhost();
    }

    // Utility ---------------------------------------------------
    private bool IsPointerOverUI()
    {
        var es = EventSystem.current;
        return es != null && es.IsPointerOverGameObject();
    }

    private Vector3Int GetHoverCell()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 m = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        Vector2 m = Input.mousePosition;
#endif
        if (worldCamera == null) return grid.WorldToCell(Vector3.zero);
        var w = worldCamera.ScreenToWorldPoint(new Vector3(m.x, m.y,0f));
        w.z =0; return grid.WorldToCell(w);
    }

    private Vector3 CellCenterWorld(Vector3Int cell) => grid.GetCellCenterWorld(cell);

    private Vector3 GetMouseWorld()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 m = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        Vector2 m = Input.mousePosition;
#endif
        var w = worldCamera ? worldCamera.ScreenToWorldPoint(new Vector3(m.x, m.y,0f)) : Vector3.zero;
        w.z =0f;
        return w;
    }

    // Update Loop ----------------------------------------------
    private void Update()
    {
        if (_shuttingDown) return;
        if (grid == null) return;
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;

        // Allow hotkeys even when over UI/paused (selection only)
        HandleHotkeys();

        // Block placement/ghost updates while paused or hovering UI
        if (Time.timeScale ==0f) return;
        if (IsPointerOverUI()) return;

        if (BulldozerActive)
        { UpdateBulldozerGhost(); HandleBulldozerClick(); return; }

        if (!Sel) return;
        var cell = GetHoverCell();
        var pos = CellCenterWorld(cell);
        UpdateGhost(pos);
        bool canPlace = CanPlaceAt(cell, Sel);
        TintGhost(canPlace ? okColor : badColor);
        if (canPlace && LeftClickDown()) TryPlace(Sel, cell);
        if (RightClickDown()) CancelGhostRotation();
        if (RotatePressed()) RotateGhost();
    }

    private void HandleHotkeys()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; if (k == null) return;
        if (k.digit1Key.wasPressedThisFrame) { SetSelectedIndex(0); return; }
        if (k.digit2Key.wasPressedThisFrame) { SetSelectedIndex(1); return; }
        if (k.digit3Key.wasPressedThisFrame) { SetSelectedIndex(2); return; }
        if (k.digit4Key.wasPressedThisFrame) { SetSelectedIndex(3); return; }
        if (k.digit5Key.wasPressedThisFrame) { SetSelectedIndex(4); return; }
        if (k.digit6Key.wasPressedThisFrame) { SetSelectedIndex(5); return; }
        if (k.digit7Key.wasPressedThisFrame) { SetSelectedIndex(6); return; }
        if (k.digit8Key.wasPressedThisFrame) { SetSelectedIndex(7); return; }
        if (k.digit9Key.wasPressedThisFrame) { SetSelectedIndex(bulldozerIndex); return; }
#else
         for (int i =0; i <9; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { if (i ==8) SetSelectedIndex(bulldozerIndex); else SetSelectedIndex(i); return; }
#endif
    }

    // Ghost -----------------------------------------------------
    private void CreateGhost()
    {
        if (_ghost) Destroy(_ghost);
        ClearHighlight();
        if (BulldozerActive)
        {
            _ghost = new GameObject("[ghost] Bulldozer");
            _ghost.layer = gameObject.layer;
            var sr = _ghost.AddComponent<SpriteRenderer>();
            sr.color = bulldozerColor;
            _ghostRenderers = new[] { sr };
            ShowGhost();
            return;
        }
        if (!Sel || !Sel.prefab) { HideGhost(); return; }
        _ghost = Instantiate(Sel.prefab);
        _ghost.name = "[ghost] " + Sel.displayName;
        _ghost.layer = gameObject.layer;
        foreach (var c in _ghost.GetComponentsInChildren<Collider2D>()) if (c) c.enabled = false;
        foreach (var b in _ghost.GetComponentsInChildren<Behaviour>()) if (b) b.enabled = false;
        _ghostRenderers = _ghost.GetComponentsInChildren<SpriteRenderer>(true);
        TintGhost(badColor);
        ShowGhost();
    }

    private void UpdateGhost(Vector3 worldPos) { if (!_ghost) return; _ghost.transform.position = worldPos; }
    private void ShowGhost() { if (_ghost) _ghost.SetActive(true); }
    private void HideGhost() { if (_ghost) _ghost.SetActive(false); }
    private void TintGhost(Color c) { if (_ghostRenderers == null) return; foreach (var r in _ghostRenderers) r.color = new Color(c.r, c.g, c.b, c.a); }
    private void RotateGhost() { if (_ghost && !BulldozerActive) _ghost.transform.Rotate(0,0,90f); }
    private void CancelGhostRotation() { if (_ghost && !BulldozerActive) _ghost.transform.rotation = Quaternion.identity; }

    // Bulldozer -------------------------------------------------
    private void ClearHighlight()
    {
        if (_highlighted.Count ==0) return;
        foreach (var sr in _highlighted)
        {
            if (sr && _originalColors.TryGetValue(sr, out var c)) sr.color = c;
        }
        _highlighted.Clear();
        _originalColors.Clear();
        _currentBulldozerTarget = null;
    }

    private void HighlightBuilding(Building b)
    {
        ClearHighlight();
        if (!b) return;
        _currentBulldozerTarget = b;
        foreach (var sr in b.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!sr) continue;
            _originalColors[sr] = sr.color;
            sr.color = bulldozerTargetColor;
            _highlighted.Add(sr);
        }
    }

    private Building GetBuildingAtCell(Vector3Int cell)
    {
        var center = CellCenterWorld(cell);
        // Use overlap box without strict layer mask (in case buildings not on Buildings layer yet)
        var hits = Physics2D.OverlapBoxAll(center, grid.cellSize *0.9f,0f);
        foreach (var h in hits)
        {
            if (!h) continue;
            var b = h.GetComponent<Building>() ?? h.GetComponentInParent<Building>();
            if (b) return b;
        }
        return null;
    }

    private Building GetBuildingUnderPointer()
    {
        var w = GetMouseWorld();
        var hits = Physics2D.OverlapPointAll(w);
        foreach (var h in hits)
        {
            if (!h) continue;
            var b = h.GetComponent<Building>() ?? h.GetComponentInParent<Building>();
            if (b) return b;
        }
        // Fall back to cell center probe
        return GetBuildingAtCell(GetHoverCell());
    }

    private void UpdateBulldozerGhost()
    {
        if (!_ghost || !BulldozerActive) { ClearHighlight(); return; }
        var cell = GetHoverCell();
        _ghost.transform.position = CellCenterWorld(cell);
        TintGhost(bulldozerColor);
        HighlightBuilding(GetBuildingUnderPointer());
    }

    private void HandleBulldozerClick()
    {
        if (!BulldozerActive) return;
        if (!LeftClickDown()) return;
        var target = GetBuildingUnderPointer();
        if (!target) return;
        // Refund
        var def = target.Definition;
        if (def && def.cost != null)
        {
            foreach (var rc in def.cost)
            Bank?.Add(rc.type, rc.amount);
        }
        Destroy(target.gameObject);
        ClearHighlight();
    }

    // Placement -------------------------------------------------
    private bool CanPlaceAt(Vector3Int baseCell, BuildableDefinition def)
    {
        if (!def || !def.prefab) return false;
        if (!CostUtils.CanAfford(Bank, def.cost)) return false;
        int defaultMask = LayerMask.GetMask("Default","Buildings");
        int maskValue = blockingLayers.value !=0 ? blockingLayers.value : defaultMask;
        var mask = (LayerMask)maskValue;
        bool requiresResource = def.placementRule == PlacementRule.OnResourceOnly;
        bool disallowResource = def.placementRule == PlacementRule.OnGroundOnly;
        bool foundResourceUnderFootprint = false;
        for (int y=0; y<def.footprint.y; y++) for (int x=0; x<def.footprint.x; x++)
        {
            var c = baseCell + new Vector3Int(x,y,0);
            var center = CellCenterWorld(c);
            var hit = Physics2D.OverlapBox(center, grid.cellSize *0.9f,0f, mask);
            if (hit) return false;
            if (requiresResource && !foundResourceUnderFootprint) { if (HasAllowedResourceNodeAt(c, def.allowedResourceTypes)) foundResourceUnderFootprint = true; }
            if (disallowResource) { if (HasAllowedResourceNodeAt(c, null)) return false; }
        }
        if (requiresResource && !foundResourceUnderFootprint) return false;
        return true;
    }

    private bool HasAllowedResourceNodeAt(Vector3Int cell, ResourceType[] allowed)
    {
        var p = CellCenterWorld(cell); var nodes = Physics2D.OverlapPointAll(p);
        foreach (var c in nodes)
        {
            var rn = c.GetComponent<ResourceNode>() ?? c.GetComponentInParent<ResourceNode>();
            if (rn != null)
            {
                if (allowed == null || allowed.Length ==0) return true;
                foreach (var t in allowed) if (t == rn.type) return true;
            }
        }
        return false;
    }

    private void TryPlace(BuildableDefinition def, Vector3Int baseCell)
    {
        var worldPos = CellCenterWorld(baseCell);
        var go = Instantiate(def.prefab, worldPos, _ghost ? _ghost.transform.rotation : Quaternion.identity);
        // Ensure layer assignment for bulldozer detection
        int buildingsLayer = LayerMask.NameToLayer("Buildings");
        if (buildingsLayer >=0) go.layer = buildingsLayer;
        var building = go.GetComponent<Building>();
        if (building)
        {
            building.ConfigureFrom(def);
            building.Definition = def;
        }
        CostUtils.Spend(Bank, def.cost);
    }

    // Gizmos ----------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        if (!Sel || grid == null) return; Gizmos.color = Color.cyan;
        var cell = Application.isPlaying ? GetHoverCell() : grid.WorldToCell(Vector3.zero);
        for (int y=0; y<Sel.footprint.y; y++) for (int x=0; x<Sel.footprint.x; x++)
        { var c = cell + new Vector3Int(x,y,0); Gizmos.DrawWireCube(CellCenterWorld(c), grid.cellSize *0.95f); }
    }

    // Input wrappers -------------------------------------------
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

    public BuildableDefinition GetDefinition(int index)
    {
        if (buildables == null || index <0 || index >= buildables.Count) return null;
        return buildables[index];
    }
}