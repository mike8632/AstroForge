using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeSelectorUI : MonoBehaviour
{
 public static RecipeSelectorUI Instance { get; private set; }
 [Header("Refs")]
 [SerializeField] private RectTransform panel;
 [SerializeField] private TMP_Text titleText;
 [SerializeField] private RectTransform listContainer;
 [SerializeField] private Button optionButtonTemplate;
 [SerializeField] private Button closeButton;

 private Assembler _assembler;
 private Smelter _smelter;

 private void Awake()
 {
 if (Instance != null && Instance != this) { Destroy(gameObject); return; }
 Instance = this;
 if (panel) panel.gameObject.SetActive(false);
 if (optionButtonTemplate) optionButtonTemplate.gameObject.SetActive(false);
 if (closeButton) closeButton.onClick.AddListener(Hide);
 }

 public void Show(Assembler assembler)
 {
 _assembler = assembler; _smelter = null;
 BuildList();
 }

 public void Show(Smelter smelter)
 {
 _smelter = smelter; _assembler = null;
 BuildList();
 }

 private void BuildList()
 {
 if (!panel || !listContainer)
 {
 Debug.LogWarning("RecipeSelectorUI: Missing panel or listContainer references.");
 return;
 }
 for (int i = listContainer.childCount -1; i >=0; i--)
 Destroy(listContainer.GetChild(i).gameObject);

 if (_assembler)
 {
 if (titleText) titleText.text = string.IsNullOrEmpty(_assembler.name) ? "Assembler" : _assembler.name;
 var recipes = _assembler.recipes;
 if (recipes == null || recipes.Count ==0)
 {
 AddInfoRow("No recipes configured for this Assembler");
 }
 else
 {
 for (int i =0; i < recipes.Count; i++)
 {
 var r = recipes[i]; if (r == null) continue;
 AddOptionRow(FormatAssemblerRecipe(r), () => { _assembler.SetSelectedRecipe(i); Hide(); });
 }
 }
 AddOptionRow("Auto (first craftable)", () => { _assembler.SetSelectedRecipe(-1); Hide(); });
 }
 else if (_smelter)
 {
 if (titleText) titleText.text = string.IsNullOrEmpty(_smelter.name) ? "Smelter" : _smelter.name;
 var recipes = _smelter.recipes;
 if (recipes == null || recipes.Count ==0)
 {
 AddInfoRow("No recipes configured for this Smelter");
 }
 else
 {
 for (int i =0; i < recipes.Count; i++)
 {
 var r = recipes[i]; if (r == null) continue;
 AddOptionRow(FormatSmelterRecipe(r), () => { _smelter.SetSelectedRecipe(i); Hide(); });
 }
 }
 AddOptionRow("Auto (first smeltable)", () => { _smelter.SetSelectedRecipe(-1); Hide(); });
 }
 if (panel) panel.gameObject.SetActive(true);
 }

 private void AddOptionRow(string text, System.Action onClick)
 {
 if (!optionButtonTemplate || !listContainer) return;
 var btn = Instantiate(optionButtonTemplate, listContainer);
 btn.gameObject.SetActive(true);
 var t = btn.GetComponentInChildren<TMP_Text>();
 if (t) t.text = text;
 btn.onClick.RemoveAllListeners();
 if (onClick != null) btn.onClick.AddListener(() => onClick());
 }

 private void AddInfoRow(string text)
 {
 if (!optionButtonTemplate || !listContainer) return;
 var btn = Instantiate(optionButtonTemplate, listContainer);
 btn.gameObject.SetActive(true);
 var t = btn.GetComponentInChildren<TMP_Text>();
 if (t) t.text = text;
 btn.interactable = false;
 }

 private string FormatAssemblerRecipe(Assembler.Recipe r)
 { return $"{r.input} x{r.inputCount} -> {r.output} x{r.outputCount} ({r.craftSeconds:0.##}s)"; }
 private string FormatSmelterRecipe(Smelter.SmeltRecipe r)
 { return $"{r.inputOre} x{r.inputCount} -> {r.outputIngot} x{r.outputCount} ({r.smeltSeconds:0.##}s)"; }

 public void Hide()
 {
 if (panel) panel.gameObject.SetActive(false);
 _assembler = null; _smelter = null;
 }
}
