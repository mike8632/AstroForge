using UnityEngine;
using TMPro;

public class ResourceHUD : MonoBehaviour
{
    [SerializeField] TMP_Text text; // Assign a TextMeshProUGUI in the Inspector

    void Start()
    {
        var bank = ResourceBank.Instance;
        if (bank != null) bank.onChanged.AddListener(Refresh);
        Refresh(); // show zeros immediately
    }

    void OnDestroy()
    {
        var bank = ResourceBank.Instance;
        if (bank != null) bank.onChanged.RemoveListener(Refresh);
    }

    void Refresh()
    {
        var b = ResourceBank.Instance;
        if (b == null) { text.text = "-"; return; }

        text.text =
            $"Stone: {b.Get(ResourceType.Stone)}\n" +
            $"Coal: {b.Get(ResourceType.Coal)}\n" +
            $"Iron: {b.Get(ResourceType.IronOre)}\n" +
            $"Copper: {b.Get(ResourceType.CopperOre)}\n" +
            $"Gold: {b.Get(ResourceType.GoldOre)}\n" +
            $"Iron Ingot: {b.Get(ResourceType.IronIngot)}";
    }
}
