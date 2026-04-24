using UnityEngine;

public class PersistentSingleton : MonoBehaviour
{
    private static readonly System.Collections.Generic.HashSet<string> Alive = new();

    [Tooltip("Ayný anahtar ile ikinci instance gelirse yok edilir.")]
    public string uniqueKey = "DefaultKey";

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(uniqueKey))
            uniqueKey = gameObject.name;

        if (Alive.Contains(uniqueKey))
        {
            Destroy(gameObject);
            return;
        }

        Alive.Add(uniqueKey);
        DontDestroyOnLoad(gameObject);
    }
}
