using UnityEngine;

/// <summary>
/// BattleBoard üzerindeki tek bir hex noktasýný temsil eder.
/// Þimdilik sadece index tutuyor; ileride içine hangi birlik yerleþti,
/// highlight, seçili mi vs. gibi durumlar ekleyebiliriz.
/// </summary>
public class UnitSlotHex : MonoBehaviour
{
    [Tooltip("Bu hex slotunun board içindeki index'i (0,1,2...).")]
    public int index;

    // Ýleride:
    // public TroopTypeSO currentTroop;
    // public bool isLocked;
    // vs. gibi alanlar eklenebilir.
}
