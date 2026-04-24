using System.Collections.Generic;
using UnityEngine;

public class MatchRewardCollector : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public PlayerArmyState armyState;

    [Header("Rules")]
    public bool requireEquipped = false;

    // Debug/collector toplamları (istersen tamamen kaldırılabilir)
    private readonly Dictionary<TroopTypeSO, int> _troopCounts = new Dictionary<TroopTypeSO, int>();

    private void OnEnable()
    {
        if (gridManager != null)
            gridManager.OnMatchResolved += OnMatchResolved;
    }

    private void OnDisable()
    {
        if (gridManager != null)
            gridManager.OnMatchResolved -= OnMatchResolved;
    }

    public void ResetRewards()
    {
        _troopCounts.Clear();
    }

    public int GetSoldierCount(TroopTypeSO troop)
    {
        if (troop == null) return 0;
        return _troopCounts.TryGetValue(troop, out int c) ? c : 0;
    }

    public Dictionary<TroopTypeSO, int> GetAllRewardsCopy()
    {
        return new Dictionary<TroopTypeSO, int>(_troopCounts);
    }

    private void OnMatchResolved(GridManager.MatchInfo info)
    {
        if (armyState == null || info.pieceType == null)
        {
            Debug.LogWarning("[RewardCollector] armyState veya pieceType NULL, ödül yazılmadı.");
            return;
        }

        PieceType piece = info.pieceType;
        bool isSpecialMatch = IsSpecialKind(info.kind);

        TroopTypeSO troop =
            (isSpecialMatch && piece.specialTroop != null) ? piece.specialTroop : piece.normalTroop;

        if (troop == null)
        {
            Debug.LogWarning($"[RewardCollector] {piece.name} için Normal/Special Troop atanmamış.");
            return;
        }

        bool equipped = armyState.IsTroopEquipped(troop);
        Debug.Log($"[RewardCollector] Seçilen troop={troop.name}, equipped={equipped}, currentSlots={armyState.CurrentUnitSlots}");

        if (requireEquipped && !equipped)
        {
            Debug.Log($"[RewardCollector] {troop.name} aktif değil, ödül yazılmadı. (requireEquipped=true)");
            return;
        }


        int addCount;

        if (isSpecialMatch)
        {
            addCount = 1;

            if (piece.grantExtraUnitOnSpecial)
                armyState.IncreaseUnitSlots(1);
        }
        else
        {
            addCount = Mathf.Max(1, info.tileCount);
        }

        // Collector toplamı (debug)
        _troopCounts[troop] = GetSoldierCount(troop) + addCount;

        // ✅ Gerçek kaynak: PlayerArmyState
        armyState.AddTroops(troop, addCount);

        Debug.Log($"[Reward] {troop.displayName} → +{addCount} asker. CollectorToplam: {_troopCounts[troop]}");
    }

    private bool IsSpecialKind(GridManager.MatchKind kind)
    {
        return kind == GridManager.MatchKind.Special_T
            || kind == GridManager.MatchKind.Special_L3x3
            || kind == GridManager.MatchKind.Special_U
            || kind == GridManager.MatchKind.Special_S;
    }
}
