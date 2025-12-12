using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Collections;

public class InputManager : MonoBehaviour
{
    public Camera mainCamera;
    public GridManager gridManager;

    private Tile selected;

    void Update()
    {
        if (!gridManager || !gridManager.CanPlayerInteract())
            return;

        bool pressed =
#if ENABLE_INPUT_SYSTEM
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            Input.GetMouseButtonDown(0);
#endif
        if (!pressed) return;

#if ENABLE_INPUT_SYSTEM
        Vector2 screen = Mouse.current.position.ReadValue();
#else
        Vector2 screen = Input.mousePosition;
#endif
        Vector2 world = mainCamera.ScreenToWorldPoint(screen);

        var hit = Physics2D.Raycast(world, Vector2.zero);
        if (hit.collider == null || !hit.collider.TryGetComponent(out Tile clicked)) return;

        HandleTileClick(clicked);
    }

    void HandleTileClick(Tile t)
    {
        if (selected == null)
        {
            Select(t);
            return;
        }

        if (selected == t)
        {
            Unselect(selected);
            selected = null;
            return;
        }

        // komþu ise swap dene
        if (Mathf.Abs(selected.x - t.x) + Mathf.Abs(selected.y - t.y) == 1)
        {
            StartCoroutine(SwapFlow(selected, t));
            return;
        }

        // komþu deðilse seçimi deðiþtir
        Unselect(selected);
        Select(t);
    }

    IEnumerator SwapFlow(Tile a, Tile b)
    {
        Unselect(a);
        Unselect(b);

        int movesBefore = gridManager.movesLeft;

        yield return StartCoroutine(gridManager.TrySwapAndResolve(a, b));

        int movesAfter = gridManager.movesLeft;

        if (movesAfter < movesBefore)
        {
            Debug.Log($"[Moves] Kalan hamle: {gridManager.movesLeft}");
        }

        selected = null;
    }


    void Select(Tile t)
    {
        selected = t;
        SetGlow(t, true);
    }

    void Unselect(Tile t)
    {
        if (t == null) return;
        SetGlow(t, false);
    }

    // Tile prefabýnda "GlowEffect" isimli çocuk obje olmalý
    void SetGlow(Tile t, bool on)
    {
        var glow = t.transform.Find("GlowEffect");
        if (glow != null) glow.gameObject.SetActive(on);
    }
}
