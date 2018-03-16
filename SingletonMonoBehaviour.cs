using UnityEngine;
using System.Collections;

public class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
{
    public static T Instance;

    void Awake()
    {
        if (!object.ReferenceEquals(Instance, null))
        {
            GameObject.Destroy(Instance);
        }

        Instance = this as T;
        Init();
    }

    void OnDestroy()
    {
        UnInit();
        if (object.ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    protected virtual void Init()
    {
    }

    protected virtual void UnInit()
    {
    }
}