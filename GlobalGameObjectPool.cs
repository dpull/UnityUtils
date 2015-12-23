using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using X;

public class GlobalGameObjectPool : SingletonMonoBehaviour<GlobalGameObjectPool> 
{
	GameObjectPool Pool;

    static GlobalGameObjectPool GetInstance()
    {
        if (Instance == null)
        {
            var go = new GameObject("GlobalGameObjectPool");
            go.AddComponent<GlobalGameObjectPool>();

            var goRecycleBin = new GameObject("RecycleBin");
            goRecycleBin.SetActive(false);
            
            var transformRecycleBin = goRecycleBin.transform;
            transformRecycleBin.SetParent(go.transform, false);
            Instance.Pool = new GameObjectPool(transformRecycleBin);
        }
        
        return Instance;
    }

	public static Transform GetTransform()
	{
        var pool = GetInstance();
		return pool.transform;
	}
    
    public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation)
    {
        var pool = GetInstance();
        return pool.Pool.Instantiate(original, position, rotation);
    }
	    
    public static GameObject Instantiate(GameObject original)
    {
        var pool = GetInstance();
        return pool.Pool.Instantiate(original);
    }
    
    public static void Destroy(GameObject go)
    {
        if (Instance == null)
        {
            GameObject.Destroy(go);
            return;
        }
        Instance.Pool.Destroy(go);
    }

    public static void Destroy(GameObject go, float delay)
    {
        if (Instance == null)
        {
            GameObject.Destroy(go, delay);
            return;
        }
        Instance.StartCoroutine(Instance.DestroyCoroutine(go, delay));
    }

    IEnumerator DestroyCoroutine(GameObject go, float time)
    {
        yield return new WaitForSeconds(time);
        if (go != null)
            Pool.Destroy(go);
    }
}

public class PoolObjectBehaviour : MonoBehaviour 
{
    private bool PoolObjectInitFlag;
    private bool PoolDestoryFlag;

    protected virtual void OnDestroy()
    {
        if (!PoolDestoryFlag)
            OnPoolDestroy();
    }
    
    protected virtual void OnSpawned()
    {
        PoolObjectInitFlag = false;
        PoolDestoryFlag = false;
    }
    
    protected virtual void OnDespawned()
    {
        PoolDestoryFlag = true;
        OnPoolDestroy();
    }
    
    protected virtual void Update()
    {
        if (!PoolObjectInitFlag)
        {
            OnPoolStart();
            PoolObjectInitFlag = true;
        }
        OnPoolUpdate();
    }

    // 推荐
    public virtual void OnPoolStart()
    { 
        
    }
    
    public virtual void OnPoolUpdate()
    { 
        
    }
    
    public virtual void OnPoolDestroy()
    { 
        
    }
}


