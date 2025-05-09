using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 플레이어 주변 청크를 로딩,언로딩하며 최적화된 월드 운영을 담당
/// </summary>
public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; } // 블럭과 상호 작용위한 인스턴스 (싱글톤)

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
    //싱글톤 설정
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 씬 전환 유지용
        }
        else
        {
            Destroy(gameObject);
        }
    }
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
    /// <summary>
    /// 지정된 월드 좌표의 블록을 파괴합니다.
    /// </summary>
    /// <param name="worldX">파괴할 블록의 월드 X 좌표</param>
    /// <param name="worldY">파괴할 블록의 월드 Y 좌표</param>
    /// <param name="worldZ">파괴할 블록의 월드 Z 좌표</param>
    /// <returns>블록 파괴 성공 여부</returns>
    public bool DestroyBlockAt(int worldX, int worldY, int worldZ)
    {
        // 월드 좌표가 속한 청크 좌표 계산
        int chunkX = Mathf.FloorToInt((float)worldX / chunkSize);
        int chunkZ = Mathf.FloorToInt((float)worldZ / chunkSize);
        Vector2Int targetChunkCoord = new Vector2Int(chunkX, chunkZ);

        // 해당 청크가 로드되어 있는지 확인
        if (loadedChunks.TryGetValue(targetChunkCoord, out Chunk targetChunk))
        {
            // 월드 좌표를 청크 내부 로컬 좌표로 변환
            int localX = worldX - chunkX * chunkSize;
            int localY = worldY; // Y 좌표는 월드 좌표 그대로 사용 (청크가 0부터 시작하므로)
            int localZ = worldZ - chunkZ * chunkSize;

            // 4. 해당 청크의 블록 변경 함수 호출
            return targetChunk.ChangeBlock(localX, localY, localZ, BlockType.Air);
        }
        else
        {
            Debug.LogWarning($"Attempted to destroy block in an unloaded chunk at coord: {targetChunkCoord}");
            return false; // 로드되지 않은 청크의 블록은 파괴할 수 없음
        }
    }
    /// <summary>
    /// 지정된 좌표의 청크에게 메시 업데이트를 요청 (Chunk의 ChangeBlock에서 호출됨)
    /// </summary>
    public void RequestChunkMeshUpdate(Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunkToUpdate))
        {
            // 해당 청크의 메시를 다시 생성하도록 요청
            chunkToUpdate.CreateChunkMesh();
            Debug.Log($"Requested mesh update for neighbor chunk at {chunkCoord}");
        }
        // else 해당 청크가 로드되지 않았으면 업데이트할 필요 없음
    }
}