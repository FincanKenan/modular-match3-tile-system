using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    public Color newColor = Color.red; // Inspector’dan da ayarlanabilir
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material.color = newColor;
    }
}
