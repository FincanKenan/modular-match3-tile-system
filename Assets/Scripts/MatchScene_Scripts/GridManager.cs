using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    // ============================================================
    //  MATCH INFO SİSTEMİ
    // ============================================================

    public enum MatchKind
    {
        Special_T,
        Special_L3x3,
        Special_U,
        Special_S,

        Plus5,
        Square2x2,
        L3x2,

        Line3,
        Line4,
        Line5,
        
    }

    public struct MatchInfo
    {
        public MatchKind kind;        // Hangi tip eşleşme
        public PieceType pieceType;   // Hangi taştan oluştu
        public int tileCount;         // Kaç taş
        public Vector2Int center;     // Tahtadaki yaklaşık merkez

        public MatchInfo(MatchKind kind, PieceType pieceType, List<Tile> tiles)
        {
            this.kind = kind;
            this.pieceType = pieceType;
            this.tileCount = (tiles != null) ? tiles.Count : 0;

            if (tiles != null && tiles.Count > 0)
            {
                int sx = 0, sy = 0;
                foreach (var t in tiles)
                {
                    sx += t.x;
                    sy += t.y;
                }
                center = new Vector2Int(sx / tiles.Count, sy / tiles.Count);
            }
            else
            {
                center = Vector2Int.zero;
            }
        }
    }

    // Her eşleşme çözüldüğünde (taşlar silinmeden hemen önce) tetiklenecek event
    public System.Action<MatchInfo> OnMatchResolved;


    // ============================================================
    //  ESKİ ALANLAR (AYNEN KALDI)
    // ============================================================

    [Header("Grid")]
    public int width = 8;
    public int height = 8;
    public GameObject tilePrefab;
    public Transform tileParent;
    public float cellSize = 1f;

    [Header("Pieces")]
    public List<PieceType> pieceTypes;

    [Header("Timings (Inspector)")]
    public float swapAnimTime = 0.12f;
    public float preMatchDelay = 0.18f;
    public float postClearDelay = 0.05f;
    public float betweenCascadeDelay = 0.10f;
    public float fallAnimSpeed = 6f;

    [Header("Session Limits (Inspector)")]
    public float levelTime = 60f;
    public int moveLimit = 5;

    [Header("Flow")]
    public GameFlowManager gameFlow;   // Inspector'dan bağlayacağız

    private bool battleStarted = false;   // Savaşa bir kere geçmek için


    [HideInInspector] public float timeLeft;
    [HideInInspector] public int movesLeft;

    public bool IsBusy { get; private set; } = false;
    [HideInInspector] public bool lastSwapCreatedMatch = false;

    [HideInInspector] public Tile[,] grid;
    int _activeMoves;

    void Start()
    {
        if (!tileParent)
        {
            var go = new GameObject("TileParent");
            tileParent = go.transform;
        }

        GenerateBoard_Playable_NoAutoMatches();
        StartSession(levelTime, moveLimit);
    }

    void Update()
    {
        // Süreyi akıt
        if (timeLeft > 0f)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0f) timeLeft = 0f;
        }

        // >>> SAVAŞA GEÇİŞ KONTROLÜ
        if (!battleStarted)
        {
            bool timeOver = (timeLeft <= 0f);
            bool movesOver = (movesLeft <= 0);

            // Board şu anda animasyon/match çözmüyorsa ve
            // süre veya hamle bittiyse battle'a geç
            if ((timeOver || movesOver) && !IsBusy)
            {
                battleStarted = true;

                if (gameFlow != null)
                {
                    Debug.Log("[GridManager] Faz değişimi: Battle'a geçiliyor.");
                    gameFlow.StartBattlePhase();
                }
                else
                {
                    Debug.LogWarning("[GridManager] GameFlowManager referansı atanmamış!");
                }
            }
        }
    }


    public void StartSession(float seconds, int moves)
    {
        timeLeft = Mathf.Max(0f, seconds);
        movesLeft = Mathf.Max(0, moves);
        IsBusy = false;
        lastSwapCreatedMatch = false;

        battleStarted = false;   // <<< ekle: yeni oyun başlarken resetle
    }


    public bool CanPlayerInteract()
    {
        return timeLeft > 0f && movesLeft > 0 && !IsBusy;
    }

    public void ConsumeMove()
    {
        if (movesLeft > 0) movesLeft--;
    }

    // -------------------- BOARD BUILD / SHUFFLE --------------------
    void GenerateBoard_Playable_NoAutoMatches()
    {
        int safety = 0;
        do
        {
            safety++;
            GenerateGrid_Random();
        }
        while ((HasAnySpecialMatches() || !HasAnyAvailableMove()) && safety < 200);
    }

    void GenerateGrid_Random()
    {
        for (int i = tileParent.childCount - 1; i >= 0; i--)
            Destroy(tileParent.GetChild(i).gameObject);

        grid = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var go = Instantiate(tilePrefab, GridToWorld(x, y), Quaternion.identity, tileParent);
                var t = go.GetComponent<Tile>();
                t.x = x; t.y = y;
                grid[x, y] = t;

                t.SetPiece(GetRandomPieceWeighted());
            }
        }
    }

    void ShuffleUntilPlayable_NoAutoMatches()
    {
        int guard = 0;
        while (true)
        {
            guard++;
            Debug.Log("[Shuffle] Hamle yok, karıştırılıyor...");
            GenerateGrid_Random();

            if (!HasAnySpecialMatches() && HasAnyAvailableMove())
                break;

            if (guard > 400) break;
        }
    }

    // -------------------------- UTILS ------------------------------
    public Vector3 GridToWorld(int x, int y) => new Vector3(x * cellSize, y * cellSize, 0f);
    bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

    PieceType GetRandomPieceWeighted()
    {
        float total = 0f;
        for (int i = 0; i < pieceTypes.Count; i++)
            total += Mathf.Max(0f, pieceTypes[i].spawnWeight);

        float r = Random.value * total;
        for (int i = 0; i < pieceTypes.Count; i++)
        {
            r -= Mathf.Max(0f, pieceTypes[i].spawnWeight);
            if (r <= 0f) return pieceTypes[i];
        }
        return pieceTypes[pieceTypes.Count - 1];
    }

    bool CanMatch(PieceType p) => p != null && !p.isSpecial;
    bool SameMatchType(PieceType a, PieceType b) => CanMatch(a) && a == b;

    // ----------------------- SWAP & FLOW ---------------------------
    public IEnumerator TrySwapAndResolve(Tile a, Tile b)
    {
        if (!CanPlayerInteract()) yield break;
        if (a == null || b == null) yield break;
        if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1) yield break;

        IsBusy = true;
        lastSwapCreatedMatch = false;

        // görsel swap
        yield return StartCoroutine(AnimateSwap(a, b));

        // ÖZEL TAŞ KONTROLÜ
        bool aSpecial = a.pieceType != null && a.pieceType.isSpecial;
        bool bSpecial = b.pieceType != null && b.pieceType.isSpecial;

        if (aSpecial || bSpecial)
        {
            if (aSpecial) a.ClearPiece();
            if (bSpecial) b.ClearPiece();

            lastSwapCreatedMatch = true;
            if (preMatchDelay > 0f) yield return new WaitForSeconds(preMatchDelay);

            yield return StartCoroutine(ResolveBoardCascading());

            // >>> SADECE GERÇEK MATCH OLUŞTUYSA HAMLE EKSİLT
            if (lastSwapCreatedMatch)
                ConsumeMove();

            IsBusy = false;
            yield break;
        }

        // NORMAL FLOW
        if (!HasAnySpecialMatches())
        {
            // eşleşme yoksa geri al
            yield return StartCoroutine(AnimateSwap(a, b));
            IsBusy = false;
            yield break;
        }

        lastSwapCreatedMatch = true;
        if (preMatchDelay > 0f) yield return new WaitForSeconds(preMatchDelay);
        yield return StartCoroutine(ResolveBoardCascading());

        // >>> burada da, match olduğu kesin; hamle düş
        if (lastSwapCreatedMatch)
            ConsumeMove();

        IsBusy = false;
    }


    IEnumerator AnimateSwap(Tile a, Tile b)
    {
        var ta = grid[a.x, a.y];
        var tb = grid[b.x, b.y];
        grid[a.x, a.y] = tb;
        grid[b.x, b.y] = ta;
        (a.x, b.x) = (b.x, a.x);
        (a.y, b.y) = (b.y, a.y);

        Vector3 posA = GridToWorld(a.x, a.y);
        Vector3 posB = GridToWorld(b.x, b.y);
        float t = 0f;
        Vector3 startA = a.transform.position;
        Vector3 startB = b.transform.position;

        while (t < 1f)
        {
            t += Time.deltaTime / swapAnimTime;
            a.transform.position = Vector3.Lerp(startA, posA, t);
            b.transform.position = Vector3.Lerp(startB, posB, t);
            yield return null;
        }
        a.transform.position = posA;
        b.transform.position = posB;
    }

    // ---------------- PATTERN DEFINITIONS --------------------------
    struct SpecialMatch
    {
        public int count;
        public List<Tile> tiles;
        public string label;
        public string orientation;   // "yan" / "dik" (sadece çizgilerde)
    }

    // T–Shape (5 taş, 4 yön) – merkez (x,y) – ÖZEL (enableTShape5)
    bool CheckSwordTShape5(int x, int y, out List<Tile> tiles)
    {
        tiles = null;

        if (!InBounds(x, y))
            return false;

        var centerType = grid[x, y].pieceType;

        if (!(CanMatch(centerType) && centerType.enableTShape5))
            return false;

        // YUKARI
        if (InBounds(x, y + 1) && InBounds(x, y + 2) &&
            InBounds(x - 1, y) && InBounds(x + 1, y))
        {
            if (SameMatchType(grid[x, y + 1].pieceType, centerType) &&
                SameMatchType(grid[x, y + 2].pieceType, centerType) &&
                SameMatchType(grid[x - 1, y].pieceType, centerType) &&
                SameMatchType(grid[x + 1, y].pieceType, centerType))
            {
                tiles = new List<Tile>
                {
                    grid[x, y],
                    grid[x, y + 1],
                    grid[x, y + 2],
                    grid[x - 1, y],
                    grid[x + 1, y],
                };
                Debug.Log($"[T-Shape] UP center=({x},{y})");
                return true;
            }
        }

        // AŞAĞI
        if (InBounds(x, y - 1) && InBounds(x, y - 2) &&
            InBounds(x - 1, y) && InBounds(x + 1, y))
        {
            if (SameMatchType(grid[x, y - 1].pieceType, centerType) &&
                SameMatchType(grid[x, y - 2].pieceType, centerType) &&
                SameMatchType(grid[x - 1, y].pieceType, centerType) &&
                SameMatchType(grid[x + 1, y].pieceType, centerType))
            {
                tiles = new List<Tile>
                {
                    grid[x, y],
                    grid[x, y - 1],
                    grid[x, y - 2],
                    grid[x - 1, y],
                    grid[x + 1, y],
                };
                Debug.Log($"[T-Shape] DOWN center=({x},{y})");
                return true;
            }
        }

        // SAĞ
        if (InBounds(x + 1, y) && InBounds(x + 2, y) &&
            InBounds(x, y - 1) && InBounds(x, y + 1))
        {
            if (SameMatchType(grid[x + 1, y].pieceType, centerType) &&
                SameMatchType(grid[x + 2, y].pieceType, centerType) &&
                SameMatchType(grid[x, y - 1].pieceType, centerType) &&
                SameMatchType(grid[x, y + 1].pieceType, centerType))
            {
                tiles = new List<Tile>
                {
                    grid[x, y],
                    grid[x + 1, y],
                    grid[x + 2, y],
                    grid[x, y - 1],
                    grid[x, y + 1],
                };
                Debug.Log($"[T-Shape] RIGHT center=({x},{y})");
                return true;
            }
        }

        // SOL
        if (InBounds(x - 1, y) && InBounds(x - 2, y) &&
            InBounds(x, y - 1) && InBounds(x, y + 1))
        {
            if (SameMatchType(grid[x - 1, y].pieceType, centerType) &&
                SameMatchType(grid[x - 2, y].pieceType, centerType) &&
                SameMatchType(grid[x, y - 1].pieceType, centerType) &&
                SameMatchType(grid[x, y + 1].pieceType, centerType))
            {
                tiles = new List<Tile>
                {
                    grid[x, y],
                    grid[x - 1, y],
                    grid[x - 2, y],
                    grid[x, y - 1],
                    grid[x, y + 1],
                };
                Debug.Log($"[T-Shape] LEFT center=({x},{y})");
                return true;
            }
        }

        return false;
    }

    // Artı (+) şekli, 5 taş – merkez (x,y) – NORMAL (tüm taşlar)
    bool CheckPlusShape5(int x, int y, out List<Tile> tiles)
    {
        tiles = null;

        if (!InBounds(x, y))
            return false;

        var centerType = grid[x, y].pieceType;
        if (!CanMatch(centerType))
            return false;

        int[][] offsets =
        {
            new[] { 0, 0 },  // merkez
            new[] { 0, 1 },  // üst
            new[] { 0, -1 }, // alt
            new[] { 1, 0 },  // sağ
            new[] { -1, 0 }  // sol
        };

        var list = new List<Tile>(5);

        foreach (var o in offsets)
        {
            int xx = x + o[0];
            int yy = y + o[1];

            if (!InBounds(xx, yy))
                return false;

            if (!SameMatchType(grid[xx, yy].pieceType, centerType))
                return false;

            list.Add(grid[xx, yy]);
        }

        tiles = list;
        Debug.Log($"[PlusShape] center=({x},{y})");
        return true;
    }

    // 3x2 / 2x3 L-Shape (4 taş) – NORMAL
    bool CheckL4_H3V2(int x, int y, PieceType p, int dirH, int dirV, out List<Tile> tiles)
    {
        tiles = null;
        int x1 = x + dirH;
        int x2 = x + 2 * dirH;
        int y1 = y + dirV;

        if (!InBounds(x, y) || !InBounds(x1, y) || !InBounds(x2, y) || !InBounds(x, y1))
            return false;

        if (!SameMatchType(grid[x, y].pieceType, p) ||
            !SameMatchType(grid[x1, y].pieceType, p) ||
            !SameMatchType(grid[x2, y].pieceType, p) ||
            !SameMatchType(grid[x, y1].pieceType, p))
            return false;

        tiles = new List<Tile> { grid[x, y], grid[x1, y], grid[x2, y], grid[x, y1] };
        return true;
    }

    bool CheckL4_V3H2(int x, int y, PieceType p, int dirV, int dirH, out List<Tile> tiles)
    {
        tiles = null;
        int y1 = y + dirV;
        int y2 = y + 2 * dirV;
        int x1 = x + dirH;

        if (!InBounds(x, y) || !InBounds(x, y1) || !InBounds(x, y2) || !InBounds(x1, y))
            return false;

        if (!SameMatchType(grid[x, y].pieceType, p) ||
            !SameMatchType(grid[x, y1].pieceType, p) ||
            !SameMatchType(grid[x, y2].pieceType, p) ||
            !SameMatchType(grid[x1, y].pieceType, p))
            return false;

        tiles = new List<Tile> { grid[x, y], grid[x, y1], grid[x, y2], grid[x1, y] };
        return true;
    }

    // 3x3 L-Shape (5 taş, 4 yön) – ÖZEL (enableLShape3x3)
    bool CheckL5_H3V3(int x, int y, PieceType p, int dirH, int dirV, out List<Tile> tiles)
    {
        tiles = null;

        if (!(CanMatch(p) && p.enableLShape3x3))
            return false;

        int x1 = x + dirH;
        int x2 = x + 2 * dirH;
        int y1 = y + dirV;
        int y2 = y + 2 * dirV;

        if (!InBounds(x, y) || !InBounds(x1, y) || !InBounds(x2, y) ||
            !InBounds(x, y1) || !InBounds(x, y2))
            return false;

        if (!SameMatchType(grid[x, y].pieceType, p) ||
            !SameMatchType(grid[x1, y].pieceType, p) ||
            !SameMatchType(grid[x2, y].pieceType, p) ||
            !SameMatchType(grid[x, y1].pieceType, p) ||
            !SameMatchType(grid[x, y2].pieceType, p))
            return false;

        tiles = new List<Tile>
        {
            grid[x, y], grid[x1, y], grid[x2, y],
            grid[x, y1], grid[x, y2]
        };
        return true;
    }

    // U-Shape (3x2 / 2x3, ortası boş, 5 taş, 4 yön) – ÖZEL (enableUShape5)
    bool CheckUShape5(int x, int y, PieceType p, out List<Tile> tiles)
    {
        tiles = null;

        if (!(CanMatch(p) && p.enableUShape5))
            return false;

        bool TryPattern(int baseX, int baseY, int[][] offsets, int holeDx, int holeDy, out List<Tile> outTiles)
        {
            outTiles = null;

            // hole pozisyonunda aynı tip varsa U değil
            int hx = baseX + holeDx;
            int hy = baseY + holeDy;
            if (InBounds(hx, hy) && SameMatchType(grid[hx, hy].pieceType, p))
                return false;

            var list = new List<Tile>(5);
            foreach (var o in offsets)
            {
                int xx = baseX + o[0];
                int yy = baseY + o[1];

                if (!InBounds(xx, yy))
                    return false;

                if (!SameMatchType(grid[xx, yy].pieceType, p))
                    return false;

                list.Add(grid[xx, yy]);
            }

            outTiles = list;
            return true;
        }

        // Yukarı bakan U – bottom-left (x,y)
        int[][] upOffsets =
        {
            new[] { 0, 1 }, new[] { 1, 1 }, new[] { 2, 1 },
            new[] { 0, 0 }, new[] { 2, 0 }
        };

        // Aşağı bakan U
        int[][] downOffsets =
        {
            new[] { 0, 0 }, new[] { 1, 0 }, new[] { 2, 0 },
            new[] { 0, 1 }, new[] { 2, 1 }
        };

        // Sağa bakan U
        int[][] rightOffsets =
        {
            new[] { 0, 0 }, new[] { 1, 0 },
            new[] { 1, 1 },
            new[] { 0, 2 }, new[] { 1, 2 }
        };

        // Sola bakan U
        int[][] leftOffsets =
        {
            new[] { 0, 0 }, new[] { 1, 0 },
            new[] { 0, 1 },
            new[] { 0, 2 }, new[] { 1, 2 }
        };

        if (TryPattern(x, y, upOffsets, 1, 0, out tiles))
        {
            Debug.Log($"[U-Shape] UP bottomLeft=({x},{y})");
            return true;
        }

        if (TryPattern(x, y, downOffsets, 1, 1, out tiles))
        {
            Debug.Log($"[U-Shape] DOWN bottomLeft=({x},{y})");
            return true;
        }

        if (TryPattern(x, y, rightOffsets, 0, 1, out tiles))
        {
            Debug.Log($"[U-Shape] RIGHT bottomLeft=({x},{y})");
            return true;
        }

        if (TryPattern(x, y, leftOffsets, 1, 1, out tiles))
        {
            Debug.Log($"[U-Shape] LEFT bottomLeft=({x},{y})");
            return true;
        }

        return false;
    }

    // S-Shape (5 taş, 4 yön) – ÖZEL (enableSShape5)
    // 3x3 grid, bottom-left (x,y) olarak düşünüyoruz.
    //
    // 1) Sağa bakan S:
    // X . .       (0,2)
    // X X X       (0,1)(1,1)(2,1)
    // . . X       (2,0)
    //
    // 2) Yukarı bakan S:
    // . X X       (1,2)(2,2)
    // . X .       (1,1)
    // X X .       (0,0)(1,0)
    //
    // 3) Sola bakan S:
    // . . X       (2,2)
    // X X X       (0,1)(1,1)(2,1)
    // X . .       (0,0)
    //
    // 4) Aşağı bakan S:
    // X X .       (0,2)(1,2)
    // . X .       (1,1)
    // . X X       (1,0)(2,0)
    bool CheckSShape5(int x, int y, PieceType p, out List<Tile> tiles)
    {
        tiles = null;

        if (!(CanMatch(p) && p.enableSShape5))
            return false;

        bool TryPattern(int baseX, int baseY, int[][] offsets, out List<Tile> outTiles)
        {
            outTiles = null;
            var list = new List<Tile>(5);

            foreach (var o in offsets)
            {
                int xx = baseX + o[0];
                int yy = baseY + o[1];

                if (!InBounds(xx, yy))
                    return false;

                if (!SameMatchType(grid[xx, yy].pieceType, p))
                    return false;

                list.Add(grid[xx, yy]);
            }

            outTiles = list;
            return true;
        }

        // 1) Sağa bakan S
        int[][] rightOffsets =
        {
            new[] { 0, 2 },
            new[] { 0, 1 }, new[] { 1, 1 }, new[] { 2, 1 },
            new[] { 2, 0 }
        };

        // 2) Yukarı bakan S
        int[][] upOffsets =
        {
            new[] { 1, 2 }, new[] { 2, 2 },
            new[] { 1, 1 },
            new[] { 0, 0 }, new[] { 1, 0 }
        };

        // 3) Sola bakan S
        int[][] leftOffsets =
        {
            new[] { 2, 2 },
            new[] { 0, 1 }, new[] { 1, 1 }, new[] { 2, 1 },
            new[] { 0, 0 }
        };

        // 4) Aşağı bakan S
        int[][] downOffsets =
        {
            new[] { 0, 2 }, new[] { 1, 2 },
            new[] { 1, 1 },
            new[] { 1, 0 }, new[] { 2, 0 }
        };

        if (TryPattern(x, y, rightOffsets, out tiles))
        {
            Debug.Log($"[S-Shape] RIGHT bottomLeft=({x},{y})");
            return true;
        }

        if (TryPattern(x, y, upOffsets, out tiles))
        {
            Debug.Log($"[S-Shape] UP bottomLeft=({x},{y})");
            return true;
        }

        if (TryPattern(x, y, leftOffsets, out tiles))
        {
            Debug.Log($"[S-Shape] LEFT bottomLeft=({x},{y})");
            return true;
        }

        if (TryPattern(x, y, downOffsets, out tiles))
        {
            Debug.Log($"[S-Shape] DOWN bottomLeft=({x},{y})");
            return true;
        }

        return false;
    }

    bool CheckSquare2x2(int x, int y, PieceType p, out List<Tile> tiles)
    {
        tiles = null;
        if (!InBounds(x + 1, y) || !InBounds(x, y + 1) || !InBounds(x + 1, y + 1))
            return false;

        if (!SameMatchType(grid[x, y].pieceType, p) ||
            !SameMatchType(grid[x + 1, y].pieceType, p) ||
            !SameMatchType(grid[x, y + 1].pieceType, p) ||
            !SameMatchType(grid[x + 1, y + 1].pieceType, p))
            return false;

        tiles = new List<Tile> { grid[x, y], grid[x + 1, y], grid[x, y + 1], grid[x + 1, y + 1] };
        return true;
    }

    // ÇİZGİLER (3–6'lı, yatay-dikey)
    void CollectLineRuns(List<SpecialMatch> result)
    {
        // YATAY
        for (int y = 0; y < height; y++)
        {
            int start = 0;
            for (int x = 1; x <= width; x++)
            {
                bool same = x < width &&
                            SameMatchType(grid[x, y].pieceType, grid[x - 1, y].pieceType);

                if (!same)
                {
                    int len = x - start;
                    if (len >= 3)
                    {
                        int bucket = Mathf.Clamp(len, 3, 5);
                        var tiles = new List<Tile>(bucket);
                        for (int k = 0; k < bucket; k++) tiles.Add(grid[start + k, y]);
                        result.Add(new SpecialMatch
                        {
                            count = bucket,
                            tiles = tiles,
                            label = "line",
                            orientation = "yan"
                        });
                    }
                    start = x;
                }
            }
        }

        // DİKEY
        for (int x = 0; x < width; x++)
        {
            int start = 0;
            for (int y = 1; y <= height; y++)
            {
                bool same = y < height &&
                            SameMatchType(grid[x, y].pieceType, grid[x, y - 1].pieceType);

                if (!same)
                {
                    int len = y - start;
                    if (len >= 3)
                    {
                        int bucket = Mathf.Clamp(len, 3, 6);
                        var tiles = new List<Tile>(bucket);
                        for (int k = 0; k < bucket; k++) tiles.Add(grid[x, start + k]);
                        result.Add(new SpecialMatch
                        {
                            count = bucket,
                            tiles = tiles,
                            label = "line",
                            orientation = "dik"
                        });
                    }
                    start = y;
                }
            }
        }
    }

    // TÜM PATTERNLERİ TOPLA
    List<SpecialMatch> CollectAllSpecial()
    {
        var result = new List<SpecialMatch>();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var p = grid[x, y].pieceType;
                if (!CanMatch(p)) continue;

                // ÖZEL: T-Shape
                if (CheckSwordTShape5(x, y, out var tSwordT))
                {
                    result.Add(new SpecialMatch
                    {
                        count = 5,
                        tiles = tSwordT,
                        label = "SwordT5",
                        orientation = null
                    });
                }

                // ÖZEL: 3x3 L-Shape
                if (p.enableLShape3x3)
                {
                    if (CheckL5_H3V3(x, y, p, +1, +1, out var a1)) result.Add(new SpecialMatch { count = 5, tiles = a1, label = "3x3", orientation = null });
                    if (CheckL5_H3V3(x, y, p, +1, -1, out var a2)) result.Add(new SpecialMatch { count = 5, tiles = a2, label = "3x3", orientation = null });
                    if (CheckL5_H3V3(x, y, p, -1, +1, out var a3)) result.Add(new SpecialMatch { count = 5, tiles = a3, label = "3x3", orientation = null });
                    if (CheckL5_H3V3(x, y, p, -1, -1, out var a4)) result.Add(new SpecialMatch { count = 5, tiles = a4, label = "3x3", orientation = null });
                }

                // ÖZEL: U-Shape
                if (p.enableUShape5 && CheckUShape5(x, y, p, out var u))
                {
                    result.Add(new SpecialMatch
                    {
                        count = 5,
                        tiles = u,
                        label = "U5",
                        orientation = null
                    });
                }

                // ÖZEL: S-Shape
                if (p.enableSShape5 && CheckSShape5(x, y, p, out var sTiles))
                {
                    result.Add(new SpecialMatch
                    {
                        count = 5,
                        tiles = sTiles,
                        label = "S5",
                        orientation = null
                    });
                }

                // NORMAL: Artı (+) 5'li
                if (CheckPlusShape5(x, y, out var plus))
                {
                    result.Add(new SpecialMatch
                    {
                        count = 5,
                        tiles = plus,
                        label = "Plus5",
                        orientation = null
                    });
                }

                // NORMAL: 2x2 kare
                if (x + 1 < width && y + 1 < height)
                {
                    if (CheckSquare2x2(x, y, p, out var sq))
                        result.Add(new SpecialMatch { count = 4, tiles = sq, label = "2x2", orientation = null });
                }

                // NORMAL: 3x2 L-Shape
                if (CheckL4_H3V2(x, y, p, +1, +1, out var t1)) result.Add(new SpecialMatch { count = 4, tiles = t1, label = "3x2", orientation = null });
                if (CheckL4_H3V2(x, y, p, +1, -1, out var t2)) result.Add(new SpecialMatch { count = 4, tiles = t2, label = "3x2", orientation = null });
                if (CheckL4_H3V2(x, y, p, -1, +1, out var t3)) result.Add(new SpecialMatch { count = 4, tiles = t3, label = "3x2", orientation = null });
                if (CheckL4_H3V2(x, y, p, -1, -1, out var t4)) result.Add(new SpecialMatch { count = 4, tiles = t4, label = "3x2", orientation = null });

                if (CheckL4_V3H2(x, y, p, +1, +1, out var t5)) result.Add(new SpecialMatch { count = 4, tiles = t5, label = "3x2", orientation = null });
                if (CheckL4_V3H2(x, y, p, +1, -1, out var t6)) result.Add(new SpecialMatch { count = 4, tiles = t6, label = "3x2", orientation = null });
                if (CheckL4_V3H2(x, y, p, -1, +1, out var t7)) result.Add(new SpecialMatch { count = 4, tiles = t7, label = "3x2", orientation = null });
                if (CheckL4_V3H2(x, y, p, -1, -1, out var t8)) result.Add(new SpecialMatch { count = 4, tiles = t8, label = "3x2", orientation = null });
            }

        // Düz çizgi eşleşmeleri
        CollectLineRuns(result);
        return result;
    }

    bool HasAnySpecialMatches() => CollectAllSpecial().Count > 0;

    // Özel eşleşme grubuna giren label’lar
    bool IsSpecialLabel(string label)
    {
        // T-Shape + 3x3 L-Shape + U-Shape + S-Shape
        return label == "SwordT5" || label == "3x3" || label == "U5" || label == "S5";
    }

    // ÖNEMLİ: Artık MatchInfo da döndürüyor
    bool TryGetPriorityClearSet(out List<Tile> toClear, out string log, out MatchInfo info)
    {
        toClear = null; log = null; info = default;
        var all = CollectAllSpecial();
        if (all.Count == 0) return false;

        // 1) ÖZEL EŞLEŞMELER GRUBU
        int idxSpecial = all.FindIndex(m => IsSpecialLabel(m.label));
        if (idxSpecial != -1)
        {
            var m = all[idxSpecial];
            toClear = new List<Tile>(m.tiles);

            MatchKind kind;
            switch (m.label)
            {
                case "SwordT5":
                    log = "T-Shape özel (5 taş) eşleşme";
                    kind = MatchKind.Special_T;
                    break;
                case "3x3":
                    log = "3x3 L-Shape özel (5 taş) eşleşme";
                    kind = MatchKind.Special_L3x3;
                    break;
                case "U5":
                    log = "U-Shape özel (5 taş, ortası boş) eşleşme";
                    kind = MatchKind.Special_U;
                    break;
                case "S5":
                    log = "S-Shape özel (5 taş) eşleşme";
                    kind = MatchKind.Special_S;
                    break;
                default:
                    log = "Özel eşleşme";
                    kind = MatchKind.Special_T;
                    break;
            }

            info = new MatchInfo(kind, m.tiles[0].pieceType, m.tiles);
            return true;
        }

        // 2) Artı (+) 5'li – normal eşleşmeler içinde en yüksek öncelik
        int idxPlus = all.FindIndex(m => m.label == "Plus5");
        if (idxPlus != -1)
        {
            var m = all[idxPlus];
            toClear = new List<Tile>(m.tiles);
            log = "Artı (+) 5'li eşleşme";

            info = new MatchInfo(MatchKind.Plus5, m.tiles[0].pieceType, m.tiles);
            return true;
        }

        // 3) 2x2 kare
        int idxSquare = all.FindIndex(m => m.label == "2x2");
        if (idxSquare != -1)
        {
            var m = all[idxSquare];
            toClear = new List<Tile>(m.tiles);
            log = "2x2 (kare eşleşme)";

            info = new MatchInfo(MatchKind.Square2x2, m.tiles[0].pieceType, m.tiles);
            return true;
        }

        // 4) 3x2 L-Shape
        int idx32 = all.FindIndex(m => m.label == "3x2");
        if (idx32 != -1)
        {
            var m = all[idx32];
            toClear = new List<Tile>(m.tiles);
            log = "3x2 L-Shape eşleşme (4 taş)";

            info = new MatchInfo(MatchKind.L3x2, m.tiles[0].pieceType, m.tiles);
            return true;
        }

        // 5..8) Çizgi eşleşmeleri — 6 -> 5 -> 4 -> 3
        foreach (int target in new int[] { 5, 4, 3 })
        {
            int idx = all.FindIndex(m => m.label == "line" && m.count == target);
            if (idx != -1)
            {
                var m = all[idx];
                toClear = new List<Tile>(m.tiles);
                log = $"{target}’li {m.orientation} çizgi eşleşme";

                MatchKind kind = MatchKind.Line3;
                switch (target)
                {
                    case 5: kind = MatchKind.Line5; break;
                    case 4: kind = MatchKind.Line4; break;
                    case 3: kind = MatchKind.Line3; break;
                }

                info = new MatchInfo(kind, m.tiles[0].pieceType, m.tiles);
                return true;
            }
        }

        return false;
    }

    IEnumerator ResolveBoardCascading()
    {
        while (true)
        {
            if (HasAnyHoles())
            {
                yield return StartCoroutine(CollapseAndFillSmooth());
                if (betweenCascadeDelay > 0f)
                    yield return new WaitForSeconds(betweenCascadeDelay);
                continue;
            }

            // BURADA YENİ İMZA KULLANIYORUZ
            if (!TryGetPriorityClearSet(out var matches, out var msg, out MatchInfo info) || matches.Count == 0)
            {
                if (!HasAnyAvailableMove())
                    ShuffleUntilPlayable_NoAutoMatches();
                yield break;
            }

            Debug.Log("[Match] " + msg);

            //  EŞLEŞME EVENT'İ BURADA TETİKLİYORUZ
            OnMatchResolved?.Invoke(info);

            if (preMatchDelay > 0f)
                yield return new WaitForSeconds(preMatchDelay);

            foreach (var t in matches)
                t.ClearPiece();

            if (postClearDelay > 0f)
                yield return new WaitForSeconds(postClearDelay);
        }
    }

    bool HasAnyHoles()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y].pieceType == null)
                    return true;
        return false;
    }

    // --------------------- COLLAPSE & FILL -------------------------
    IEnumerator CollapseAndFillSmooth()
    {
        _activeMoves = 0;

        for (int x = 0; x < width; x++)
        {
            int writeY = 0;
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y].pieceType != null)
                {
                    if (writeY != y)
                    {
                        grid[x, writeY].SetPiece(grid[x, y].pieceType);
                        grid[x, y].ClearPiece();
                        grid[x, writeY].transform.position = GridToWorld(x, y);

                        _activeMoves++;
                        StartCoroutine(SmoothMoveAndSignal(
                            grid[x, writeY].transform,
                            GridToWorld(x, writeY)
                        ));
                    }
                    writeY++;
                }
            }

            for (int y = writeY; y < height; y++)
            {
                var t = grid[x, y];
                t.SetPiece(GetRandomPieceWeighted());
                var spawn = GridToWorld(x, height) + Vector3.up * 0.25f;
                t.transform.position = spawn;

                _activeMoves++;
                StartCoroutine(SmoothMoveAndSignal(t.transform, GridToWorld(x, y)));
            }
        }

        yield return new WaitUntil(() => _activeMoves == 0);
    }

    IEnumerator SmoothMoveAndSignal(Transform tr, Vector3 target)
    {
        while ((tr.position - target).sqrMagnitude > 0.0001f)
        {
            tr.position = Vector3.MoveTowards(tr.position, target, fallAnimSpeed * Time.deltaTime);
            yield return null;
        }
        tr.position = target;
        _activeMoves = Mathf.Max(0, _activeMoves - 1);
    }

    // -------------------- MOVE AVAILABILITY ------------------------
    bool HasAnyAvailableMove()
    {
        // Tahtada en az bir özel taş varsa hamle var say
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y].pieceType != null && grid[x, y].pieceType.isSpecial)
                    return true;

        int[] dx = { 1, 0 };
        int[] dy = { 0, 1 };

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var a = grid[x, y];
                if (!CanMatch(a.pieceType)) continue;

                for (int k = 0; k < 2; k++)
                {
                    int nx = x + dx[k];
                    int ny = y + dy[k];
                    if (!InBounds(nx, ny)) continue;

                    var b = grid[nx, ny];
                    if (!CanMatch(b.pieceType)) continue;

                    SwapPieceTypes(a, b, false);
                    bool ok = HasAnySpecialMatches();
                    SwapPieceTypes(a, b, false);

                    if (ok) return true;
                }
            }
        return false;
    }

    void SwapPieceTypes(Tile a, Tile b, bool updateSprite)
    {
        var tmp = a.pieceType;
        a.pieceType = b.pieceType;
        b.pieceType = tmp;
        if (updateSprite)
        {
            if (a.spriteRenderer) a.spriteRenderer.sprite = a.pieceType ? a.pieceType.sprite : null;
            if (b.spriteRenderer) b.spriteRenderer.sprite = b.pieceType ? b.pieceType.sprite : null;
        }
    }
}
