using UnityEngine;

public class TroopCountProvider : MonoBehaviour
{
    [SerializeField] private PlayerArmyState armyState;

    private void Awake()
    {
        if (armyState == null)
        {
            armyState = FindFirstObjectByType<PlayerArmyState>();
        }

        if (armyState == null)
            Debug.LogWarning("[TroopCountProvider] Sahne içinde PlayerArmyState bulunamadý.");
    }

    /// <summary> Belirli bir birlikten kaç asker var? </summary>
    public int GetCount(TroopTypeSO troop)
    {
        if (armyState == null || troop == null)
            return 0;

        return armyState.GetCount(troop);
    }

    /// <summary> Bu birlikten 1 asker harcamaya çalýþýr. Baþarýlýysa true döner. </summary>
    public bool TryConsumeOne(TroopTypeSO troop)
    {
        if (armyState == null || troop == null)
            return false;

        return armyState.TryConsumeTroops(troop, 1);
    }

    /// <summary> Dýþarýdan ekstra asker eklemek istersen (match ödülü vs.) </summary>
    public void AddTroops(TroopTypeSO troop, int amount)
    {
        if (armyState == null || troop == null || amount <= 0)
            return;

        armyState.AddTroops(troop, amount);
    }
}
