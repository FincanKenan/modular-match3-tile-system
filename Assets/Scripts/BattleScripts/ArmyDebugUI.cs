using System.Text;
using UnityEngine;
using UnityEngine.UI; // UI Text için

/// <summary>
/// Ekranda debug amaçlı şu bilgileri gösterir:
/// - Kaç unit slotu açık? (CurrentUnitSlots / MaxSlotCount)
/// - Her slotta hangi birlik var?
/// - Bu birlikten kaç asker birikti?
/// </summary>
public class ArmyDebugUI : MonoBehaviour
{
    [Header("References")]
    public PlayerArmyState armyState;
    public MatchRewardCollector rewardCollector;

    [Header("UI")]
    [Tooltip("Bilgileri yazdıracağımız UI Text bileşeni.")]
    public Text debugText;

    [Header("Update Settings")]
    [Tooltip("Her karede güncellensin mi? (Sürekli değişen değerler için true)")]
    public bool updateEveryFrame = true;

    void Start()
    {
        RefreshNow();
    }

    void Update()
    {
        if (updateEveryFrame)
            RefreshNow();
    }

    /// <summary>
    /// Şu anki ordu durumunu debugText'e yazar.
    /// İstersen başka bir yerden de çağırabilirsin (örneğin buton).
    /// </summary>
    public void RefreshNow()
    {
        if (debugText == null)
            return;

        if (armyState == null || armyState.config == null)
        {
            debugText.text = "ArmyDebugUI: ArmyState veya Config eksik.";
            return;
        }

        int currentSlots = armyState.CurrentUnitSlots;
        int maxSlots = armyState.config.MaxSlotCount;

        var sb = new StringBuilder();
        sb.AppendLine($"Unit Slots: {currentSlots} / {maxSlots}");
        sb.AppendLine("----------------------");

        for (int i = 0; i < maxSlots; i++)
        {
            TroopTypeSO troop = armyState.config.slotTroops != null && i < armyState.config.slotTroops.Length
                ? armyState.config.slotTroops[i]
                : null;

            // Slot açık mı?
            bool isOpen = i < currentSlots;

            string slotState = isOpen ? "Açık" : "Kilitli";

            if (troop == null)
            {
                sb.AppendLine($"Slot {i}: ({slotState}) [Boş]");
                continue;
            }

            // Bu birlikten kaç asker var?
            int soldierCount = rewardCollector != null
                ? rewardCollector.GetSoldierCount(troop)
                : 0;

            string name = string.IsNullOrEmpty(troop.displayName) ? troop.name : troop.displayName;

            sb.AppendLine($"Slot {i}: ({slotState}) {name}  →  Asker: {soldierCount}");
        }

        debugText.text = sb.ToString();
    }
}
