using System.Collections.Generic;
using UnityEngine;

public class BattleBoard : MonoBehaviour
{
    [Header("Slots")]
    [Tooltip("Sahnede yerleþtirdiðin hex slot referanslarý (UnitSlotHex). Sýralarý index ile ayný olsun.")]
    public List<UnitSlotHex> hexSlots = new List<UnitSlotHex>();

    [Tooltip("Kaç slot aktif olsun? (Genelde 12'nin tamamý)")]
    public int activeSlots = 12;

    private void Awake()
    {
        if (hexSlots != null)
        {
            activeSlots = Mathf.Clamp(activeSlots, 0, hexSlots.Count);
        }
    }

    private void Start()
    {
        ApplyActiveSlots();
    }

    public void ApplyActiveSlots()
    {
        if (hexSlots == null || hexSlots.Count == 0)
        {
            Debug.LogWarning("[BattleBoard] Hex slot listesi boþ.");
            return;
        }

        activeSlots = Mathf.Clamp(activeSlots, 0, hexSlots.Count);

        for (int i = 0; i < hexSlots.Count; i++)
        {
            if (hexSlots[i] == null)
                continue;

            hexSlots[i].index = i;

            bool shouldBeActive = i < activeSlots;
            hexSlots[i].gameObject.SetActive(shouldBeActive);
        }

        Debug.Log($"[BattleBoard] ApplyActiveSlots: activeSlots={activeSlots} / max={hexSlots.Count}");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (hexSlots != null)
        {
            activeSlots = Mathf.Clamp(activeSlots, 0, hexSlots.Count);
        }
    }
#endif
}
