using System.Collections.Generic;
using UnityEngine;

public class PlayerArmyState : MonoBehaviour
{
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


    private bool _initialized = false;

    public int CurrentUnitSlots
    {
        get
        {
            EnsureInitialized();   // <<< ÖNEMLÝ
            if (config == null) return 0;
            int max = config.MaxSlotCount;
            return Mathf.Clamp(currentUnitSlots, 0, max);
        }
    }

    void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        if (config != null)
        {
            int max = config.MaxSlotCount;
            currentUnitSlots = Mathf.Clamp(
                currentUnitSlots > 0 ? currentUnitSlots : config.startingUnitSlots,
                1,
                max
            );

            Debug.Log($"[PlayerArmyState] Init. config={config.name}, startingSlots={config.startingUnitSlots}, max={config.MaxSlotCount}, current={currentUnitSlots}");
        }
        else
        {
            Debug.LogWarning("[PlayerArmyState] Config yok!");
        }

        _initialized = true;
    }

    public void SetUnitSlots(int newCount)
    {
        EnsureInitialized();
        if (config == null) return;
        currentUnitSlots = Mathf.Clamp(newCount, 1, config.MaxSlotCount);
    }

    public void IncreaseUnitSlots(int amount)
    {
        EnsureInitialized();
        if (config == null) return;
        currentUnitSlots = Mathf.Clamp(currentUnitSlots + amount, 1, config.MaxSlotCount);
        Debug.Log($"[PlayerArmyState] Açýk unit slot sayýsý: {currentUnitSlots}");
    }

    public TroopTypeSO GetTroopInSlot(int slotIndex)
    {
        EnsureInitialized();
        if (config == null || config.slotTroops == null) return null;
        if (slotIndex < 0 || slotIndex >= CurrentUnitSlots) return null;

        return config.slotTroops[slotIndex];
    }

    public bool IsTroopEquipped(TroopTypeSO troop)
    {
        EnsureInitialized();
        if (troop == null || config == null || config.slotTroops == null)
            return false;

        int slots = CurrentUnitSlots;
        for (int i = 0; i < slots; i++)
        {
            if (config.slotTroops[i] == troop)
                return true;
        }
        return false;
    }

    public int GetSlotIndexOfTroop(TroopTypeSO troop)
    {
        EnsureInitialized();
        if (troop == null || config == null || config.slotTroops == null)
            return -1;

        int slots = CurrentUnitSlots;
        for (int i = 0; i < slots; i++)
        {
            if (config.slotTroops[i] == troop)
                return i;
        }
        return -1;
    }

    [Header("Runtime Troop Inventory")]
    public List<TroopStack> troopStacks = new List<TroopStack>();

    // Belirli bir birlikten kaç asker var?
    public int GetCount(TroopTypeSO troop)
    {
        var stack = troopStacks.Find(s => s.troop == troop);
        return stack != null ? stack.count : 0;
    }

    // Match’ten asker eklerken bunu kullanacaðýz
    public void AddTroops(TroopTypeSO troop, int amount)
    {
        var stack = troopStacks.Find(s => s.troop == troop);
        if (stack == null)
        {
            stack = new TroopStack { troop = troop, count = 0 };
            troopStacks.Add(stack);
        }

        stack.count += amount;
    }

    // Unit slot’a yerleþtirirken asker “harcamak” için
    public bool TryConsumeTroops(TroopTypeSO troop, int amount)
    {
        var stack = troopStacks.Find(s => s.troop == troop);
        if (stack == null || stack.count < amount)
            return false;

        stack.count -= amount;
        return true;
    }
}
