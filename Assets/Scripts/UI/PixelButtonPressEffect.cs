using UnityEngine;
using UnityEngine.EventSystems;

public class PixelButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform target; // assign the root of the visual button
    [SerializeField] private float pressOffset = 2f;

    private Vector2 _originalPos;

    private void Awake()
    {
        if (!target) target = transform as RectTransform;
        _originalPos = target.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        target.anchoredPosition = _originalPos + Vector2.down * pressOffset;
    }

    public void OnPointerUp(PointerEventData eventData) => ResetPos();
    public void OnPointerExit(PointerEventData eventData) => ResetPos();

    private void ResetPos() => target.anchoredPosition = _originalPos;
}