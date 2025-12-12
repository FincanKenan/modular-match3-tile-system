using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Troop Type")]
public class TroopTypeSO : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;

    [Tooltip("1 = low tier, 2 = mid, 3 = high")]
    [Range(1, 3)]
    public int level = 1;

    [Header("Visuals")]
    public GameObject prefab;   // Sahnede görünecek asker objesi

    [Header("Base Stats (ileride kullanýlacak)")]
    public int baseAttack = 10;
    public int baseDefense = 5;
    public int baseRange = 1;
    public float baseCritChance = 0.05f;

    [Header("UI")]
    public Sprite icon;
}
