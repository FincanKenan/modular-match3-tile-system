using System;
using System.Collections.Generic;
using UnityEngine;

public class TroopCountProvider : MonoBehaviour
{
    [Header("References")]
    public PlayerArmyState armyState;

    public event Action OnCountsChanged;

    private void OnEnable()
    {
        BindArmyState();
    }

    private void OnDisable()
    {
        UnbindArmyState();
    }

    private void BindArmyState()
    {
        if (armyState == null)
            armyState = FindFirstObjectByType<PlayerArmyState>();

        if (armyState != null)
        {
            armyState.OnInventoryChanged += HandleInventoryChanged;
            Debug.Log("[TroopCountProvider] ArmyState bağlandı -> " + armyState.name);
        }
        else
        {
            Debug.LogWarning("[TroopCountProvider] PlayerArmyState bulunamadı.");
        }
    }

    private void UnbindArmyState()
    {
        if (armyState != null)
            armyState.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        OnCountsChanged?.Invoke();
    }

    public int GetCount(TroopTypeSO troop)
    {
        if (troop == null || armyState == null) return 0;

        var stacks = armyState.TroopStacks;
        if (stacks == null) return 0;

        for (int i = 0; i < stacks.Count; i++)
        {
            var s = stacks[i];
            if (s != null && s.troop == troop)
                return s.count;
        }
        return 0;
    }

    // ✅ UI bununla sadece "elde olan" troopları üretir
    public List<TroopTypeSO> GetOwnedTroops()
    {
        var result = new List<TroopTypeSO>();
        if (armyState == null || armyState.TroopStacks == null) return result;

        for (int i = 0; i < armyState.TroopStacks.Count; i++)
        {
            var s = armyState.TroopStacks[i];
            if (s == null || s.troop == null) continue;
            if (s.count <= 0) continue;

            if (!result.Contains(s.troop))
                result.Add(s.troop);
        }

        return result;
    }
}
