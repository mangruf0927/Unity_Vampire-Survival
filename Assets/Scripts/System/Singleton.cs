using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static bool isQuitting;
    public static bool destroyOnLoad = false;
    protected bool IsInstance => instance == this;

    public static T Instance
    {
        get
        {
            if (isQuitting || !Application.isPlaying) return null;
            if (instance == null) instance = FindAnyObjectByType<T>();
            if (instance == null)
            {
                GameObject singleton = new("(singleton) " + typeof(T).Name);
                instance = singleton.AddComponent<T>();
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this as T;
        if (!destroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    protected static void ResetSingleton()
    {
        instance = null;
        isQuitting = false;
    }

    protected virtual void OnApplicationQuit()
    {
        if (IsInstance) isQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (IsInstance) instance = null;
    }
}
