using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MatchRewardCollector + PlayerArmyState'e göre,
/// her unit slotu için dikdörtgen bir formasyonda asker spawn eder.
/// Satýrlar önden arkaya, her satýr X ekseninde ortalanýr.
/// </summary>
public class ArmySpawnerHorizontal : MonoBehaviour
{
    [Header("References")]
    public PlayerArmyState armyState;
    public MatchRewardCollector rewardCollector;
    public Transform armyRoot;

    [Header("Unit Grid Settings")]
    [Tooltip("Bir unit içindeki maksimum sütun (ön sýradaki asker sayýsý). Örn: 10 => 10+10+...")]
    public int unitRowCapacity = 10;

    [Tooltip("Ayný satýrdaki askerler arasý X mesafesi.")]
    public float soldierSpacingX = 1.5f;

    [Tooltip("Satýrlar arasý Z mesafesi.")]
    public float soldierSpacingZ = 1.5f;

    [Tooltip("Unit dikdörtgeninin sað/sol kenarý ile askerler arasýndaki boþluk.")]
    public float unitPaddingX = 0.5f;

    [Tooltip("Unit dikdörtgeninin ön/arka kenarý ile askerler arasýndaki boþluk.")]
    public float unitPaddingZ = 0.5f;

    [Tooltip("Unit'ler arasý Z mesafesi (bir unitin arkasýndaki diðer unitin mesafesi).")]
    public float unitGapZ = 2f;

    [Header("Global Offset")]
    [Tooltip("Ýlk unit merkezinin ArmyRoot'a göre offset'i.")]
    public Vector3 firstUnitOffset = Vector3.zero;

    [Header("Debug")]
    public bool logDebug = true;
    public bool drawUnitBounds = true;
    [Tooltip("Unit sýnýrlarýný çizmek için kullanýlacak materyal (ör: Emission açýk sarý).")]
    public Material unitBoundsMaterial;

    [Header("Camera")]
    public ArmyCameraController battleCamera;

    // Dahili slot yapýsý
    private struct BattleSlot
    {
        public TroopTypeSO troop;
        public int soldierCount;
        public int slotIndex;
    }

    private readonly List<BattleSlot> _battleSlots = new List<BattleSlot>();

    #region Public API

    /// <summary>
    /// GameFlowManager -> StartBattlePhase tarafýndan çaðrýlýr.
    /// </summary>
    public void SpawnArmy()
    {
        Debug.Log("[ArmySpawner] SpawnArmy çaðrýldý.");

        if (armyState == null || rewardCollector == null || armyRoot == null)
        {
            Debug.LogWarning(
                $"[ArmySpawner] Referanslar eksik. " +
                $"armyState={(armyState != null)}, " +
                $"rewardCollector={(rewardCollector != null)}, " +
                $"armyRoot={(armyRoot != null)}"
            );
            return;
        }

        // 1) Savaþta kullanýlacak slotlarý oluþtur
        if (!BuildBattleSlotsFromState())
            return;

        // 2) Eski asker + eski debug objelerini temizle
        ClearArmyRootChildren();

        // 3) Unit'leri önden arkaya doðru diz
        float accumulatedDepth = 0f;
        for (int i = 0; i < _battleSlots.Count; i++)
        {
            BattleSlot slot = _battleSlots[i];
            int count = slot.soldierCount;
            if (count <= 0 || slot.troop == null || slot.troop.prefab == null)
                continue;

            int rows = GetRowCount(count);
            int maxCols = Mathf.Min(unitRowCapacity, count);

            float depth = ComputeUnitDepth(rows);          // Z boyu
            float width = ComputeUnitWidth(maxCols);       // X boyu

            // Bu unit'in merkezi:
            // Ýlk unit için firstUnitOffset,
            // sonrakiler için bir önceki unit'in depth'i + gap kadar arkaya kay.
            Vector3 unitCenter = armyRoot.position + firstUnitOffset;
            unitCenter.z -= accumulatedDepth + depth * 0.5f;  // kamera gerideyse -Z ile arkaya gitsin

            if (logDebug)
            {
                Debug.Log(
                    $"[ArmySpawner] Unit {i}: troop={slot.troop.name}, " +
                    $"soldiers={count}, rows={rows}, maxCols={maxCols}, " +
                    $"center={unitCenter}, sizeX={width}, sizeZ={depth}");
            }

            // 3.a) Askerleri spawn et
            SpawnUnitSoldiers(slot, unitCenter, rows);

            // 3.b) Debug için sýnýr dikdörtgeni çiz
            DrawUnitBoundsRect(unitCenter, width, depth, $"Unit{i}_{slot.troop.name}");

            // 3.c) Bir sonraki unit, bu unitin arkasýnda dursun
            accumulatedDepth += depth + unitGapZ;
        }

        // 4) Kamera orduya zýplasýn
        
    }

    #endregion

    #region Core Logic

    /// <summary>
    /// PlayerArmyState + MatchRewardCollector'dan,
    /// savaþta gerçekten asker çýkaracak slot listesini oluþturur.
    /// </summary>
    private bool BuildBattleSlotsFromState()
    {
        _battleSlots.Clear();

        if (armyState == null || rewardCollector == null)
        {
            Debug.LogWarning("[ArmySpawner] BuildBattleSlotsFromState: armyState veya rewardCollector eksik.");
            return false;
        }

        int slotCount = armyState.CurrentUnitSlots;
        if (logDebug)
            Debug.Log($"[ArmySpawner] BuildBattleSlotsFromState: slotCount = {slotCount}");

        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            TroopTypeSO troop = armyState.GetTroopInSlot(slotIndex);
            if (troop == null)
            {
                if (logDebug)
                    Debug.Log($"[ArmySpawner] Slot {slotIndex}: troop yok, atlanýyor.");
                continue;
            }

            int soldierCount = rewardCollector.GetSoldierCount(troop);

            if (logDebug)
                Debug.Log($"[ArmySpawner] Slot {slotIndex}: {troop.name}, soldierCount = {soldierCount}");

            if (soldierCount <= 0)
                continue;

            BattleSlot slot = new BattleSlot
            {
                troop = troop,
                soldierCount = soldierCount,
                slotIndex = slotIndex
            };
            _battleSlots.Add(slot);
        }

        if (_battleSlots.Count == 0)
        {
            Debug.LogWarning("[ArmySpawner] Savaþ için aktif slot bulunamadý, spawn yapýlmadý.");
            return false;
        }

        if (logDebug)
            Debug.Log($"[ArmySpawner] Toplam aktif battle slot sayýsý = {_battleSlots.Count}");

        return true;
    }

    private void ClearArmyRootChildren()
    {
        for (int i = armyRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(armyRoot.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Verilen unit için kaç satýr gerektiðini hesaplar.
    /// </summary>
    private int GetRowCount(int soldierCount)
    {
        if (unitRowCapacity <= 0)
            return 1;

        return Mathf.Max(1, Mathf.CeilToInt(soldierCount / (float)unitRowCapacity));
    }

    /// <summary>
    /// Max kolon sayýsýna göre unit geniþliðini hesaplar.
    /// (Askerler arasý spacing + sað/sol padding dahil)
    /// </summary>
    private float ComputeUnitWidth(int maxCols)
    {
        if (maxCols <= 1)
            return 2f * unitPaddingX;

        float contentWidth = (maxCols - 1) * soldierSpacingX; // askerlerin kapladýðý alan
        return contentWidth + 2f * unitPaddingX;
    }

    /// <summary>
    /// Satýr sayýsýna göre unit derinliðini hesaplar.
    /// (Satýrlar arasý spacing + ön/arka padding dahil)
    /// </summary>
    private float ComputeUnitDepth(int rows)
    {
        if (rows <= 1)
            return 2f * unitPaddingZ;

        float contentDepth = (rows - 1) * soldierSpacingZ;
        return contentDepth + 2f * unitPaddingZ;
    }

    /// <summary>
    /// Tek bir unit içindeki askerleri, verilen merkez etrafýnda spawn eder.
    /// </summary>
    private void SpawnUnitSoldiers(BattleSlot slot, Vector3 unitCenter, int rows)
    {
        int total = slot.soldierCount;
        if (total <= 0) return;

        int maxCols = Mathf.Min(unitRowCapacity, total);

        float width = ComputeUnitWidth(maxCols);
        float depth = ComputeUnitDepth(rows);

        // Ön sýra (row 0) düþmana en yakýn olan sýra olsun.
        // depth/2 - paddingZ ile ön kenardan içeri giriyoruz.
        float frontZ = unitCenter.z + (depth * 0.5f) - unitPaddingZ;

        int spawned = 0;

        for (int row = 0; row < rows; row++)
        {
            int remaining = total - spawned;
            if (remaining <= 0)
                break;

            int soldiersThisRow = Mathf.Min(unitRowCapacity, remaining);

            // Bu satýrýn kendi geniþliði (padding hariç)
            float rowWidth = (soldiersThisRow - 1) * soldierSpacingX;

            // X'te ortala: satýr merkezini unitCenter.x'e hizala
            float startX = unitCenter.x - rowWidth * 0.5f;

            // Bu satýrýn Z'i: ön kenardan baþlayýp her satýr için geriye doðru spacing kadar git
            float rowZ = frontZ - row * soldierSpacingZ;

            for (int col = 0; col < soldiersThisRow; col++)
            {
                float x = startX + col * soldierSpacingX;
                Vector3 pos = new Vector3(x, unitCenter.y, rowZ);

                Instantiate(slot.troop.prefab, pos, Quaternion.identity, armyRoot);
                spawned++;
            }
        }
    }

    #endregion

    #region Debug Drawing

    private void DrawUnitBoundsRect(Vector3 center, float width, float depth, string nameSuffix)
    {
        if (!drawUnitBounds || unitBoundsMaterial == null)
            return;

        // Basit bir Quad ile zemin üzerinde dikdörtgen çizelim
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = $"UnitBounds_{nameSuffix}";
        quad.transform.SetParent(armyRoot, false);

        // Yerden hafif yukarýda (flicker olmasýn diye)
        quad.transform.position = center + Vector3.up * 0.02f;
        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // yere paralel

        quad.transform.localScale = new Vector3(width, depth, 1f);

        var mr = quad.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sharedMaterial = unitBoundsMaterial;

        // Collider'a ihtiyacýmýz yok, silelim
        var col = quad.GetComponent<Collider>();
        if (col != null)
            Destroy(col);
    }

    #endregion
}
