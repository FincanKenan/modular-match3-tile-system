using UnityEngine;

/// <summary>
/// ArmyRoot altındaki bütün unitleri tarayıp
/// kamerayı onların etrafına otomatik ortalayan ve zoomlayan controller.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ArmyCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform armyRoot;   // Hierarchy'deki ArmyRoot

    [Header("Angles")]
    [SerializeField] private float yaw = 45f;      // Y ekseni etrafında dönüş
    [SerializeField] private float pitch = 50f;    // X ekseni etrafında yukarıdan bakış

    [Header("Distance / Zoom")]
    [Tooltip("Kameranın hedefe olan minimum uzaklığı (ya da orthoSize alt limiti).")]
    [SerializeField] private float minDistance = 8f;

    [Tooltip("Kameranın hedefe olan maksimum uzaklığı (ya da orthoSize üst limiti).")]
    [SerializeField] private float maxDistance = 30f;

    [Tooltip("Ordunun bounds'ına eklenecek ekstra boşluk.")]
    [SerializeField] private float boundsPadding = 2f;

    [Header("Smoothing")]
    [SerializeField] private float positionLerpSpeed = 4f;
    [SerializeField] private float rotationLerpSpeed = 4f;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (armyRoot == null)
        {
            Debug.LogWarning("[ArmyCameraController] ArmyRoot atanmamış.");
        }
    }

    private void LateUpdate()
    {
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        if (armyRoot == null)
            return;

        // 1) ArmyRoot altındaki bütün Renderer'ların bounds'ını hesapla
        if (!TryGetArmyBounds(out Bounds bounds))
        {
            // Eğer hiçbir renderer yoksa sadece ArmyRoot pozisyonuna bak
            bounds = new Bounds(armyRoot.position, Vector3.one * 5f);
        }

        // Bounds merkezini ve yarıçapını al
        Vector3 targetCenter = bounds.center;
        // XZ düzleminde en büyük yarıçap
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + boundsPadding;
        radius = Mathf.Max(radius, 1f); // güvenli alt limit

        if (_cam.orthographic)
        {
            UpdateOrthographicCamera(targetCenter, radius);
        }
        else
        {
            UpdatePerspectiveCamera(targetCenter, radius);
        }
    }

    /// <summary>
    /// ArmyRoot altındaki tüm Renderer'ları tarar, bounds çıkarmaya çalışır.
    /// </summary>
    private bool TryGetArmyBounds(out Bounds bounds)
    {
        bounds = new Bounds();

        // Sadece ArmyRoot altındaki Rendererlara bakıyoruz
        var renderers = armyRoot.GetComponentsInChildren<Renderer>();

        bool hasAny = false;
        foreach (var r in renderers)
        {
            if (!r.enabled)
                continue;

            if (!hasAny)
            {
                bounds = r.bounds;
                hasAny = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return hasAny;
    }

    private void UpdateOrthographicCamera(Vector3 targetCenter, float radius)
    {
        // Ortho kamerada radius, direkt orthoSize gibi kullanılabilir
        float targetSize = Mathf.Clamp(radius, minDistance, maxDistance);

        // Kamera rotasyonu
        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0f);

        // Kamera pozisyonu (distance sabit; ortho kamerada zoom, orthoSize'dan geliyor)
        float distance = maxDistance; // burada istersen sabit bir değer kullanabilirsin
        Vector3 offset = targetRot * (Vector3.back * distance);
        Vector3 targetPos = targetCenter + offset;

        // Lerp ile yumuşak geçiş
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * positionLerpSpeed
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationLerpSpeed
        );

        _cam.orthographicSize = Mathf.Lerp(
            _cam.orthographicSize,
            targetSize,
            Time.deltaTime * positionLerpSpeed
        );
    }

    private void UpdatePerspectiveCamera(Vector3 targetCenter, float radius)
    {
        // Perspektif kamerada FOV'a göre gerekli mesafeyi hesaplayalım
        float fovRad = _cam.fieldOfView * Mathf.Deg2Rad;
        // Yükseklik ekseni üzerinden hesap
        float distanceByHeight = radius / Mathf.Tan(fovRad * 0.5f);

        // Ekran oranına göre yatay eksen de devreye girebilir
        float aspect = _cam.aspect;
        float distanceByWidth = distanceByHeight / aspect;

        float targetDistance = Mathf.Max(distanceByHeight, distanceByWidth);
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

        // Kamera rotasyonu
        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0f);

        // Kamera pozisyonu
        Vector3 offset = targetRot * (Vector3.back * targetDistance);
        Vector3 targetPos = targetCenter + offset;

        // Lerp ile yumuşak geçiş
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * positionLerpSpeed
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationLerpSpeed
        );
    }
}
