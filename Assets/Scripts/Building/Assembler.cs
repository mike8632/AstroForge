using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Building))]
public class Assembler : MonoBehaviour
{
    [System.Serializable]
    public class Recipe
    {
        public ResourceType input;
        public int inputCount = 1;
        public ResourceType output;
        public int outputCount = 1;
        public float craftSeconds = 2f;
    }

    public List<Recipe> recipes = new();
    public Transform outputPoint;
    public GameObject itemPrefab;

    private readonly Dictionary<ResourceType,int> _buffer = new();

    private float _progress;
    private Recipe _current;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var item = other.GetComponent<ItemEntity>();
        if (!item) return;

        // Take item into buffer
        _buffer.TryGetValue(item.type, out var cnt);
        _buffer[item.type] = cnt + 1;
        Destroy(item.gameObject);
    }

    private void Update()
    {
        if (_current == null) _current = FindCraftable();
        if (_current == null) return;

        _progress += Time.deltaTime;
        if (_progress >= _current.craftSeconds)
        {
            if (!_buffer.TryGetValue(_current.input, out var have) || have < _current.inputCount)
            {
                _current = null;
                return;
            }

            if (!CanEmit())
            {
                _progress = _current.craftSeconds;
                return;
            }

            _buffer[_current.input] = have - _current.inputCount;
            if (_buffer[_current.input] <= 0) _buffer.Remove(_current.input);

            for (int i = 0; i < _current.outputCount; i++)
                Emit(_current.output);

            _progress -= _current.craftSeconds;
            _buffer.TryGetValue(_current.input, out var remain);
            if (remain < _current.inputCount) _current = null;
        }
    }

    private Recipe FindCraftable()
    {
        foreach (var r in recipes)
        {
            _buffer.TryGetValue(r.input, out var have);
            if (have >= r.inputCount) return r;
        }
        return null;
    }

    private void Emit(ResourceType type)
    {
        if (!itemPrefab) return;
        var pos = outputPoint ? outputPoint.position : transform.position + Vector3.right * 0.5f;
        var go = Instantiate(itemPrefab, pos, Quaternion.identity);
        var item = go.GetComponent<ItemEntity>();
        if (item) item.type = type;
    }

    private bool CanEmit() => itemPrefab != null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (recipes == null) return;
        for (int i = 0; i < recipes.Count; i++)
        {
            var r = recipes[i];
            if (r == null) continue;
            if (r.inputCount < 1) r.inputCount = 1;
            if (r.outputCount < 1) r.outputCount = 1;
            if (r.craftSeconds <= 0f) r.craftSeconds = 0.01f;
        }
    }
#endif
}