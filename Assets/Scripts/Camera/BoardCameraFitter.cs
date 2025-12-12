using UnityEngine;

/// <summary>
/// GridManager içindeki width/height'a göre kamerayý otomatik
/// merkezler ve tüm tahtayý ekrana sýðdýrýr.
/// Hem Orthographic hem Perspective kamerayla çalýþýr.
/// </summary>
[RequireComponent(typeof(Camera))]
public class BoardCameraFitter : MonoBehaviour
{
    [Header("References")]
    public GridManager grid;                 // Tahtayý üreten script

    [Header("Board Settings")]
    [Tooltip("Dünyadaki 1 karenin kenar uzunluðu (unit). Genelde 1.")]
    public float cellSize = 1f;
    [Tooltip("Tahta çevresine eklenecek boþluk (unit).")]
    public float padding = 0.5f;
    [Tooltip("Tahta z-ekseni (dünyada hangi derinlikte kurulu?). Genelde 0.")]
    public float boardZ = 0f;

    [Header("Camera Settings")]
    [Tooltip("Ortho kamerada minimum size sýnýrý.")]
    public float minOrthoSize = 2f;
    [Tooltip("Perspective kamerada min uzaklýk sýnýrý.")]
    public float minPerspDistance = 5f;
    [Tooltip("Kameranýn tahtaya bakýþ açýsý (saf 2D için 0,0,-1 ile bakýn).")]
    public Vector3 lookDirection = new Vector3(0, 0, -1);

    [Header("Update")]
    [Tooltip("Start'ta otomatik hizala.")]
    public bool fitOnStart = true;
    [Tooltip("Her kare kontrol edip boyut deðiþtiyse yeniden hizala.")]
    public bool autoFollow = true;

    private Camera cam;
    private int lastW = -1, lastH = -1;
    private float lastAspect = -1f;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (fitOnStart) FitNow();
    }

    void LateUpdate()
    {
        if (!autoFollow || grid == null) return;

        // Boyutlar veya ekran oraný deðiþtiyse tekrar hizala
        if (grid.width != lastW || grid.height != lastH || Mathf.Abs(cam.aspect - lastAspect) > 0.0001f)
            FitNow();
    }

    /// <summary>
    /// Kamerayý tahtaya göre merkezler ve sýðdýrýr.
    /// Grid üretimi (GenerateGrid) bittiði an güvenle çaðrýlabilir.
    /// </summary>
    public void FitNow()
    {
        if (grid == null || grid.width <= 0 || grid.height <= 0)
            return;

        lastW = grid.width;
        lastH = grid.height;
        lastAspect = cam.aspect;

        // --- Tahta boyutlarý (world) ---
        float boardW = lastW * cellSize;
        float boardH = lastH * cellSize;

        // Tahta merkezi (dünya koordinatý)
        Vector3 boardCenter = new Vector3(
            (lastW - 1) * 0.5f * cellSize,
            (lastH - 1) * 0.5f * cellSize,
            boardZ
        );

        // Kameranýn bakýþ yönünü normalize et (0,0,-1 gibi)
        Vector3 dir = lookDirection.sqrMagnitude < 0.0001f ? Vector3.back : lookDirection.normalized;

        if (cam.orthographic)
        {
            // Ortho: dikey yarýçap (size) ve yatay için aspect bazlý sýnýr
            float halfH = boardH * 0.5f + padding;
            float halfW = boardW * 0.5f + padding;

            // Ekrana sýðdýrmak için gerekli orthoSize = max(halfH, halfW/aspect)
            float targetSize = Mathf.Max(halfH, halfW / cam.aspect);
            cam.orthographicSize = Mathf.Max(minOrthoSize, targetSize);

            // Pozisyonu tam merkeze, bakýþ yönünde geri çek
            // Ortho için Z ayrýdýr; sadece merkez + look dir'e göre offset
            float camDistance = 10f; // Ortho’da önemsiz; clipping için negatif Z’ye çekiyoruz
            Vector3 camPos = boardCenter - dir * camDistance;
            cam.transform.position = camPos;
            cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
        else
        {
            // Perspective: FOV'dan gereken uzaklýðý hesapla (hem dikey hem yatay için)
            // Dikey yarý ölçü
            float halfH = boardH * 0.5f + padding;
            // Yatay yarý ölçü
            float halfW = boardW * 0.5f + padding;

            // FOV radyan
            float vFovRad = cam.fieldOfView * Mathf.Deg2Rad;
            // Dikey için gereken uzaklýk
            float distV = halfH / Mathf.Tan(vFovRad * 0.5f);

            // Yatay FOV (aspect'tan türet)
            float hFovRad = 2f * Mathf.Atan(Mathf.Tan(vFovRad * 0.5f) * cam.aspect);
            float distH = halfW / Mathf.Tan(hFovRad * 0.5f);

            float targetDist = Mathf.Max(distV, distH, minPerspDistance);

            // Pozisyon: merkezin lookDirection tersi yönde targetDist kadar
            Vector3 camPos = boardCenter - dir * targetDist;
            cam.transform.position = camPos;
            cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // Near/Far clipping ayarý (opsiyonel)
            float near = Mathf.Max(0.1f, targetDist - 50f);
            float far = targetDist + 50f;
            cam.nearClipPlane = near;
            cam.farClipPlane = far;
        }
    }

    // Ýstersen runtime’da grid referansýný set edip hemen hizala
    public void Attach(GridManager g, bool fitImmediately = true)
    {
        grid = g;
        if (fitImmediately) FitNow();
    }
}
