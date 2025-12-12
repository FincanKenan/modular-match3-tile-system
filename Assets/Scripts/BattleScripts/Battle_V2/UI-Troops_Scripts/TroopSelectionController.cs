using System.Collections.Generic;
using UnityEngine;

public class TroopSelectionController : MonoBehaviour
{
    [Header("Available Troops")]
    public List<TroopTypeSO> availableTroops = new List<TroopTypeSO>();

    // Þu anda seçili buton indexi
    private int _selectedIndex = -1;

    /// <summary>
    /// Þu anda seçili olan birlik (yoksa null).
    /// TryPlaceOnHex içinde bunu kullanýyoruz.
    /// </summary>
    public TroopTypeSO SelectedTroop
    {
        get
        {
            if (availableTroops == null) return null;
            if (_selectedIndex < 0 || _selectedIndex >= availableTroops.Count)
                return null;

            return availableTroops[_selectedIndex];
        }
    }

    /// <summary>
    /// UI butonu týklandýðýnda çaðrýlacak.
    /// </summary>
    public void SelectTroopByIndex(int index)
    {
        if (availableTroops == null || availableTroops.Count == 0)
        {
            _selectedIndex = -1;
            return;
        }

        if (index < 0 || index >= availableTroops.Count)
        {
            _selectedIndex = -1;
            Debug.LogWarning($"[TroopSelection] Geçersiz index: {index}");
            return;
        }

        _selectedIndex = index;
        var t = availableTroops[index];
        Debug.Log($"[TroopSelection] Seçilen birlik index={index}, name={t.displayName}");
    }

    /// <summary>
    /// Eðer klavyeden seçim yapmak istersen (1,2,3..) buna çaðrý ekleyebilirsin.
    /// Þu an sadece UI kullanýyoruz, zorunlu deðil.
    /// </summary>
    private void Update()
    {
        // ÖRNEK: 1,2,3 tuþlarý ile seçim yapmak istersen
        /*
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectTroopByIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectTroopByIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectTroopByIndex(2);
        */
    }
}
