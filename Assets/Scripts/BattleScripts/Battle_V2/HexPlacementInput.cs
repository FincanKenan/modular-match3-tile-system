using UnityEngine;
using UnityEngine.InputSystem;

public class HexPlacementInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask hexLayerMask;
    [SerializeField] private BattleDeploymentState deploymentState;
    [SerializeField] private TroopSelectionController selection;
    [SerializeField] private TroopCountProvider countProvider;
    [SerializeField] private ArmySpawnerHex armySpawner;
    [SerializeField] private TroopSelectionUI troopUI;   // UI referansı

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        Vector2 screenPos = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, hexLayerMask);

        if (!hit.collider)
            return;

        var hex = hit.collider.GetComponent<UnitSlotHex>();
        if (hex == null)
        {
            Debug.Log("[HexPlacement] Raycast, UnitSlotHex olmayan collider’a çarptı: " +
                      hit.collider.name);
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPlaceOnHex(hex);
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryRemoveFromHex(hex);
        }
    }

    private void TryPlaceOnHex(UnitSlotHex hex)
    {
        if (selection == null || selection.SelectedTroop == null)
        {
            Debug.Log("[HexPlacement] Seçili birlik yok.");
            return;
        }

        TroopTypeSO troop = selection.SelectedTroop;

        if (countProvider != null && countProvider.GetCount(troop) <= 0)
        {
            Debug.Log($"[HexPlacement] {troop.displayName} için asker KALMADI.");
            return;
        }

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

        if (countProvider != null)
        {
            bool consumed = countProvider.TryConsumeOne(troop);
            Debug.Log($"[HexPlacement] {troop.displayName} -> 1 asker harcandı? {consumed}");
        }

        // UI sayıları yenile
        if (troopUI != null)
            troopUI.RefreshCounts();

        // Army görsellerini yenile
        if (armySpawner != null)
            armySpawner.RefreshVisuals();
    }

    private void TryRemoveFromHex(UnitSlotHex hex)
    {
        if (deploymentState == null)
            return;

        TroopTypeSO removedTroop;
        bool removed = deploymentState.TryRemoveOneTroopFromHex(hex, out removedTroop);

        if (!removed || removedTroop == null)
        {
            Debug.Log("[HexPlacement] Bu hex’ten geri alınacak asker yok.");
            return;
        }

        if (countProvider != null)
            countProvider.AddTroops(removedTroop, 1);

        if (troopUI != null)
            troopUI.RefreshCounts();

        if (armySpawner != null)
            armySpawner.RefreshVisuals();

        Debug.Log($"[HexPlacement] {removedTroop.displayName} -> 1 asker geri alındı.");
    }
}
