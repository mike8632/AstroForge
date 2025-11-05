using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class ResourceNode : MonoBehaviour
{
    public ResourceType type = ResourceType.Coal;

    [Tooltip("If true, infinite; otherwise finite amount.")]
    public bool infinite = true;

    [Tooltip("Only used if infinite == false.")]
    public int amount = 100;

    [Header("Depletion")]
    [Tooltip("Invoke when the node depletes.")]
    public UnityEvent OnDepleted;

    [Tooltip("If true, Destroy the GameObject on depletion; otherwise it is disabled.")]
    public bool destroyOnDeplete = true;

    private bool _depleted;

    public int Remaining => infinite ? int.MaxValue : Mathf.Max(0, amount);

    public int Extract(int request)
    {
        if (request <= 0) return 0;
        if (infinite || _depleted) return request; 

        int take = Mathf.Min(amount, request);
        amount -= take;
        if (amount <= 0) Deplete();
        return take;
    }

    private void Deplete()
    {
        if (_depleted) return;
        _depleted = true;
        try { OnDepleted?.Invoke(); } catch { }

        if (destroyOnDeplete)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (amount < 0) amount = 0;
    }
#endif
}