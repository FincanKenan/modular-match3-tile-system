using UnityEngine;

public class HexPlacementInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerArmyState armyState;
    [SerializeField] private TroopCountProvider countProvider;
    [SerializeField] private TroopSelectionUI troopUI;
    [SerializeField] private BattleDeploymentState deploymentState;
    [SerializeField] private ArmySpawnerHorizontal armySpawner;

    private void Awake()
    {
        if (armyState == null) armyState = FindFirstObjectByType<PlayerArmyState>();
        if (countProvider == null) countProvider = FindFirstObjectByType<TroopCountProvider>();
        if (troopUI == null) troopUI = FindFirstObjectByType<TroopSelectionUI>();
        if (deploymentState == null) deploymentState = FindFirstObjectByType<BattleDeploymentState>();
        if (armySpawner == null) armySpawner = FindFirstObjectByType<ArmySpawnerHorizontal>();
    }

    // Örnek: hex'e troop koyma (senin dosyanda isim farklı olabilir)
    private void TryPlaceToHex(UnitSlotHex hex, TroopTypeSO troop)
    {
        if (troop == null || hex == null) return;

        // önce asker var mı kontrol (UI provider üzerinden değil, state üzerinden)
        if (armyState == null)
        {
            Debug.LogWarning("[HexPlacement] armyState NULL");
            return;
        }

        // Hex'e atama
        if (deploymentState == null)
        {
            Debug.LogWarning("[HexPlacement] DeploymentState atanmamış.");
            return;
        }

        bool placed = deploymentState.TryAssignTroopToHex(hex, troop);
        if (!placed)
        {
            Debug.Log($"[HexPlacement] TryAssignTroopToHex BAŞARISIZ. Hex={hex.name}, troop={troop.displayName}");
            return;
        }

        // 1 asker harca
        bool consumed = armyState.TryConsumeOne(troop);
        Debug.Log($"[HexPlacement] {troop.displayName} -> 1 asker harcandı? {consumed}");

        // UI sayıları yenile
        if (troopUI != null)
            troopUI.RefreshCounts();

        // Army görsellerini yenile
        
    }

    // Hex'ten 1 asker geri alma
    private void TryRemoveFromHex(UnitSlotHex hex)
    {
        if (hex == null) return;
        if (deploymentState == null) return;

        TroopTypeSO removedTroop;
        bool removed = deploymentState.TryRemoveOneTroopFromHex(hex, out removedTroop);

        if (!removed || removedTroop == null)
        {
            Debug.Log("[HexPlacement] Bu hex’ten geri alınacak asker yok.");
            return;
        }

        // 1 asker geri ver
        if (armyState != null)
            armyState.AddTroops(removedTroop, 1);

        // UI sayıları yenile
        if (troopUI != null)
            troopUI.RefreshCounts();

        // Army görsellerini yenile
        if (troopUI != null)
            troopUI.RefreshCounts();
    }
}
