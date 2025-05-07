using UnityEngine;

/// <summary>
/// 하나의 청크(Chunk)를 구성하는 클래스.
/// - Perlin Noise 기반으로 블록을 배치.
/// - BlockPool을 통해 오브젝트를 재사용.
/// </summary>
public class Chunk : MonoBehaviour
{
    private Vector2Int chunkCoord;
    private int chunkSize;
    private int seed;
    private float noiseScale;
    private float heightMultiplier;

    /// <summary>
    /// 청크 초기화 및 블록 생성
    /// </summary>
    public void Initialize(Vector2Int coord, int chunkSize, int seed, float noiseScale, float heightMultiplier)
    {
        this.chunkCoord = coord;
        this.chunkSize = chunkSize;
        this.seed = seed;
        this.noiseScale = noiseScale;
        this.heightMultiplier = heightMultiplier;

        gameObject.name = $"Chunk_{coord.x}_{coord.y}";
        transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

        GenerateBlocks();
    }

    /// <summary>
    /// 노이즈 기반 높이에 따라 블록을 배치
    /// </summary>
    private void GenerateBlocks()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int worldX = chunkCoord.x * chunkSize + x;
                int worldZ = chunkCoord.y * chunkSize + z;

                float noiseValue = Mathf.PerlinNoise((worldX + seed * 73856093) * noiseScale, (worldZ + seed * 19349663) * noiseScale);
                int height = Mathf.FloorToInt(noiseValue * heightMultiplier);

                for (int y = 0; y <= height; y++)
                {
                    BlockType type = (y == height) ? BlockType.Grass : BlockType.Stone;
                    GameObject block = BlockPool.Instance.GetBlock(type);

                    if (block != null)
                    {
                        block.transform.SetParent(this.transform);
                        block.transform.position = new Vector3(worldX, y, worldZ);
                    }
#if UNITY_EDITOR
                    else
                    {
                        Debug.LogWarning($"[Chunk] Block of type {type} could not be fetched from the pool.");
                    }
#endif
                }
            }
        }
    }
    /// <summary>
    /// 청크가 제거될 때 블록을 모두 오브젝트 풀로 반환
    /// </summary>
    public void ReturnAllBlocks()
{
    for (int i = transform.childCount - 1; i >= 0; i--)
    {
        Transform child = transform.GetChild(i);
        GameObject block = child.gameObject;

        if (block != null)
        {
            BlockType type = block.name.Contains("Stone") ? BlockType.Stone : BlockType.Grass;

            if (block.activeSelf)
            {
                child.SetParent(null); // 🔑 부모(Chunk)에서 분리
                BlockPool.Instance.ReturnBlock(block);
            }
        }
    }
}
}