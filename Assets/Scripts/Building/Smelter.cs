using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Building))]
public class Smelter : MonoBehaviour
{
 public enum IntakeSide { Any, LocalRight, LocalLeft, LocalUp, LocalDown }

 [System.Serializable]
 public class SmeltRecipe
 {
 public ResourceType inputOre; // e.g. IronOre
 public int inputCount =1;
 public ResourceType outputIngot; // e.g. IronIngot
 public int outputCount =1;
 public float smeltSeconds =2f; // time per batch
 }

 [Header("Recipes")] public List<SmeltRecipe> recipes = new();
 [Header("Intake Settings")] [Tooltip("Only pick up items approaching from this local side of the building.")] public IntakeSide intakeSide = IntakeSide.Any;
 [Range(0.5f,1f)] public float intakeDotThreshold =0.75f;

 [Header("Output")] public Transform outputPoint; // where ingots spawn
 [SerializeField] private GameObject itemPrefab; // generic item entity prefab

 private readonly Dictionary<ResourceType,int> _buffer = new();
 private float _progress;
 private SmeltRecipe _current;

 private void OnTriggerEnter2D(Collider2D other)
 {
 var item = other.GetComponent<ItemEntity>();
 if (!item) return;
 if (!IsInput(item.type)) return;
 if (!CanIntake(item.transform.position)) return;
 _buffer.TryGetValue(item.type, out var cnt);
 _buffer[item.type] = cnt +1;
 Destroy(item.gameObject);
 }

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
 if (_progress >= _current.smeltSeconds)
 {
 if (!_buffer.TryGetValue(_current.inputOre, out var have) || have < _current.inputCount)
 { _current = null; return; }
 if (!CanEmit()) { _progress = _current.smeltSeconds; return; }
 _buffer[_current.inputOre] = have - _current.inputCount;
 if (_buffer[_current.inputOre] <=0) _buffer.Remove(_current.inputOre);
 for (int i =0; i < _current.outputCount; i++) Emit(_current.outputIngot);
 _progress -= _current.smeltSeconds;
 _buffer.TryGetValue(_current.inputOre, out var remain);
 if (remain < _current.inputCount) _current = null;
 }
 }

 private bool IsInput(ResourceType t) { foreach (var r in recipes) if (r != null && r.inputOre == t) return true; return false; }
 private SmeltRecipe FindCraftable() { foreach (var r in recipes) { if (r == null) continue; _buffer.TryGetValue(r.inputOre, out var have); if (have >= r.inputCount) return r; } return null; }
 private void Emit(ResourceType type) { if (!itemPrefab) return; var pos = outputPoint ? outputPoint.position : transform.position + transform.right *0.5f; var go = Instantiate(itemPrefab, pos, Quaternion.identity); var item = go.GetComponent<ItemEntity>(); if (item) item.type = type; }
 private bool CanEmit() => itemPrefab != null;

#if UNITY_EDITOR
 private void OnValidate()
 {
 if (recipes == null) return;
 foreach (var r in recipes)
 {
 if (r == null) continue;
 if (r.inputCount <1) r.inputCount =1;
 if (r.outputCount <1) r.outputCount =1;
 if (r.smeltSeconds <=0f) r.smeltSeconds =0.01f;
 }
 intakeDotThreshold = Mathf.Clamp(intakeDotThreshold,0.5f,1f);
 }
#endif
}
