using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [System.Serializable]
    public class ObjPool
    {
        public string tag;
        public GameObject prefab;
        public int poolSize;
    }

    public List<ObjPool> objPools;

    private Dictionary<string, Queue<GameObject>> _objPoolDict;
    private Dictionary<GameObject, string> _activeObjTags;
    private Transform _poolParent;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        _poolParent = new GameObject("PooledObjects").transform;
        _objPoolDict = new Dictionary<string, Queue<GameObject>>();
        _activeObjTags = new Dictionary<GameObject, string>();
        
        foreach (ObjPool pool in objPools)
        {
            Queue<GameObject> poolObject = new Queue<GameObject>();

            for (int i = 0; i < pool.poolSize; i++)
            {
                GameObject obj = Instantiate(pool.prefab, _poolParent);
                obj.SetActive(false);
                poolObject.Enqueue(obj);
            }

            _objPoolDict.Add(pool.tag, poolObject);
        }
    }

    public GameObject GetFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        Queue<GameObject> poolObject = _objPoolDict[tag];
        GameObject objToSpawn;

        // 풀에 사용 가능한 오브젝트가 있으면 큐에서 꺼내서 사용
        if (poolObject.Count > 0)
        {
            objToSpawn = poolObject.Dequeue();
        }
        else // 풀이 비어있으면 새로 생성
        {
            ObjPool pool = objPools.Find(p => p.tag == tag);
            objToSpawn = Instantiate(pool.prefab, _poolParent);
        }
        
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true); 
        _activeObjTags[objToSpawn] = tag;
        return objToSpawn;
    }
    public void ReturnToPool(GameObject objToReturn)
    {
        if (objToReturn == null || !objToReturn.activeInHierarchy) return;

        if (_activeObjTags.TryGetValue(objToReturn, out string tag))
        {
            _activeObjTags.Remove(objToReturn); // 활성 목록에서 제거
            objToReturn.SetActive(false);
            _objPoolDict[tag].Enqueue(objToReturn); // 비활성화 후 다시 큐에 넣음
        }
        else
        {
            Destroy(objToReturn);
        }
    }
}