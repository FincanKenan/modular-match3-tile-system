using System.Collections.Generic;
using UnityEngine;

public class TroopSelectionUI : MonoBehaviour
{
    [Header("References")]
    public TroopSelectionController selection;
    public RectTransform buttonContainer;
    public TroopButtonView troopButtonPrefab;
    public TroopCountProvider countProvider;

    // Oluþturulan butonlarý troop'a göre tut
    private readonly Dictionary<TroopTypeSO, TroopButtonView> _buttonsByTroop = new();

    private void OnEnable()
    {
        if (countProvider == null)
            countProvider = FindFirstObjectByType<TroopCountProvider>();

        if (selection == null)
            selection = FindFirstObjectByType<TroopSelectionController>();

        // En kritik: event'e baðlan
        if (countProvider != null)
            countProvider.OnCountsChanged += HandleCountsChanged;

        // Ýlk çizim
        RebuildFromInventory();
        RefreshCounts();
    }

    private void OnDisable()
    {
        if (countProvider != null)
            countProvider.OnCountsChanged -= HandleCountsChanged;
    }

    private void HandleCountsChanged()
    {
        // Yeni troop eklenmiþ olabilir
        RebuildFromInventory();
        RefreshCounts();
    }

    /// <summary>
    /// PlayerArmyState envanterinde olan troop'lar için butonlarý üretir.
    /// Oyun baþýnda envanter boþsa buton oluþmaz.
    /// </summary>
    private void RebuildFromInventory()
    {
        if (buttonContainer == null || troopButtonPrefab == null || countProvider == null)
            return;

        // Envanterde hangi trooplar var?
        List<TroopTypeSO> ownedTroops = countProvider.GetOwnedTroops(); // aþaðýda provider'a ekleyeceðiz

        // 1) Yeni trooplar için buton oluþtur
        for (int i = 0; i < ownedTroops.Count; i++)
        {
            TroopTypeSO troop = ownedTroops[i];
            if (troop == null) continue;

            if (_buttonsByTroop.ContainsKey(troop))
                continue;

            var btn = Instantiate(troopButtonPrefab, buttonContainer);
            int initial = countProvider.GetCount(troop);

            // index: selection için gerekebilir. Burada "owned list" index'i veriyoruz.
            btn.Init(troop, i, selection, initial);
            _buttonsByTroop[troop] = btn;
        }

        // 2) Ýstersen count=0 olanlarý kaldýr/gizle. Þimdilik KALDIRMIYORUZ (stabil kalsýn).
        // Eðer kaldýrmak istersen:
        // - ownedTroops listesinde olmayanlarý Destroy et.
    }

    public void RefreshCounts()
    {
        if (countProvider == null)
        {
            countProvider = FindFirstObjectByType<TroopCountProvider>();
            if (countProvider == null) return;
        }

        foreach (var kv in _buttonsByTroop)
        {
            TroopTypeSO troop = kv.Key;
            TroopButtonView btn = kv.Value;
            if (btn == null) continue;

            int c = countProvider.GetCount(troop);
            btn.RefreshCount(c);
        }
    }
}
