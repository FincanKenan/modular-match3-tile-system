using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x, y;
    public PieceType pieceType;
    public SpriteRenderer spriteRenderer;

    private void Reset()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetPiece(PieceType type)
    {
        if (!spriteRenderer)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (!spriteRenderer)
            {
                Debug.LogError("SpriteRenderer missing on " + name);
                return;
            }
        }

        if (type == null)
        {
            Debug.LogError("PieceType is null for SetPiece on tile " + name);
            return;
        }

        pieceType = type;
        spriteRenderer.sprite = type.sprite;
    }

    public void ClearPiece()
    {
        pieceType = null;
        if (spriteRenderer) spriteRenderer.sprite = null;
    }
}
