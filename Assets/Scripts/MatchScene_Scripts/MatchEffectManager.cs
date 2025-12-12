using UnityEngine;

public class MatchEffectManager : MonoBehaviour
{
    public GridManager grid;

    void OnEnable()
    {
        if (grid != null)
            grid.OnMatchResolved += HandleMatch;
    }

    void OnDisable()
    {
        if (grid != null)
            grid.OnMatchResolved -= HandleMatch;
    }

    void HandleMatch(GridManager.MatchInfo info)
    {
        // Eþleþmenin oluþtuðu taþ tipi:
        var piece = info.pieceType;
        var center = info.center;

        switch (info.kind)
        {
            // ---------------- ÖZEL EÞLEÞMELER ----------------
            case GridManager.MatchKind.Special_T:
                Debug.Log($"[Effect] T-Shape özel. Taþ: {piece?.name}, Count={info.tileCount}");
                OnSpecialT(center, piece);
                break;

            case GridManager.MatchKind.Special_L3x3:
                Debug.Log($"[Effect] 3x3 L özel. Taþ: {piece?.name}, Count={info.tileCount}");
                OnSpecialL3x3(center, piece);
                break;

            case GridManager.MatchKind.Special_U:
                Debug.Log($"[Effect] U-Shape özel. Taþ: {piece?.name}, Count={info.tileCount}");
                OnSpecialU(center, piece);
                break;

            case GridManager.MatchKind.Special_S:
                Debug.Log($"[Effect] S-Shape özel. Taþ: {piece?.name}, Count={info.tileCount}");
                OnSpecialS(center, piece);
                break;

            // ---------------- NORMAL ÖZELLER ------------------
            case GridManager.MatchKind.Plus5:
                Debug.Log($"[Effect] Plus (+) 5'li. Taþ: {piece?.name}");
                OnPlus5(center, piece);
                break;

            case GridManager.MatchKind.Square2x2:
                Debug.Log($"[Effect] 2x2 kare. Taþ: {piece?.name}");
                OnSquare2x2(center, piece);
                break;

            case GridManager.MatchKind.L3x2:
                Debug.Log($"[Effect] 3x2 L. Taþ: {piece?.name}");
                OnL3x2(center, piece);
                break;

            // ---------------- ÇÝZGÝ EÞLEÞMELERÝ --------------
            case GridManager.MatchKind.Line3:
                Debug.Log($"[Effect] 3'lü çizgi. Taþ: {piece?.name}");
                OnLine3(center, piece);
                break;

            case GridManager.MatchKind.Line4:
                Debug.Log($"[Effect] 4'lü çizgi. Taþ: {piece?.name}");
                OnLine4(center, piece);
                break;

            case GridManager.MatchKind.Line5:
                Debug.Log($"[Effect] 5'li çizgi. Taþ: {piece?.name}");
                OnLine5(center, piece);
                break;

           
        }
    }

    // ---------------------------------------------------------
    //  BURADAN SONRASI: Her eþleþme tipi için ayrý metodlar
    //  Þu an içleri boþ, istediðin gameplay efektlerini buraya
    //  tek tek dolduracaksýn.
    // ---------------------------------------------------------

    void OnSpecialT(Vector2Int center, PieceType type)
    {
        // TODO: T-Shape özel yeteneði
    }

    void OnSpecialL3x3(Vector2Int center, PieceType type)
    {
        // TODO: 3x3 L özel yeteneði
    }

    void OnSpecialU(Vector2Int center, PieceType type)
    {
        // TODO: U-Shape özel yeteneði
    }

    void OnSpecialS(Vector2Int center, PieceType type)
    {
        // TODO: S-Shape özel yeteneði
    }

    void OnPlus5(Vector2Int center, PieceType type)
    {
        // TODO: Artý (+) 5'li
    }

    void OnSquare2x2(Vector2Int center, PieceType type)
    {
        // TODO: 2x2 kare
    }

    void OnL3x2(Vector2Int center, PieceType type)
    {
        // TODO: 3x2 L
    }

    void OnLine3(Vector2Int center, PieceType type)
    {
        // TODO: 3'lü çizgi (ör: küçük buff)
    }

    void OnLine4(Vector2Int center, PieceType type)
    {
        // TODO: 4'lü çizgi (ör: orta seviye buff)
    }

    void OnLine5(Vector2Int center, PieceType type)
    {
        // TODO: 5'li çizgi (ör: güçlü buff)
    }

    
}
