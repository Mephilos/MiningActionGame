using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 플레이어 주변 청크를 로딩,언로딩하며 최적화된 월드 운영을 담당
/// </summary>
public class ChunkManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private Material chunkSharedMaterial;
    
    [Header("World Settings")]
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private int chunkBuildHeight = 64;   // 청크의 Y축 최대 높이
    [SerializeField] private int worldSeed = 12345;
    [SerializeField] private float noiseScale = 0.05f;      
    [SerializeField] private float terrainHeightMultiplier = 20f; 

    [Header("View Settings")]
    [SerializeField] private int viewDistanceInChunks = 2;

    private Dictionary<Vector2Int, Chunk> loadedChunks = new Dictionary<Vector2Int, Chunk>();
    private Vector2Int lastPlayerChunkCoord;

    private void Start()
    {
        // 청크 프리팹에 필수 컴포넌트가 있는지 확인 (안전 장치)
        if (chunkPrefab.GetComponent<Chunk>() == null)
            chunkPrefab.AddComponent<Chunk>();
        if (chunkPrefab.GetComponent<MeshFilter>() == null)
            chunkPrefab.AddComponent<MeshFilter>();
        if (chunkPrefab.GetComponent<MeshRenderer>() == null)
            chunkPrefab.AddComponent<MeshRenderer>();

        if (chunkSharedMaterial == null)
        {
            Debug.LogError("[ChunkManager] Chunk Shared Material is not assigned!");
        }
        lastPlayerChunkCoord = GetPlayerChunkCoord();
        UpdateChunks();
    }

    private void Update()
    {
        Vector2Int currentPlayerChunkCoord = GetPlayerChunkCoord();
        if (currentPlayerChunkCoord != lastPlayerChunkCoord)
        {
            lastPlayerChunkCoord = currentPlayerChunkCoord;
            UpdateChunks();
        }
    }

    private void UpdateChunks()
    {
        HashSet<Vector2Int> requiredChunkCoords = new HashSet<Vector2Int>();

        // 플레이어 주변에 필요한 청크 좌표 계산
        for (int xOffset = -viewDistanceInChunks; xOffset <= viewDistanceInChunks; xOffset++)
        {
            for (int zOffset = -viewDistanceInChunks; zOffset <= viewDistanceInChunks; zOffset++)
            {
                Vector2Int coord = new Vector2Int(lastPlayerChunkCoord.x + xOffset, lastPlayerChunkCoord.y + zOffset);
                requiredChunkCoords.Add(coord);

                // 아직 로드되지 않은 청크라면 새로 생성 또는 풀에서 가져와 로드
                if (!loadedChunks.ContainsKey(coord))
                {
                    LoadChunkAt(coord);
                }
            }
        }
        // 더 이상 필요 없는 청크(시야에서 벗어난 청크) 언로드
        List<Vector2Int> chunksToUnloadKeys = new List<Vector2Int>();
        foreach (var loadedChunkPair in loadedChunks)
        {
            if (!requiredChunkCoords.Contains(loadedChunkPair.Key))
            {
                chunksToUnloadKeys.Add(loadedChunkPair.Key);
            }
        }

        foreach (var coordToUnload in chunksToUnloadKeys)
        {
            UnloadChunkAt(coordToUnload);
        }
    }

    /// <summary>
    /// 지정된 좌표에 청크를 로드
    /// </summary>
    private void LoadChunkAt(Vector2Int coord)
    {
        Vector3 worldPosition = new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);
        GameObject chunkGO = Instantiate(chunkPrefab, worldPosition, Quaternion.identity, this.transform); // 청크 매니저의 자식으로 생성
        Chunk chunkScript = chunkGO.GetComponent<Chunk>();

        int derivedSeed = worldSeed + coord.x * 7384 + coord.y * 1934;

        // Chunk 스크립트 초기화
        chunkScript.Initialize(coord, chunkSize, chunkBuildHeight, derivedSeed, noiseScale, terrainHeightMultiplier, chunkSharedMaterial);
        loadedChunks.Add(coord, chunkScript);
    }


    /// <summary>
    /// 지정된 좌표의 청크를 언로드 (파괴 또는 풀에 반환)
    /// </summary>
    private void UnloadChunkAt(Vector2Int coord)
    {
        if (loadedChunks.TryGetValue(coord, out Chunk chunkToUnload))
        {
            Destroy(chunkToUnload.gameObject); // 일단은 즉시 파괴
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