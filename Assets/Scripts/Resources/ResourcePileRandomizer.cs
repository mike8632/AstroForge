using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ResourcePileRandomizer : MonoBehaviour
{
    [Range(0f, 20f)] public float rotationVariance = 10f;   // degrees
    [Range(0f, 0.25f)] public float scaleVariance = 0.08f;  // ± around 1

    void OnEnable() { Apply(); }
#if UNITY_EDITOR
    void OnValidate() { Apply(); }
#endif

    void Apply()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return;
#endif
        // Make it deterministic from world position (grid cell)
        int x = Mathf.RoundToInt(transform.position.x);
        int y = Mathf.RoundToInt(transform.position.y);
        uint h = Hash2D((uint)x, (uint)y);

        float r01a = To01(h * 2246822519u);
        float r01b = To01(h * 3266489917u);

        float rot = Mathf.Lerp(-rotationVariance, rotationVariance, r01a);
        float scl = 1f + Mathf.Lerp(-scaleVariance, scaleVariance, r01b);

        transform.rotation = Quaternion.Euler(0f, 0f, rot);
        transform.localScale = new Vector3(scl, scl, 1f);
    }

    static uint Hash2D(uint x, uint y)
    {
        // simple FNV-1a 32-bit
        uint h = 2166136261u;
        h = (h ^ x) * 16777619u;
        h = (h ^ y) * 16777619u;
        return h;
    }
    static float To01(uint v) => (v & 0xFFFFFF) / 16777215f; // 24-bit to 0..1
}
