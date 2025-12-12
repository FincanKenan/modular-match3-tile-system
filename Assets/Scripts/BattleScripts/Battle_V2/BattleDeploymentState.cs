using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Hangi hex’te hangi birlik var + o hex’te kaç asker var
/// ve PlayerArmyState.CurrentUnitSlots’a göre
/// en fazla kaç FARKLI hex’e birlik yerleştirilebileceğini kontrol eder.
/// </summary>
public class BattleDeploymentState : MonoBehaviour
{
    [Header("References")]
    public BattleBoard battleBoard;
    public PlayerArmyState armyState;

    [Header("Seviye başına hex kapasitesi")]
    public int level1Capacity = 20;
    public int level2Capacity = 12;
    public int level3Capacity = 8;

    [Serializable]
    public class HexDeploymentSlot
    {
        public UnitSlotHex hexSlot;
        public TroopTypeSO assignedTroop;   // null = boş
        public bool isUnlocked = true;      // ileride kilitli bölgeler için
        [Min(0)] public int soldierCount = 0; // Bu hex’teki asker sayısı
    }

    [Header("Deployment Slots")]
    public List<HexDeploymentSlot> slots = new List<HexDeploymentSlot>();

    /// <summary>En fazla kaç FARKLI hex’e birlik koyabiliriz?</summary>
    public int MaxUnits => armyState != null ? armyState.CurrentUnitSlots : 0;

    /// <summary>Şu an asker bulunan FARKLI hex sayısı.</summary>
    public int CurrentDeployedUnitCount =>
        slots.Count(s => s.assignedTroop != null && s.soldierCount > 0);

    private void Awake()
    {
        SyncWithBoard();
    }

    /// <summary>
    /// BattleBoard’daki tüm UnitSlotHex’lerden slot listesini yeniden kurar.
    /// </summary>
    public void SyncWithBoard()
    {
        slots.Clear();

        if (battleBoard == null || battleBoard.hexSlots == null)
        {
            Debug.LogWarning("[BattleDeploymentState] BattleBoard yok.");
            return;
        }

        foreach (var hex in battleBoard.hexSlots)
        {
            if (hex == null) continue;

            slots.Add(new HexDeploymentSlot
            {
                hexSlot = hex,
                assignedTroop = null,
                isUnlocked = true,
                soldierCount = 0
            });
        }

        Debug.Log($"[BattleDeploymentState] SyncWithBoard: {slots.Count} slot oluşturuldu.");
    }

    /// <summary>
    /// Tek bir tıklama: verilen hex’e verilen birlikten 1 asker eklemeye çalışır.
    /// Kurallar:
    /// - Hex kilitliyse yerleştirmez.
    /// - Hex’te farklı tip birlik varsa önce temizlemeden yerleştirmez.
    /// - İlk askeri koyuyorsak (soldierCount==0) farklı dolu hex sayısı limiti
    ///   (MaxUnits) aşılmamalı.
    /// - Level’e göre capacity doluysa yerleştirmez.
    /// </summary>
    public bool TryAssignTroopToHex(UnitSlotHex hex, TroopTypeSO troop)
    {
        if (hex == null || troop == null)
            return false;

        var slot = slots.FirstOrDefault(s => s.hexSlot == hex);
        if (slot == null)
        {
            Debug.LogWarning("[BattleDeploymentState] Bu hex için slot bulunamadı.");
            return false;
        }

        // --- 1) Bu hex’e İLK KEZ asker koyuyorsak
        if (slot.assignedTroop == null && slot.soldierCount == 0)
        {
            int diffHexCount = slots.Count(s => s.assignedTroop != null && s.soldierCount > 0);

            Debug.Log($"[BattleDeploymentState] Farklı dolu hex sayısı = {diffHexCount}, MaxUnits = {MaxUnits}");

            if (diffHexCount >= MaxUnits)
            {
                Debug.Log("[BattleDeploymentState] Max unit sınırına ulaşıldı (farklı hex sayısı limiti).");
                return false;
            }
        }

        // --- 2) Bu hex’te başka tip birlik varsa reddet
        if (slot.assignedTroop != null && slot.assignedTroop != troop)
        {
            Debug.Log("[BattleDeploymentState] Bu hex’te başka birlik var. " +
                      "Önce temizlemen gerekiyor (ClearHex).");
            return false;
        }

        // --- 3) Level’e göre kapasite kontrolü
        int maxCap = GetCapacityForTroop(troop);
        if (slot.soldierCount >= maxCap)
        {
            Debug.Log($"[BattleDeploymentState] Hex kapasitesi dolu. " +
                      $"seviye={troop.level}, max={maxCap}");
            return false;
        }

        // --- 4) Buraya geldiysek 1 asker daha eklenebilir
        if (slot.assignedTroop == null)
            slot.assignedTroop = troop;

        slot.soldierCount++;

        Debug.Log($"[BattleDeploymentState] Hex {slot.hexSlot.index} => " +
                  $"{slot.assignedTroop.displayName} x{slot.soldierCount}");

        return true;
    }

    /// <summary>Seviyeye göre bu hex’teki maksimum asker kapasitesi.</summary>
    private int GetCapacityForTroop(TroopTypeSO troop)
    {
        if (troop == null) return 0;

        switch (troop.level)
        {
            case 1: return level1Capacity;   // Inspector’dan 20
            case 2: return level2Capacity;   // Inspector’dan 12
            case 3: return level3Capacity;   // Inspector’dan 8
            default:
                return level1Capacity;       // Tanımsız level için güvenli değer
        }
    }

    /// <summary>Belirtilen hex’teki birliği tamamen kaldırır (tip ve sayı sıfırlanır).</summary>
    public void ClearHex(UnitSlotHex hex)
    {
        var slot = slots.FirstOrDefault(s => s.hexSlot == hex);
        if (slot == null) return;

        slot.assignedTroop = null;
        slot.soldierCount = 0;
    }

    /// <summary>Hex’ten 1 asker eksiltir, eksilen askerin tipini out ile döner.</summary>
    public bool TryRemoveOneTroopFromHex(UnitSlotHex hex, out TroopTypeSO removedTroop)
    {
        removedTroop = null;
        if (hex == null)
            return false;

        var slot = slots.FirstOrDefault(s => s.hexSlot == hex);
        if (slot == null)
            return false;

        if (slot.soldierCount <= 0 || slot.assignedTroop == null)
            return false;

        removedTroop = slot.assignedTroop;
        slot.soldierCount--;

        if (slot.soldierCount == 0)
            slot.assignedTroop = null;

        Debug.Log($"[BattleDeploymentState] Hex {slot.hexSlot.index}’ten 1 asker çıkarıldı. " +
                  $"Kalan={slot.soldierCount}");

        return true;
    }

    /// <summary>Index ile erişmek istersen.</summary>
    public TroopTypeSO GetTroopOnHex(int index)
    {
        if (index < 0 || index >= slots.Count) return null;
        return slots[index].assignedTroop;
    }

    /// <summary>Spawner için: tüm slotların snapshot’ı.</summary>
    public List<HexDeploymentSlot> GetSlots()
    {
        return slots;
    }

    public TroopTypeSO GetTroopOnHex(UnitSlotHex hex)
    {
        if (hex == null) return null;
        var slot = slots.FirstOrDefault(s => s.hexSlot == hex);
        return slot != null ? slot.assignedTroop : null;
    }

    public int GetIndexOfHex(UnitSlotHex hex)
    {
        if (hex == null) return -1;
        return slots.FindIndex(s => s.hexSlot == hex);
    }
}
