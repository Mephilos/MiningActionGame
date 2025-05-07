using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 주변 청크를 로딩,언로딩하며 최적화된 월드 운영을 담당
/// </summary>
public class ChunkManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private int worldSeed = 12345;

    [Header("Settings")]
    [SerializeField] private int viewDistanceInChunks = 2;

    private Dictionary<Vector2Int, Chunk> loadedChunks = new();
    private Vector2Int lastPlayerChunk;

    private void Start()
    {
        lastPlayerChunk = GetPlayerChunkCoord();
        UpdateChunks();
    }

    private void Update()
    {
        Vector2Int currentChunk = GetPlayerChunkCoord();
        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            UpdateChunks();
        }
    }

    /// <summary>
    /// 시야 범위 내 청크 유지 및 외부 청크 제거
    /// </summary>
    private void UpdateChunks()
    {
        HashSet<Vector2Int> newChunkCoords = new();

        for (int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
        {
            for (int z = -viewDistanceInChunks; z <= viewDistanceInChunks; z++)
            {
                Vector2Int coord = new(lastPlayerChunk.x + x, lastPlayerChunk.y + z);
                newChunkCoords.Add(coord);

                if (!loadedChunks.ContainsKey(coord))
                {
                    Vector3 worldPosition = new(coord.x * chunkSize, 0f, coord.y * chunkSize);
                    GameObject chunkGO = Instantiate(chunkPrefab, worldPosition, Quaternion.identity, transform);
                    Chunk chunk = chunkGO.GetComponent<Chunk>();
                    int derivedSeed = worldSeed + coord.x * 73856093 + coord.y * 19349663;
                    chunk.Initialize(coord, chunkSize, derivedSeed, 0.2f, 5f);
                    loadedChunks.Add(coord, chunk);
                }
            }
        }

       // 외부 청크 제거
        List<Vector2Int> toRemove = new();
        foreach (var coord in loadedChunks.Keys)
        {
            if (!newChunkCoords.Contains(coord))
            {
                Chunk chunk = loadedChunks[coord];
                chunk.ReturnAllBlocks(); // 블록 반환
                Destroy(chunk.gameObject); // 빈 청크 오브젝트 제거
                toRemove.Add(coord);
            }
        }

        foreach (var coord in toRemove)
        {
            loadedChunks.Remove(coord);
        }
    }

    /// <summary>
    /// 플레이어의 현재 청크 좌표 계산
    /// </summary>
    private Vector2Int GetPlayerChunkCoord()
    {
        int x = Mathf.FloorToInt(player.position.x / chunkSize);
        int z = Mathf.FloorToInt(player.position.z / chunkSize);
        return new Vector2Int(x, z);
    }
}