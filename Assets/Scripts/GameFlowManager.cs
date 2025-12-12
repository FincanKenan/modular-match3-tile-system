using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    [Header("Roots")]
    [Tooltip("Taþ eþleþme tahtasýný tutan ana obje (Grid, UI vs.).")]
    public GameObject puzzleRoot;

    [Tooltip("Ordu/savaþ tarafýný tutan ana obje.")]
    public GameObject battleRoot;

    [Header("Camera Scripts")]
    public BoardCameraFitter boardCameraFitter;
    public ArmyCameraController armyCameraController;

    [Header("Battle Systems")]
    public MatchRewardCollector rewardCollector;
    public ArmySpawnerHorizontal armySpawner;

    void Start()
    {
        StartPuzzlePhase();
    }

    public void StartPuzzlePhase()
    {
        if (puzzleRoot) puzzleRoot.SetActive(true);
        if (battleRoot) battleRoot.SetActive(false);

        if (boardCameraFitter) boardCameraFitter.enabled = true;
        if (armyCameraController) armyCameraController.enabled = false;

        // Yeni puzzle baþlýyorsa eski ödülleri sýfýrla
        if (rewardCollector) rewardCollector.ResetRewards();
    }

    /// <summary>
    /// Örnek: süre bittiðinde, hamle bittiðinde veya bir butona bastýðýnda burayý çaðýr.
    /// 1) Puzzle fazý biter
    /// 2) Ordu spawn edilir
    /// 3) Kamera ArmyCameraController'a devredilir
    /// </summary>
    Camera _cam;

    void Awake()
    {
        if (boardCameraFitter != null)
            _cam = boardCameraFitter.GetComponent<Camera>();
        if (_cam == null && armyCameraController != null)
            _cam = armyCameraController.GetComponent<Camera>();
    }

    public void StartBattlePhase()
    {
        if (puzzleRoot) puzzleRoot.SetActive(false);
        if (battleRoot) battleRoot.SetActive(true);

        // --- KAMERA: BATTLE MODU ---
        if (_cam != null)
        {
            _cam.orthographic = false;
            _cam.fieldOfView = 40f;
        }

        if (boardCameraFitter)
            boardCameraFitter.enabled = false;

        if (armyCameraController)
            armyCameraController.enabled = true;

        // 1) Orduyu spawn et
        if (armySpawner)
            armySpawner.SpawnArmy();

       
    }



}
