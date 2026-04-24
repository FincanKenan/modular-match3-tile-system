using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerArmyState : MonoBehaviour
{
    public event Action OnInventoryChanged;

    [Header("Config")]
    public PlayerArmyConfig config;

    [Header("Runtime")]
    [SerializeField] private int currentUnitSlots = 0;

    [System.Serializable]
    public class TroopStack
    {
        public TroopTypeSO troop;
        public int count;
    }

    [Header("Runtime Troop Inventory")]
    public List<TroopStack> TroopStacks = new List<TroopStack>();

    // ✅ Slotlara hangi troop’un takılı olduğunu tutar
    // slotTroops.Count = MaxSlotCount kadar olur (initte hazırlanır)
    [Header("Runtime Slot Loadout")]
    [SerializeField] private List<TroopTypeSO> slotTroops = new List<TroopTypeSO>();

    private bool _initialized = false;

    public int CurrentUnitSlots
    {
        get
        {
            EnsureInitialized();
            if (config == null) return 0;
            return Mathf.Clamp(currentUnitSlots, 0, config.MaxSlotCount);
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        int maxSlots = (config != null) ? config.MaxSlotCount : 0;

        if (config != null)
        {
            currentUnitSlots = Mathf.Clamp(
                currentUnitSlots > 0 ? currentUnitSlots : config.startingUnitSlots,
                1,
                config.MaxSlotCount
            );
        }

        // ✅ Slot listesi hazırlanır
        if (maxSlots > 0)
        {
            if (slotTroops == null) slotTroops = new List<TroopTypeSO>();
            if (slotTroops.Count != maxSlots)
            {
                slotTroops.Clear();
                for (int i = 0; i < maxSlots; i++)
                    slotTroops.Add(null);
            }
        }

        _initialized = true;
    }

    // ----------------------------------------------------
    // ✅ ArmySpawnerHorizontal’ın istediği metod
    // ----------------------------------------------------
    public TroopTypeSO GetTroopInSlot(int slotIndex)
    {
        EnsureInitialized();
        if (slotTroops == null) return null;
        if (slotIndex < 0 || slotIndex >= slotTroops.Count) return null;
        return slotTroops[slotIndex];
    }

    // Slot’a troop tak (selection sistemi burayı çağırmalı)
    public bool SetTroopInSlot(int slotIndex, TroopTypeSO troop)
    {
        EnsureInitialized();
        if (slotTroops == null) return false;
        if (slotIndex < 0 || slotIndex >= slotTroops.Count) return false;

        slotTroops[slotIndex] = troop;
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ----------------------------------------------------
    // ✅ MatchRewardCollector’ın kullandıkları
    // ----------------------------------------------------

    // Şimdilik equipped = herhangi bir slota takılı mı?
    public bool IsTroopEquipped(TroopTypeSO troop)
    {
        if (troop == null) return false;
        EnsureInitialized();
        if (slotTroops == null) return false;

        for (int i = 0; i < slotTroops.Count; i++)
        {
            if (slotTroops[i] == troop)
                return true;
        }
        return false;
    }

    public void IncreaseUnitSlots(int amount)
    {
        EnsureInitialized();
        if (amount <= 0) return;

        int max = (config != null) ? config.MaxSlotCount : int.MaxValue;
        currentUnitSlots = Mathf.Clamp(currentUnitSlots + amount, 0, max);

        OnInventoryChanged?.Invoke();
    }

    // ----------------------------------------------------
    // ✅ Envanter API (UI / ödül sistemi burayı kullanır)
    // ----------------------------------------------------

    public void AddTroops(TroopTypeSO troop, int amount)
    {
        if (troop == null || amount <= 0) return;

        var stack = TroopStacks.Find(s => s.troop == troop);
        if (stack != null)
            stack.count += amount;
        else
            TroopStacks.Add(new TroopStack { troop = troop, count = amount });

        OnInventoryChanged?.Invoke();
    }

    public bool TryConsumeOne(TroopTypeSO troop)
    {
        if (troop == null) return false;

        var stack = TroopStacks.Find(s => s.troop == troop);
        if (stack == null || stack.count <= 0) return false;

        stack.count--;

        OnInventoryChanged?.Invoke();
        return true;
    }
}
