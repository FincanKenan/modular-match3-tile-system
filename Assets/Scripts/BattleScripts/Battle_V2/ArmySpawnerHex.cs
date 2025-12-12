using System.Collections.Generic;
using UnityEngine;

public class ArmySpawnerHex : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleDeploymentState deploymentState;
    [SerializeField] private Transform armyRoot;

    /// <summary>Her slot için tek bir "unit root" GameObject tutuyoruz.</summary>
    private readonly Dictionary<BattleDeploymentState.HexDeploymentSlot, GameObject> _spawnedRoots
        = new Dictionary<BattleDeploymentState.HexDeploymentSlot, GameObject>();

    /// <summary>Hex merkezleri arasındaki minimum mesafe (formasyon için referans).</summary>
    private float _neighborDistance = 0f;

    [Header("Formation Settings")]
    [Tooltip("Yan yana iki asker arasındaki mesafe, neighborDistance * spacingFactor olarak hesaplanır.")]
    [SerializeField] private float spacingFactor = 0.15f;

    [Tooltip("Ön ve arka sıra arasındaki derinlik, neighborDistance * rowDepthFactor olarak hesaplanır.")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float rowDepthFactor = 0.2f;

    // maxCols değeri için center-out kolon sırası cache
    private readonly Dictionary<int, List<int>> _centerColumnOrders = new Dictionary<int, List<int>>();

    [Header("Unit Slot (Hex) Settings")]
    [Tooltip("Hex slotunun minimum scale çarpanı (1 = başlangıç boyutu).")]
    [SerializeField] private float minHexScaleMultiplier = 1f;

    [Tooltip("Hex slotunun maksimum scale çarpanı (güvenlik için limit).")]
    [SerializeField] private float maxHexScaleMultiplier = 1.6f;

    [Tooltip("Asker alanının üstüne eklenecek ekstra margin (neighborDistance yüzdesi).")]
    [Range(0.01f, 0.3f)]
    [SerializeField] private float hexMarginFactor = 0.08f;

    /// <summary>Her UnitSlotHex için başlangıç localScale değeri.</summary>
    private readonly Dictionary<UnitSlotHex, Vector3> _baseHexScale = new Dictionary<UnitSlotHex, Vector3>();

    /// <summary>Her UnitSlotHex için başlangıç world-space yarıçapı (X yönü).</summary>
    private readonly Dictionary<UnitSlotHex, float> _baseHexHalfSize = new Dictionary<UnitSlotHex, float>();

    [Header("Unit Separation Settings")]
    [Tooltip("Unitlerin birbirinden kaçma davranışı aktif mi?")]
    [SerializeField] private bool enableSeparation = true;

    [Tooltip("Separation iterasyon sayısı (2-4 arası genelde yeterli).")]
    [SerializeField] private int separationIterations = 3;

    [Tooltip("İtme kuvveti çarpanı.")]
    [SerializeField] private float separationStrength = 1f;

    [Tooltip("Her unit’in hex merkezinden en fazla uzaklaşabileceği mesafe, neighborDistance * maxOffsetFactor.")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float maxOffsetFactor = 0.25f;

    // Separation için geçici veri
    private struct UnitVisual
    {
        public GameObject root;
        public Vector3 basePos;
        public float radius;
    }

    private void Start()
    {
        Debug.Log("[ArmySpawnerHex] Start çağrıldı.");
        ComputeNeighborDistance();
        CacheHexBaseData();
        RefreshVisuals();
    }

    /// <summary>
    /// Hex slotlarının birbirine olan en küçük mesafesini bulur.
    /// Bunu formasyon için referans olarak kullanıyoruz.
    /// </summary>
    private void ComputeNeighborDistance()
    {
        if (deploymentState == null || deploymentState.slots == null ||
            deploymentState.slots.Count < 2)
        {
            _neighborDistance = 1f; // güvenli varsayılan
            return;
        }

        float minDist = float.MaxValue;
        var slots = deploymentState.slots;

        for (int i = 0; i < slots.Count; i++)
        {
            var a = slots[i];
            if (a == null || a.hexSlot == null) continue;

            Vector3 posA = a.hexSlot.transform.position;

            for (int j = i + 1; j < slots.Count; j++)
            {
                var b = slots[j];
                if (b == null || b.hexSlot == null) continue;

                Vector3 posB = b.hexSlot.transform.position;
                float dist = Vector3.Distance(posA, posB);

                if (dist > 0f && dist < minDist)
                    minDist = dist;
            }
        }

        if (minDist == float.MaxValue || minDist <= 0f)
            minDist = 1f;

        _neighborDistance = minDist;

        Debug.Log($"[ArmySpawnerHex] Neighbor distance hesaplandı: {_neighborDistance}");
    }

    /// <summary>Başlangıçta tüm hex slotlarının base boyutu ve scale’ini cache’ler.</summary>
    private void CacheHexBaseData()
    {
        if (deploymentState == null || deploymentState.slots == null)
            return;

        foreach (var slot in deploymentState.slots)
        {
            if (slot == null || slot.hexSlot == null) continue;

            var hex = slot.hexSlot;
            if (_baseHexScale.ContainsKey(hex))
                continue;

            _baseHexScale[hex] = hex.transform.localScale;

            var col2D = hex.GetComponent<BoxCollider2D>();
            if (col2D != null)
            {
                // World-space genişlik: size.x * lossyScale.x
                float worldWidth = col2D.size.x * hex.transform.lossyScale.x;
                float half = Mathf.Max(0.01f, worldWidth * 0.5f);
                _baseHexHalfSize[hex] = half;
            }
            else
            {
                // Tahmini bir başlangıç yarıçapı
                _baseHexHalfSize[hex] = _neighborDistance * 0.25f;
            }
        }
    }

    public void RefreshVisuals()
    {
        if (deploymentState == null || armyRoot == null)
        {
            Debug.LogWarning("[ArmySpawnerHex] Referanslar eksik.");
            return;
        }

        var slots = deploymentState.slots;
        if (slots == null)
        {
            Debug.LogWarning("[ArmySpawnerHex] deploymentState.slots = null");
            return;
        }

        if (_neighborDistance <= 0f)
            ComputeNeighborDistance();
        if (_baseHexScale.Count == 0)
            CacheHexBaseData();

        Debug.Log($"[ArmySpawnerHex] RefreshVisuals. Slot sayısı = {slots.Count}");

        // 0) Eski unit root'ları temizle
        foreach (var kvp in _spawnedRoots)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        _spawnedRoots.Clear();

        // Separation için liste
        List<UnitVisual> visuals = new List<UnitVisual>();

        // 1) Dolu slotlar için unit root + askerleri oluştur
        foreach (var slot in slots)
        {
            if (slot == null) continue;

            if (slot.assignedTroop == null || slot.soldierCount <= 0)
            {
                ResetHexScale(slot.hexSlot);
                continue;
            }

            if (slot.assignedTroop.prefab == null)
            {
                Debug.LogWarning($"[ArmySpawnerHex] {slot.assignedTroop.displayName} için prefab atanmadı.");
                ResetHexScale(slot.hexSlot);
                continue;
            }

            Vector3 basePos = slot.hexSlot.transform.position;

            int level = Mathf.Clamp(slot.assignedTroop.level, 1, 3);
            int maxCols = GetMaxColumnsForLevel(level);      // 1→10, 2→6, 3→4
            int maxCapacity = GetMaxCapacityForLevel(level); // 1→20, 2→12, 3→8

            int soldierCount = Mathf.Clamp(slot.soldierCount, 1, maxCapacity);

            // --- UNIT ROOT ---
            GameObject unitRoot = new GameObject(
                $"Unit_{slot.assignedTroop.displayName}_Hex_{slot.hexSlot.index}");
            unitRoot.transform.SetParent(armyRoot);
            unitRoot.transform.position = basePos;

            // Formasyon parametreleri
            float step = _neighborDistance * spacingFactor;             // yan yana asker mesafesi
            float origin = -(maxCols - 1) * 0.5f * step;                // en sol kolonun offset'i
            float zOffset = _neighborDistance * rowDepthFactor;         // ön/arka sıra derinliği

            // Ön ve arka sıra için kaç asker?
            int frontCount = Mathf.Min(soldierCount, maxCols);
            int backCount = Mathf.Clamp(soldierCount - frontCount, 0, maxCols);

            // Center-out kolon sırası
            List<int> colOrder = GetCenterColumnOrder(maxCols);

            // Bounds için
            bool hasBounds = false;
            Vector3 localMin = Vector3.zero;
            Vector3 localMax = Vector3.zero;

            // ÖN SIRA (rowIndex=0, z=+zOffset)
            SpawnRow(slot, unitRoot.transform, rowIndex: 0, count: frontCount,
                     maxCols: maxCols, colOrder: colOrder, origin: origin,
                     step: step, zOffset: +zOffset,
                     ref hasBounds, ref localMin, ref localMax);

            // ARKA SIRA (rowIndex=1, z=-zOffset)
            SpawnRow(slot, unitRoot.transform, rowIndex: 1, count: backCount,
                     maxCols: maxCols, colOrder: colOrder, origin: origin,
                     step: step, zOffset: -zOffset,
                     ref hasBounds, ref localMin, ref localMax);

            float unitRadius = _neighborDistance * 0.3f; // güvenli varsayılan

            // Bounds'a göre hex scale ve unit radius ayarla
            if (hasBounds)
            {
                Vector3 size = localMax - localMin;
                float halfX = Mathf.Max(0.01f, size.x * 0.5f);
                float halfZ = Mathf.Max(0.01f, size.z * 0.5f);
                float soldierRadius = Mathf.Max(halfX, halfZ);

                // Hex'i büyüt
                UpdateHexScale(slot.hexSlot, soldierRadius);

                // Separation için unit yarıçapı (asker alanı + margin)
                float margin = _neighborDistance * hexMarginFactor;
                unitRadius = soldierRadius + margin;
            }
            else
            {
                ResetHexScale(slot.hexSlot);
            }

            // Separation listesine ekle
            visuals.Add(new UnitVisual
            {
                root = unitRoot,
                basePos = basePos,
                radius = unitRadius
            });

            _spawnedRoots[slot] = unitRoot;

            Debug.Log($"[ArmySpawnerHex] Slot hexIndex={slot.hexSlot.index}, " +
                      $"troop={slot.assignedTroop.displayName}, count={slot.soldierCount}, " +
                      $"maxCapacity={maxCapacity}");
        }

        // 2) Unitler arası separation uygula
        if (enableSeparation)
        {
            ApplySeparation(visuals);
        }
    }

    /// <summary>
    /// Belirli bir satır için, askerleri center-out kolon sırasına göre spawn eder
    /// ve bounds günceller.
    /// </summary>
    private void SpawnRow(
        BattleDeploymentState.HexDeploymentSlot slot,
        Transform unitRoot,
        int rowIndex,
        int count,
        int maxCols,
        List<int> colOrder,
        float origin,
        float step,
        float zOffset,
        ref bool hasBounds,
        ref Vector3 localMin,
        ref Vector3 localMax)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            int col = colOrder[i];                 // center-out kolon index
            float x = origin + col * step;
            float z = zOffset;

            Vector3 localPos = new Vector3(x, 0f, z);

            var go = Object.Instantiate(slot.assignedTroop.prefab, unitRoot);
            go.name = $"{slot.assignedTroop.displayName}_row{rowIndex}_seat_{i + 1}";
            go.transform.localPosition = localPos;

            UpdateLocalBounds(localPos, ref hasBounds, ref localMin, ref localMax);
        }
    }

    private void UpdateLocalBounds(
        Vector3 localPos,
        ref bool hasBounds,
        ref Vector3 localMin,
        ref Vector3 localMax)
    {
        if (!hasBounds)
        {
            hasBounds = true;
            localMin = localPos;
            localMax = localPos;
        }
        else
        {
            localMin = Vector3.Min(localMin, localPos);
            localMax = Vector3.Max(localMax, localPos);
        }
    }

    /// <summary>
    /// Askerlerin kapladığı alana göre UnitSlotHex'in scale'ini büyütür.
    /// </summary>
    private void UpdateHexScale(UnitSlotHex hex, float soldierRadius)
    {
        if (hex == null || !_baseHexScale.ContainsKey(hex) || !_baseHexHalfSize.ContainsKey(hex))
            return;

        Vector3 baseScale = _baseHexScale[hex];
        float baseHalf = _baseHexHalfSize[hex];

        float margin = _neighborDistance * hexMarginFactor;
        float targetHalf = soldierRadius + margin;

        targetHalf = Mathf.Max(baseHalf * minHexScaleMultiplier, targetHalf);

        float maxHalfNeighbor = _neighborDistance * 0.5f * 0.9f;
        targetHalf = Mathf.Min(targetHalf, maxHalfNeighbor);

        float maxHalfByScale = baseHalf * maxHexScaleMultiplier;
        targetHalf = Mathf.Min(targetHalf, maxHalfByScale);

        float scaleFactor = (baseHalf > 0f) ? targetHalf / baseHalf : 1f;
        hex.transform.localScale = baseScale * scaleFactor;
    }

    /// <summary>Hiç asker yoksa hex scale’ini başlangıç değerine döndürür.</summary>
    private void ResetHexScale(UnitSlotHex hex)
    {
        if (hex == null)
            return;

        if (_baseHexScale.TryGetValue(hex, out var baseScale))
        {
            hex.transform.localScale = baseScale;
        }
    }

    /// <summary>
    /// Unitler arası basit separation uygular. Sadece görsel unitRoot pozisyonlarını kaydırır.
    /// </summary>
    private void ApplySeparation(List<UnitVisual> visuals)
    {
        if (visuals == null || visuals.Count <= 1)
            return;

        int count = visuals.Count;
        Vector3[] offsets = new Vector3[count];

        float maxOffset = _neighborDistance * maxOffsetFactor;

        for (int iter = 0; iter < separationIterations; iter++)
        {
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    Vector3 posI = visuals[i].basePos + offsets[i];
                    Vector3 posJ = visuals[j].basePos + offsets[j];

                    // XZ düzleminde hesapla
                    Vector2 pi = new Vector2(posI.x, posI.z);
                    Vector2 pj = new Vector2(posJ.x, posJ.z);

                    Vector2 diff = pj - pi;
                    float dist = diff.magnitude;

                    float minDist = visuals[i].radius + visuals[j].radius;

                    if (dist <= 0.0001f)
                    {
                        // Aynı noktadaysa rastgele azıcık ayır
                        diff = new Vector2(0.001f, 0f);
                        dist = diff.magnitude;
                    }

                    if (dist < minDist)
                    {
                        Vector2 dir = diff / dist;
                        float push = (minDist - dist) * 0.5f * separationStrength;

                        Vector2 pushVec = dir * push;

                        // i sola, j sağa doğru itilsin (eşit paylaşım)
                        offsets[i] -= new Vector3(pushVec.x, 0f, pushVec.y);
                        offsets[j] += new Vector3(pushVec.x, 0f, pushVec.y);
                    }
                }
            }

            // Her iterasyon sonunda offsetleri clamp et
            for (int i = 0; i < count; i++)
            {
                if (offsets[i].sqrMagnitude > maxOffset * maxOffset)
                {
                    offsets[i] = offsets[i].normalized * maxOffset;
                }
            }
        }

        // Son pozisyonları uygula
        for (int i = 0; i < count; i++)
        {
            if (visuals[i].root == null) continue;

            Vector3 finalPos = visuals[i].basePos + offsets[i];
            visuals[i].root.transform.position = finalPos;
        }
    }

    /// <summary>
    /// maxCols için center-out kolon sırasını döner:
    /// Örnek: maxCols=10 → 4,5,3,6,2,7,1,8,0,9
    ///        maxCols=6  → 2,3,1,4,0,5
    ///        maxCols=4  → 1,2,0,3
    /// </summary>
    private List<int> GetCenterColumnOrder(int maxCols)
    {
        if (maxCols <= 0) maxCols = 1;

        if (_centerColumnOrders.TryGetValue(maxCols, out var cached))
            return cached;

        var order = new List<int>(maxCols);

        if (maxCols == 1)
        {
            order.Add(0);
        }
        else if (maxCols % 2 == 0)
        {
            // ÇİFT kolon sayısı: iki orta kolon var (left, right)
            int left = maxCols / 2 - 1;
            int right = maxCols / 2;

            while (order.Count < maxCols)
            {
                if (left >= 0)
                {
                    order.Add(left);
                    left--;
                }
                if (order.Count >= maxCols) break;

                if (right < maxCols)
                {
                    order.Add(right);
                    right++;
                }
            }
        }
        else
        {
            int center = maxCols / 2;
            order.Add(center);

            int offset = 1;
            while (order.Count < maxCols)
            {
                int left = center - offset;
                int right = center + offset;

                if (left >= 0)
                    order.Add(left);
                if (order.Count >= maxCols) break;

                if (right < maxCols)
                    order.Add(right);

                offset++;
            }
        }

        _centerColumnOrders[maxCols] = order;
        return order;
    }

    /// <summary>
    /// Seviye 1 → 10 kolon, seviye 2 → 6 kolon, seviye 3 → 4 kolon.
    /// </summary>
    private int GetMaxColumnsForLevel(int level)
    {
        switch (level)
        {
            case 1: return 10;
            case 2: return 6;
            case 3: return 4;
            default: return 10;
        }
    }

    /// <summary>
    /// BattleDeploymentState içindeki kapasite değerlerini kullanıyoruz:
    /// 1.seviye → 20, 2.seviye → 12, 3.seviye → 8.
    /// </summary>
    private int GetMaxCapacityForLevel(int level)
    {
        if (deploymentState == null) return 0;

        switch (level)
        {
            case 1: return deploymentState.level1Capacity;
            case 2: return deploymentState.level2Capacity;
            case 3: return deploymentState.level3Capacity;
            default: return deploymentState.level1Capacity;
        }
    }
}
