#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SetupPhysics2DMatrix
{
    [MenuItem("Tools/AstroForge/Setup Physics2D Matrix")]
    public static void Setup()
    {
        // Helper
        void SetCollide(string a, string b, bool collide)
        {
            int la = LayerMask.NameToLayer(a);
            int lb = LayerMask.NameToLayer(b);
            if (la < 0 || lb < 0) { Debug.LogWarning($"Layer missing: {a} or {b}"); return; }
            Physics2D.IgnoreLayerCollision(la, lb, !collide);
            Physics2D.IgnoreLayerCollision(lb, la, !collide);
        }

        for (int i = 0; i < 32; i++)
            for (int j = 0; j < 32; j++)
                Physics2D.IgnoreLayerCollision(i, j, true);

        SetCollide("Buildings", "Buildings", true);
        SetCollide("Items", "CollectorTrigger", true);
        SetCollide("Items", "BeltTrigger", true);

        Debug.Log("AstroForge Physics2D matrix set.");
    }
}
#endif
