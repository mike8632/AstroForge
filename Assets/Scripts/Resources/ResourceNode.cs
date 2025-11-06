using UnityEngine;

[DisallowMultipleComponent]
public class ResourceNode : MonoBehaviour
{
    public ResourceType type = ResourceType.Coal;

    [Tooltip("If true, infinite; otherwise finite amount.")]
    public bool infinite = true;

    [Tooltip("Only used if infinite == false.")]
    public int amount = 100;

    // Extract up to requested. Returns actual extracted.
    public int Extract(int request)
    {
        if (request <= 0) return 0;
        if (infinite) return request;
        int take = Mathf.Min(amount, request);
        amount -= take;
        if (amount <= 0) Deplete();
        return take;
    }

    private void Deplete()
    {
        // Simple: hide/destroy when empty. You can swap sprite, play effect, etc.
        Destroy(gameObject);
    }
}