using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual bool Persistent => true;

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this as T;

        if (Persistent)
            DontDestroyOnLoad(gameObject);

        OnAwake();
    }

    protected virtual void OnAwake() { }
}