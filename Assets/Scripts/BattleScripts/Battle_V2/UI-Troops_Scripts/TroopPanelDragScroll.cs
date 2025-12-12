using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;   // LayoutRebuilder için

public class TroopPanelDragScroll : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform content;   // Content (butonların parent'i)
    [SerializeField] private RectTransform viewport;  // TroopPanel (mask'li panel)

    private float _minY;   // en fazla aşağı ne kadar inebilir
    private float _maxY;   // en fazla yukarı (genelde 0)

    private void Awake()
    {
        if (viewport == null)
            viewport = GetComponent<RectTransform>();
    }

    private void Start()
    {
        RecalculateBounds();
    }

    /// <summary>
    /// Content / viewport boyutlarına göre kaydırma sınırlarını hesapla.
    /// </summary>
    public void RecalculateBounds()
    {
        if (content == null || viewport == null)
            return;

        // LayoutGroup + ContentSizeFitter kullandığımız için
        // rect değerlerinin güncellenmesi için zorla rebuild ediyoruz.
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        // Panelden kısa ise kaydırmaya gerek yok
        if (contentHeight <= viewportHeight + 0.5f)
        {
            _minY = _maxY = 0f;
            var pos = content.anchoredPosition;
            pos.y = 0f;
            content.anchoredPosition = pos;
        }
        else
        {
            float diff = contentHeight - viewportHeight;

            // Başlangıçta y = 0 (üst kısım görünüyor)
            // Aşağıdaki ikonları görmek için y POZİTİF yönde artsın istiyoruz.
            _minY = 0f;      // başlangıç / en yukarı
            _maxY = diff;    // en aşağıdaki ikonlar da ekrana gelene kadar
        }

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Yeni drag başlamışken limitleri tazele (buton sayısı değişmiş olabilir)
        RecalculateBounds();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (content == null) return;

        Vector2 pos = content.anchoredPosition;

        // Parmağı / mouse'u AŞAĞI çekince (delta.y > 0) => y POSİTİF artsın
        // böylece ikonlar YUKARI gider ve alttakiler görünür.
        pos.y += eventData.delta.y;

        // sınırlar 0 .. diff
        pos.y = Mathf.Clamp(pos.y, _minY, _maxY);

        content.anchoredPosition = pos;
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        // Şimdilik ekstra bir iş yok.
    }
}
