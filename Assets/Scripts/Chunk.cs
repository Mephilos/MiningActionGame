using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// 하나의 청크(Chunk)를 구성하는 클래스.
/// - Perlin Noise 기반으로 블록을 배치.
/// - BlockPool을 통해 오브젝트를 재사용.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    private Vector2Int chunkCoord;
    private int chunkSize; // X, Z 크기
    private int chunkBuildHeight; //청크의 최대 높이
    private int seed; 
    private float noiseScale;
    private float worldHeightMultiplier;//월드 굴곡 조절
    private BlockType[,,] blockData;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

//------------------------메쉬 정점 데이터---------------------------
    //텍스처 아틀라스 사이즈
    //아틀라스에 16*16의 타일이 있고, 타일 택스쳐 uv는 1/16;
    private const float TileTextureSize = 1f / 16f;
    //메시 생성에 쓰일 타일면의정점 데이터
    private static readonly Vector3[] FaceVertices_Back = { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0) }; // Z-
    private static readonly Vector3[] FaceVertices_Front = { new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 0, 1) }; // Z+
    private static readonly Vector3[] FaceVertices_Top = { new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0) }; // Y+
    private static readonly Vector3[] FaceVertices_Bottom = { new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 0, 0) }; // Y-
    private static readonly Vector3[] FaceVertices_Left = { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0) }; // X-
    private static readonly Vector3[] FaceVertices_Right = { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1) }; // X+
    //면을 구성하는 삼각형 인덱스
    private static readonly int[] FaceTriangles = { 0, 1, 2, 0, 2, 3};
    //맞닿은 블록 검사를 위한 오프셋
    private static readonly Vector3Int[] FaceCheckDirections = {
        new Vector3Int(0, 0, -1), // Back
        new Vector3Int(0, 0, 1),  // Front
        new Vector3Int(0, 1, 0),  // Top
        new Vector3Int(0, -1, 0), // Bottom
        new Vector3Int(-1, 0, 0), // Left
        new Vector3Int(1, 0, 0)   // Right
    };
    //정점 데이터를 모아놓은 베열
    private static readonly Vector3[][] AllFaceVertices = {
        FaceVertices_Back, FaceVertices_Front, 
        FaceVertices_Top, FaceVertices_Bottom, 
        FaceVertices_Left, FaceVertices_Right
    };

//------------------------------메쉬 정점 데이터 끝-----------------------------
    /// <summary>
    /// 청크 초기화 및 블록 생성
    /// </summary>
    public void Initialize(Vector2Int coord, int chunkSize, int buildHeight, int seed,
     float noiseScale, float heightMultiplier, Material chunkMaterial)
    {
        this.chunkCoord = coord;
        this.chunkSize = chunkSize;
        this.chunkBuildHeight = buildHeight; // 청크의 높이
        this.seed = seed;
        this.noiseScale = noiseScale;
        this.worldHeightMultiplier = heightMultiplier;

        //청크 이름
        gameObject.name = $"Chunk_{coord.x}_{coord.y}";
        
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if(chunkMaterial != null)
        {
            meshRenderer.material = chunkMaterial;
        }
        else
        {
            Debug.LogError($"[Chunk {gameObject.name}] Material 지정되지 않음");
        }

        blockData = new BlockType[chunkSize, chunkBuildHeight, chunkSize];

        PopulateBlockData();
    }

    /// <summary>
    /// 노이즈 기반 높이에 따라 블록 데이터 베열 기록
    /// GameObject 베제 데이터만 기록
    /// </summary>
    private void PopulateBlockData()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                //월드 좌표 (노이즈 계산시에 사용)
                int worldX = chunkCoord.x * chunkSize + x;
                int worldZ = chunkCoord.y * chunkSize + z;
                //perlin Noise 적용 지표면 높이 게산
                float noiseValue = Mathf.PerlinNoise((worldX + seed * 0.7385f) * noiseScale, (worldZ + seed * 0.1934f) * noiseScale);
                int surfaceheight = Mathf.FloorToInt(noiseValue * worldHeightMultiplier);

                surfaceheight = Mathf.Clamp(surfaceheight, 0, chunkBuildHeight - 1);

                // y(지상고?)를 따서 블록 타입 결정
                for (int y = 0; y <= chunkBuildHeight; y++)
                {
                    if(y > surfaceheight)
                    {
                        blockData[x, y, z] = BlockType.Air; //지표면 보다 높으면 빈공간
                    }
                    else if (y == surfaceheight)
                    {
                        blockData[x, y, z] = BlockType.Grass; //지표면은 풀
                    }
                    else
                    {
                        blockData[x,y,z] = BlockType.Stone; //아래는 돌
                    }
                }
            }
        }
    }


    ///<summary>
    /// 블록데이터 배열을 이용해서 렌더링
    /// </summary>
    public void CreateChunkMesh()
    {
        List<Vector3> vertices = new List<Vector3>(); //정점
        List<int> triangles = new List<int>(); //면구성 인덱스
        List<Vector2> uvs = new List<Vector2>(); //텍스처 uv값
    
        for(int y = 0; y < chunkBuildHeight; y++)
        {
            for(int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    BlockType currentBlockType = blockData[x, y, z];
                    //블록 위치가 빈공간이라면 메쉬 만들필요 없음
                    if(currentBlockType == BlockType.Air)
                        continue;
                    
                    //6방향의 이웃 블록을 확인하여 노출된 면만 메쉬 데이터 입력
                    for(int i = 0; i < 6; i++)//0:Back, 1:Front, 2:Top, 3:Bottom, 4:Left, 5:Right
                    {
                        Vector3Int neighborPos = new Vector3Int(x, y, z) + FaceCheckDirections[i];

                        if (IsNeighborBlockTransparent(neighborPos))
                        {
                            // 그릴 면 정점 카운터
                            int currentVertexCount = vertices.Count;

                            // 면의 정점 4개 추가 (로컬 좌표)
                            foreach (Vector3 vertexOffset in AllFaceVertices[i])
                            {
                                vertices.Add(new Vector3(x, y, z) + vertexOffset);
                            }

                            // 면의 UV 좌표 추가(아틀라스 기준)
                            uvs.AddRange(GetFaceUVs(currentBlockType, i));

                            // 면의 삼각형 인덱스 2개 추가
                            foreach (int triangleIndexOffset in FaceTriangles)
                            {
                                triangles.Add(currentVertexCount + triangleIndexOffset);
                            }
                        }
                    }
                }
            }
        }

        Mesh mesh = new Mesh();

        //유니티는 메시당 65535개의 정점 제한이 있음(16비트), 이보다 많은 정점 사용시 32비트 인덱스 버퍼 사용 명시
        if(vertices.Count > 65535) //정점이 65535를 넘은 시 32비트 인덱스 포멧 사용
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 
        }
        // 리스트의 데이터를 베열로 변환 하여 Mesh객체에 할당
        mesh.vertices = vertices.ToArray(); //ToArray: List ->Array로 변환
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals(); //법선 백터 계산(라이팅에 사용)
        mesh.RecalculateBounds();   // 메시 경계 계산 (컬링에 사용)

        //메쉬 필터에 할당.
        meshFilter.mesh = mesh;
    }

    /// <summary>
    /// 청크 내 로컬 좌표 기준 이웃 블록의 상태를 확인(판별용)
    /// </summary>
    private bool IsNeighborBlockTransparent(Vector3Int localNeighborPos)
    {
        // 청크 경계 바깥인지 확인
        if (localNeighborPos.x < 0 || localNeighborPos.x >= chunkSize ||
            localNeighborPos.y < 0 || localNeighborPos.y >= chunkBuildHeight ||
            localNeighborPos.z < 0 || localNeighborPos.z >= chunkSize)
        {
            return true; // 경계 바깥은 항상 노출된 면으로 간주
        }
        // 청크 내부라면 해당 위치의 블록이 Air 타입인지 확인
        return blockData[localNeighborPos.x, localNeighborPos.y, localNeighborPos.z] 
                == BlockType.Air;
    }
    // <summary>
    /// 특정 블록 타입과 면 방향에 대한 UV 좌표 배열을 반환합니다.(텍스처를 잘라서 면판별)
    /// </summary>
    /// <param name="blockType">현재 블록의 타입</param>
    /// <param name="faceIndex">면의 인덱스 (0:Back, 1:Front, 2:Top, 3:Bottom, 4:Left, 5:Right)</param>
    /// <returns>해당 면에 적용될 UV 좌표 4개 배열</returns>
    private Vector2[] GetFaceUVs(BlockType blockType, int faceIndex)
    {
        Vector2[] uvs = new Vector2[4];
        Vector2 tileOffset = Vector2.zero; // 아틀라스(청크하나 기준) 블록 텍스처의 시작 타일 좌표 (0,0 부터 시작)

        // 블록 타입에 따라 사용할 타일의 아틀라스 내 좌표(오프셋)를 결정
        switch (blockType)
        {
            case BlockType.Grass:
                // Grass 블록의 경우, 윗면(Top, faceIndex==2)은 풀 텍스처, 옆면은 풀+흙, 아랫면(Bottom, faceIndex==3)은 흙 텍스처
                if (faceIndex == 2) // Top
                    tileOffset = new Vector2(0, 0); // 아틀라스 (0,0) 위치에 풀 윗면 텍스처
                else if (faceIndex == 3) // Bottom
                    tileOffset = new Vector2(2, 0); // 아틀라스 (2,0) 위치에 흙 텍스처
                else // Sides
                    tileOffset = new Vector2(1, 0); // 아틀라스 (1,0) 위치에 풀 옆면 텍스처
                break;
            case BlockType.Stone:
                tileOffset = new Vector2(3, 0); // 아틀라스 (3,0) 위치에 돌 텍스처 (모든 면 동일 가정)
                break;
            
            default: // 정의되지 않은 블록은 기본 텍스처 또는 에러 텍스처
                tileOffset = new Vector2(15, 15); // 예: 아틀라스 맨 끝 타일
                break;
        }
        // UV 좌표 계산 (타일 오프셋 * 타일 크기)를 기준으로 각 꼭짓점의 UV를 설정
        // UV는 0~1 사이의 값. 아틀라스 이미지의 왼쪽 하단이 (0,0), 오른쪽 상단이 (1,1)
        // 정점 순서: 왼쪽 아래, 왼쪽 위, 오른쪽 위, 오른쪽 아래 (AllFaceVertices[faceIndex] 와 동일한 순서여야 함)
        uvs[0] = new Vector2(tileOffset.x * TileTextureSize, tileOffset.y * TileTextureSize);                         // 좌하단
        uvs[1] = new Vector2(tileOffset.x * TileTextureSize, (tileOffset.y + 1) * TileTextureSize);                 // 좌상단
        uvs[2] = new Vector2((tileOffset.x + 1) * TileTextureSize, (tileOffset.y + 1) * TileTextureSize);           // 우상단
        uvs[3] = new Vector2((tileOffset.x + 1) * TileTextureSize, tileOffset.y * TileTextureSize);                 // 우하단

        return uvs;
    }
    /// <summary>
    /// 청크가 파괴될 때 메시를 명시적으로 파괴하여 메모리 누수를 방지합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(meshFilter.sharedMesh);
            }
            else
            {
            }
        }
    }



    // /// <summary>
    // /// 청크가 제거될 때 블록을 모두 오브젝트 풀로 반환
    // /// </summary>
    // public void ReturnAllBlocks()
    // {
    //     for (int i = transform.childCount - 1; i >= 0; i--)
    //     {
    //         Transform child = transform.GetChild(i);
    //         GameObject block = child.gameObject;

    //         if (block != null && block.activeSelf) 
    //         {
    //             if (block.activeSelf)
    //             {
    //                 BlockPool.Instance.ReturnBlock(block);
    //                 child.SetParent(null);
    //             }
    //         }
    //     }
    // }
}