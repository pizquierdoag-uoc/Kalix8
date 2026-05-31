using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    protected virtual bool Persistent => true;

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Persistente: ya existe uno entre escenas → destruye el duplicado nuevo.
            // No persistente: el viejo Instance pertenece a una escena que se está
            // descargando; lo reemplazamos por el nuevo en vez de destruir éste.
            if (Persistent) { Destroy(gameObject); return; }
        }
        Instance = this as T;
        if (Persistent) DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
