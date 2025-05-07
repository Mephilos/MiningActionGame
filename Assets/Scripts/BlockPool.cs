using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 블록 오브젝트를 재사용하기 위한 오브젝트 풀링 매니저
/// </summary>
public class BlockPool : MonoBehaviour
{
    public static BlockPool Instance { get; private set; }

    [System.Serializable]
    public class BlockEntry
    {
        public BlockType type;
        public GameObject prefab;
        public int initialSize = 64;
    }

    [Header("풀링할 블록 프리팹 설정")]
    [SerializeField] private List<BlockEntry> blockEntries;

    private Dictionary<BlockType, Queue<GameObject>> pool = new();
    private Dictionary<BlockType, GameObject> prefabMap = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        else Destroy(gameObject);

        foreach (var entry in blockEntries)
        {
            if (entry.prefab == null)
            {
                Debug.LogError($"[BlockPool] BlockEntry for type '{entry.type}' has a null prefab assigned!");
                continue; // 프리팹이 없으면 이 항목은 건너뜀
            }

            prefabMap[entry.type] = entry.prefab;
            pool[entry.type] = new Queue<GameObject>();

            for (int i = 0; i < entry.initialSize; i++)
            {
                GameObject obj = Instantiate(entry.prefab, transform);

                // BlockInfo 타입 정보 설정
                BlockInfo info = obj.GetComponent<BlockInfo>();
                if (info == null)
                    info = obj.AddComponent<BlockInfo>();

                info.type = entry.type;

                obj.SetActive(false);
                pool[entry.type].Enqueue(obj);
            }
            Debug.Log($"[BlockPool] Initialized pool for type '{entry.type}' with {pool[entry.type].Count} objects.");
        }
    }

    /// <summary>
    /// 블록을 풀에서 가져오기 (없으면 새로 생성)
    /// </summary>
    public GameObject GetBlock(BlockType type)
    {
        if (!pool.ContainsKey(type)) return null;

        if (pool[type].Count == 0)
        {
            GameObject newBlock = Instantiate(prefabMap[type], transform);
            return newBlock;
        }

        GameObject obj = pool[type].Dequeue();
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 블록을 다시 풀에 반환
    /// </summary>
    public void ReturnBlock(GameObject obj)
    {
        BlockInfo info = obj.GetComponent<BlockInfo>();
        if (info == null)
        {
            Debug.LogWarning("[BlockPool] Returned object has no BlockInfo component!");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        pool[info.type].Enqueue(obj);
    }
    public bool HasBlockType(BlockType type)
    {
        return pool.ContainsKey(type);
    }
}