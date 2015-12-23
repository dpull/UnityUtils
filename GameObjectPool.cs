using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace X
{
    public class GameObjectPool
    {
        class ObjectPoolItem
        {
            // public DateTime LastInstantiateTime; 
            public Stack<GameObject> GameObjects = new Stack<GameObject>(); 
        }
        
        class ObjectPoolParameter : MonoBehaviour
        {
            public int InstanceID;
        }

        Transform RecycleBin;
        Dictionary<int?, ObjectPoolItem> ObjectPoolItems = new Dictionary<int?, ObjectPoolItem>();

        public GameObjectPool(Transform recycleBin)
        {
            RecycleBin = recycleBin;
        }

        bool CreateObjectPoolItem(GameObject go, out int instanceID, out ObjectPoolItem item)
        {
            instanceID = 0;
            item = null;
            
            var parameter = go.GetComponent<ObjectPoolParameter>();
            if (parameter == null)
                return false;
            
            instanceID = parameter.InstanceID;
            if (ObjectPoolItems.TryGetValue(instanceID, out item))
                return true;
            
            item = new ObjectPoolItem();
            ObjectPoolItems[instanceID] = item;
            return true;
        }
        
        public GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation)
        {
            int instanceID = original.GetInstanceID(); // 缓存池创建时不检查GUID参数，因为外部可能进行改变
            ObjectPoolItem item;
            
            if (!ObjectPoolItems.TryGetValue(instanceID, out item))
            {
                var newObj = GameObject.Instantiate(original, position, rotation) as GameObject;
                var parameter = newObj.AddMissingComponent<ObjectPoolParameter>();
                parameter.InstanceID = instanceID;
                newObj.BroadcastMessage("OnSpawned", SendMessageOptions.DontRequireReceiver);
                return newObj; 
            }
            
            var obj = item.GameObjects.Pop();
            if (item.GameObjects.Count == 0)
                ObjectPoolItems.Remove(instanceID);
            
            obj.transform.SetParent(null, false);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.BroadcastMessage("OnSpawned", SendMessageOptions.DontRequireReceiver);
            return obj;
        }
        
        public GameObject Instantiate(GameObject original)
        {
            return Instantiate(original, original.transform.position, original.transform.rotation);
        }
        
        public void Destroy(GameObject go)
        {
            int instanceID;
            ObjectPoolItem item;
            
            if (!CreateObjectPoolItem(go, out instanceID, out item))
            {
                go.transform.SetParent(RecycleBin, false);
                GameObject.Destroy(go);
                return;
            }

            go.BroadcastMessage("OnDespawned", SendMessageOptions.DontRequireReceiver);
            go.transform.SetParent(RecycleBin, false);
            item.GameObjects.Push(go);
        }
    }
}


