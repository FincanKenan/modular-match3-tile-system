using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Player Army Config")]
public class PlayerArmyConfig : ScriptableObject
{
    [Header("Unit Slot Settings")]
    [Tooltip("Oyuncunun oyuna BAÞLARKEN açýk olan unit slot sayýsý (3 öneriyorsun).")]
    public int startingUnitSlots = 3;

    [Tooltip("Toplam tanýmlý unit slotu. Array uzunluðu kadar slot potansiyel olarak var.")]
    public TroopTypeSO[] slotTroops;

    // Maksimum açýlabilir slot = slotTroops.Length
    public int MaxSlotCount => slotTroops != null ? slotTroops.Length : 0;
}
