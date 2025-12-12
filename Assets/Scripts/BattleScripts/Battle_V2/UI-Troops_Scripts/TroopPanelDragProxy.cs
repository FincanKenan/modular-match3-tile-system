using UnityEngine;
using UnityEngine.EventSystems;

public class TroopPanelDragProxy : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TroopPanelDragScroll targetScroll;

    private void Awake()
    {
        if (targetScroll == null)
        {
            // Sahnedeki ilk TroopPanelDragScroll'ü bul
            targetScroll = FindFirstObjectByType<TroopPanelDragScroll>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (targetScroll != null)
            targetScroll.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetScroll != null)
            targetScroll.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (targetScroll != null)
            targetScroll.OnEndDrag(eventData);
    }
}
