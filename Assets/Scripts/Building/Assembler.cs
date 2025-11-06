using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Building))]
public class Assembler : MonoBehaviour
{
    public enum IntakeSide { Any, LocalRight, LocalLeft, LocalUp, LocalDown }

    [System.Serializable]
    public class Recipe
    {
        public ResourceType input;
        public int inputCount =1;
        public ResourceType output;
        public int outputCount =1;
        public float craftSeconds =2f;
    }

    [Header("Crafting")] public List<Recipe> recipes = new();
    [Header("Intake Settings")] [Tooltip("Only pick up items approaching from this local side of the building.")] public IntakeSide intakeSide = IntakeSide.Any;
    [Range(0.5f,1f)] [Tooltip("Dot threshold for direction similarity when restricting intake side.")] public float intakeDotThreshold =0.75f;

    [Header("Output")] public Transform outputPoint;
    public GameObject itemPrefab;

    [Header("Selection")]
    [Tooltip("-1 = auto (first craftable). Otherwise, force this recipe.")]
    [SerializeField] private int selectedRecipeIndex = -1;
    public int SelectedRecipeIndex => selectedRecipeIndex;
    public void SetSelectedRecipe(int index)
    { selectedRecipeIndex = Mathf.Clamp(index, -1, recipes != null ? recipes.Count -1 : -1); _current = null; _progress =0f; }

    private readonly Dictionary<ResourceType,int> _buffer = new();
    private float _progress;
    private Recipe _current;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var item = other.GetComponent<ItemEntity>();
        if (!item) return;
        if (!CanIntake(item.transform.position)) return;
        if (selectedRecipeIndex >=0)
        {
            if (recipes == null || selectedRecipeIndex >= recipes.Count) return;
            if (item.type != recipes[selectedRecipeIndex].input) return;
        }
        else
        {
            if (!IsInput(item.type)) return;
        }
        _buffer.TryGetValue(item.type, out var cnt);
        _buffer[item.type] = cnt +1;
        Destroy(item.gameObject);
    }

    private bool IsInput(ResourceType t)
    { if (recipes == null) return false; foreach (var r in recipes) if (r != null && r.input == t) return true; return false; }

    private bool CanIntake(Vector3 itemPos)
    {
        if (intakeSide == IntakeSide.Any) return true;
        Vector2 delta = (Vector2)itemPos - (Vector2)transform.position;
        if (delta.sqrMagnitude <0.0001f) return false;
        Vector2 dir = delta.normalized;
        Vector2 allowed = intakeSide switch
        {
            IntakeSide.LocalRight => transform.right,
            IntakeSide.LocalLeft => -transform.right,
            IntakeSide.LocalUp => transform.up,
            IntakeSide.LocalDown => -transform.up,
            _ => dir
        };
        allowed.Normalize();
        return Vector2.Dot(dir, allowed) >= intakeDotThreshold;
    }

    private void Update()
    {
        if (_current == null) _current = FindCraftable();
        if (_current == null) return;

        _progress += Time.deltaTime;
        if (_progress >= _current.craftSeconds)
        {
            if (!_buffer.TryGetValue(_current.input, out var have) || have < _current.inputCount)
            { _current = null; return; }
            if (!CanEmit()) { _progress = _current.craftSeconds; return; }
            _buffer[_current.input] = have - _current.inputCount;
            if (_buffer[_current.input] <=0) _buffer.Remove(_current.input);
            for (int i =0; i < _current.outputCount; i++) Emit(_current.output);
            _progress -= _current.craftSeconds;
            _buffer.TryGetValue(_current.input, out var remain); if (remain < _current.inputCount) _current = null;
        }
    }

    private Recipe FindCraftable()
    {
        if (recipes == null || recipes.Count ==0) return null;
        if (selectedRecipeIndex >=0 && selectedRecipeIndex < recipes.Count)
        {
            var r = recipes[selectedRecipeIndex];
            _buffer.TryGetValue(r.input, out var have);
            return have >= r.inputCount ? r : null;
        }
        foreach (var r in recipes)
        { _buffer.TryGetValue(r.input, out var have); if (have >= r.inputCount) return r; }
        return null;
    }

    private void Emit(ResourceType type)
    {
        if (!itemPrefab) return;
        var pos = outputPoint ? outputPoint.position : transform.position + transform.right *0.5f;
        var go = Instantiate(itemPrefab, pos, Quaternion.identity);
        var item = go.GetComponent<ItemEntity>();
        if (item) item.type = type;
    }

    private bool CanEmit() => itemPrefab != null;

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        var ui = RecipeSelectorUI.Instance;
        if (ui) ui.Show(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (recipes == null) return;
        for (int i =0; i < recipes.Count; i++)
        {
            var r = recipes[i];
            if (r == null) continue;
            if (r.inputCount <1) r.inputCount =1;
            if (r.outputCount <1) r.outputCount =1;
            if (r.craftSeconds <=0f) r.craftSeconds =0.01f;
        }
        intakeDotThreshold = Mathf.Clamp(intakeDotThreshold,0.5f,1f);
        selectedRecipeIndex = Mathf.Clamp(selectedRecipeIndex, -1, recipes != null ? recipes.Count -1 : -1);
    }
#endif
}