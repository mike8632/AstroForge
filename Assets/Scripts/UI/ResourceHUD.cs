using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ResourceHUD : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private ResourceType[] showOrder =
    {
        ResourceType.Coal, ResourceType.IronOre, ResourceType.CopperOre, ResourceType.GoldOre, ResourceType.Stone
    };

    private void Awake()
    {
        if (!text) text = GetComponent<Text>();
        ResourceBank.Instance?.onChanged.AddListener(UpdateView);
        UpdateView();
    }

    private void OnDestroy()
    {
        if (ResourceBank.Instance) ResourceBank.Instance.onChanged.RemoveListener(UpdateView);
    }

    public void UpdateView()
    {
        if (!text || !ResourceBank.Instance) return;
        var sb = new StringBuilder();
        foreach (var t in showOrder)
        {
            int v = ResourceBank.Instance.Get(t);
            sb.Append(t).Append(": ").Append(v).Append("  ");
        }
        text.text = sb.ToString();
    }
}