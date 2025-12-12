// PieceType.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Match3/PieceType")]
public class PieceType : ScriptableObject
{
    [Header("Base")]
    public string typeName;
    public Sprite sprite;

    [Header("Special Tile Settings")]
    public bool isSpecial = false;
    public float spawnWeight = 1f;

    [Header("Special Match Toggles")]
    public bool enableTShape5 = false;
    public bool enableLShape3x3 = false;
    public bool enableUShape5 = false;
    public bool enableSShape5 = false;

    [Header("Battle Mapping")]
    [Tooltip("Bu taþýn normal eþleþmesinden çýkacak birlik tipi (genelde seviye 1).")]
    public TroopTypeSO normalTroop;

    [Tooltip("Bu taþýn ÖZEL eþleþmesinden çýkacak birlik tipi (seviye 2/3). Boþsa normalTroop kullanýlýr.")]
    public TroopTypeSO specialTroop;

    [Header("Unit Slot Upgrade")]
    [Tooltip("Bu taþýn ÖZEL eþleþmesi gerçekleþtiðinde 1 adet ekstra Unit slotu açsýn mý? (Ýleride kullanýlacak özellik)")]
    public bool grantExtraUnitOnSpecial = false;
}
