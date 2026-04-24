using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TroopButtonView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button button;

    private int _index;
    private TroopSelectionController _selection;
    private TroopTypeSO _troop;



    //  EKLENDİ: UI refresh için dışarıdan troop okunabilsin
    public TroopTypeSO Troop => _troop;

    //  EKLENDİ: TroopSelectionUI ile uyum (SetCount -> RefreshCount)
    public void SetCount(int count) => RefreshCount(count);

    public void Init(TroopTypeSO troop, int index, TroopSelectionController selection, int initialCount)
    {
        _troop = troop;
        _index = index;
        _selection = selection;

        // İKON
        if (iconImage != null && troop != null)
            iconImage.sprite = troop.icon;

        // SAYI
        RefreshCount(initialCount);

        // TIKLAMA
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        _selection?.SelectTroopByIndex(_index);
    }

    public void RefreshCount(int count)
    {
        if (countText != null)
            countText.text = "x" + count.ToString();
    }
}
