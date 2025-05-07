using UnityEngine;

/// <summary>
/// 월드 전체의 초기 청크를 생성하는 클래스
/// - 청크 프리팹을 기반으로 플레이어 주변 청크를 배치
/// - 각 청크는 독립적으로 Chunk 컴포넌트가 초기화 처리함
/// </summary>
public class CubeWorldGenerator : MonoBehaviour
{
    [Header("World Settings")]

    [SerializeField] private int worldSizeInChunks = 3;
    // 생성할 청크의 수 (예: 3이면 3x3 청크)

    [SerializeField] private int chunkSize = 16;
    // 하나의 청크가 커버하는 블록의 폭/길이

    [SerializeField] private float noiseScale = 0.1f;
    // Perlin Noise의 샘플 밀도 (값이 작을수록 넓은 지형 패턴)

    [SerializeField] private float heightMultiplier = 5f;
    // Perlin Noise 값에 곱해지는 y축 높이 배수

    [SerializeField] private GameObject grassBlockPrefab;
    [SerializeField] private GameObject stoneBlockPrefab;
    // 블록 프리팹: 최상층은 grass, 나머지는 stone

    [SerializeField] private Transform blocksParent;
    // 생성된 모든 청크를 담을 부모 트랜스폼

    [SerializeField] private GameObject chunkPrefab;
    // Chunk.cs가 붙어있는 프리팹 (청크 단위로 생성될 오브젝트)

    private int seed;

    private void Start()
    {
        // 시드를 랜덤으로 생성하고 전체 월드 생성 시작
        seed = Random.Range(0, 99999);
        GenerateWorld();
    }

    /// <summary>
    /// 전체 월드를 청크 단위로 생성
    /// </summary>
    public void GenerateWorld()
    {
        // 부모가 비어있으면 자동 생성
        if (blocksParent == null)
        {
            blocksParent = new GameObject("GeneratedChunks").transform;
            blocksParent.parent = this.transform;
        }

        // 랜덤 초기화
        Random.InitState(seed);
        Debug.Log($"Seed: {seed}");

        // 월드 중심 기준으로 좌우/앞뒤로 청크 배치
        int half = worldSizeInChunks / 2;

        for (int cx = -half; cx <= half; cx++)
        {
            for (int cz = -half; cz <= half; cz++)
            {
                Vector2Int chunkCoord = new Vector2Int(cx, cz);
                GenerateChunkAt(chunkCoord);
            }
        }
    }

    /// <summary>
    /// 주어진 청크 좌표에 프리팹으로 청크 오브젝트를 생성하고 초기화
    /// </summary>
    /// <param name="coord">청크 좌표 (X, Z)</param>
    private void GenerateChunkAt(Vector2Int coord)
    {
        // 프리팹을 씬에 생성
        GameObject chunkGO = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity, blocksParent);

        // Chunk 스크립트를 가져와 초기화 실행
        Chunk chunk = chunkGO.GetComponent<Chunk>();
        chunk.Initialize(
            coord,
            chunkSize,
            seed,
            noiseScale,
            heightMultiplier
        );
    }
}