using UnityEngine;

public class CubeWorldGenerator : MonoBehaviour
{
    [Header("World Settings")]
    [SerializeField] private int worldWidth = 50;
    [SerializeField] private int worldDepth = 50;
    [SerializeField] private float noiseScale = 0.1f;
    [SerializeField] private int seed = 0;
    [SerializeField] private float heightMultiplier = 5f;
    [SerializeField] private GameObject grassBlockPrefab;
    [SerializeField] private GameObject stoneBlockPrefab;
    [SerializeField] private Transform blocksParent;

    private void Start()
    {
        GenerateWorld();
    }

    public void GenerateWorld()
    {
        if (blocksParent == null)
        {
            blocksParent = new GameObject("GeneratedBlocks").transform;
            blocksParent.parent = this.transform;
        }

        Random.InitState(seed);

        for (int x = 0; x < worldWidth; x++)
        {
            for (int z = 0; z < worldDepth; z++)
            {
                float noiseValue = Mathf.PerlinNoise((x + seed) * noiseScale, (z + seed) * noiseScale);
                int height = Mathf.FloorToInt(noiseValue * heightMultiplier);

                for (int y = 0; y <= height; y++)
                {
                    GameObject prefabToUse = (y == height) ? grassBlockPrefab : stoneBlockPrefab;
                    Vector3 position = new Vector3(x, y, z);
                    Instantiate(prefabToUse, position, Quaternion.identity, blocksParent);
                }
            }
        }
    }
}