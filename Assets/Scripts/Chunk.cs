using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// 하나의 청크(Chunk)를 구성하는 클래스
/// - Perlin Noise 기반으로 블록을 배치
/// - 메쉬 결합 사용
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
    private const float AtlasTotalTilesX = 4f; // 가로로 4칸 (1024 / 256 = 4)
    private const float AtlasTotalTilesY = 4f; // 세로로 4칸 (1024 / 256 = 4)

    // 타일 하나의 U, V 크기 (0~1 UV 공간 기준)
    private const float TileU = 1f / AtlasTotalTilesX; // 1 / 4 = 0.25
    private const float TileV = 1f / AtlasTotalTilesY; // 1 / 4 = 0.25
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
                for (int y = 0; y < chunkBuildHeight; y++)
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
        CreateChunkMesh();
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

        MeshCollider meshCollider = GetComponent<MeshCollider>(); // MeshCollider 컴포넌트 가져오기
        if (meshCollider != null)
        {
            // 기존 메시가 있다면 제거 후 새로 할당 (메시 업데이트 시 중요)
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh; // 생성된 메시를 MeshCollider에도 할당
            Debug.Log($"[Chunk {gameObject.name}] Mesh assigned to MeshCollider.");
        }
        else
        {
            Debug.LogWarning($"[Chunk {gameObject.name}] MeshCollider component not found!");
        }
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
    /// 특정 블록 타입과 면 방향에 대한 UV 좌표 배열을 반환(텍스처를 잘라서 면판별)
    /// </summary>
    /// <param name="blockType">현재 블록의 타입</param>
    /// <param name="faceIndex">면의 인덱스 (0:Back, 1:Front, 2:Top, 3:Bottom, 4:Left, 5:Right)</param>
    /// <returns>해당 면에 적용될 UV 좌표 4개 배열</returns>
    private Vector2[] GetFaceUVs(BlockType blockType, int faceIndex)
    {
        Vector2[] uvs = new Vector2[4];
        Vector2 tileOffset = Vector2.zero; // 아틀라스(청크하나 기준) 블록 텍스처의 시작 타일 좌표 (0,0 좌측하단 부터 시작)

        // 블록 타입에 따라 사용할 타일의 아틀라스 내 좌표(오프셋)를 결정
        switch (blockType)
        {
            case BlockType.Grass:
                // Grass 블록의 경우, 윗면(Top, faceIndex==2)은 풀 텍스처, 옆면은 풀+흙, 아랫면(Bottom, faceIndex==3)은 흙 텍스처
                if (faceIndex == 2) // Top
                    tileOffset = new Vector2(0, 3); // 아틀라스 (0,3) 위치에 풀 윗면 텍스처
                else if (faceIndex == 3) // Bottom
                    tileOffset = new Vector2(1, 2); // 아틀라스 (1,2) 위치에 흙 텍스처
                else // Sides
                    tileOffset = new Vector2(1, 3); // 아틀라스 (1,3) 위치에 풀 옆면 텍스처
                break;
            case BlockType.Stone:
                tileOffset = new Vector2(1, 2); // 아틀라스 (1,2) 위치에 돌 텍스처 (모든 면 동일 가정)
                break;
            
            default: // 정의되지 않은 블록은 기본 텍스처 또는 에러 텍스처
                tileOffset = new Vector2(AtlasTotalTilesX - 1, AtlasTotalTilesY - 1);
                Debug.LogWarning($"GetFaceUVs: Unhandled BlockType '{blockType}'. Using default UVs (Atlas last tile).");
                break;
        }
        // UV 좌표 계산 (타일 오프셋 * 타일 크기)를 기준으로 각 꼭짓점의 UV를 설정
        // UV는 0 1 사이의 값. 아틀라스 이미지의 왼쪽 하단이 (0,0), 오른쪽 상단이 (1,1)
        // 정점 순서: 왼쪽 아래, 왼쪽 위, 오른쪽 위, 오른쪽 아래 (AllFaceVertices[faceIndex] 와 동일한 순서여야 함)
        uvs[0] = new Vector2(tileOffset.x * TileU, tileOffset.y * TileV);                         // 좌하단
        uvs[1] = new Vector2(tileOffset.x * TileU, (tileOffset.y + 1) * TileV);                 // 좌상단
        uvs[2] = new Vector2((tileOffset.x + 1) * TileU, (tileOffset.y + 1) * TileV);           // 우상단
        uvs[3] = new Vector2((tileOffset.x + 1) * TileU, tileOffset.y * TileV);                 // 우하단

        return uvs;
    }
    /// <summary>
    /// 청크가 파괴될 때 메시를 명시적으로 파괴하여 메모리 누수를 방지
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
    /// <summary>
    /// 청크 내의 지정된 로컬 좌표의 블록 타입을 변경하고 메시를 업데이트
    /// </summary>
    /// <param name="localX">변경할 블록의 청크 내 X 좌표 (0 ~ chunkSize-1)</param>
    /// <param name="localY">변경할 블록의 청크 내 Y 좌표 (0 ~ chunkBuildHeight-1)</param>
    /// <param name="localZ">변경할 블록의 청크 내 Z 좌표 (0 ~ chunkSize-1)</param>
    /// <param name="newType">새로 설정할 블록 타입</param>
    /// <returns>변경 성공 여부</returns>
    public bool ChangeBlock(int localX, int localY, int localZ, BlockType newType)
    {
        // 1. 좌표 유효성 검사
        if (localX < 0 || localX >= chunkSize ||
            localY < 0 || localY >= chunkBuildHeight ||
            localZ < 0 || localZ >= chunkSize)
        {
            Debug.LogWarning($"ChangeBlock failed: Coordinate ({localX}, {localY}, {localZ}) is out of bounds for chunk {chunkCoord}.");
            return false; // 청크 범위 밖
        }

        // 변경할 위치의 현재 블록 타입 확인 (이미 같은 타입이거나 Air인데 Air로 바꾸려는 경우는 무시)
        if (blockData[localX, localY, localZ] == newType)
        {
            return false; // 변경 사항 없음
        }

        // 블록 데이터 업데이트
        blockData[localX, localY, localZ] = newType;

        // 메시 재생성 요청
        CreateChunkMesh(); // 변경된 데이터로 메시를 다시 만듦

        // 인접 청크 업데이트 확인
        CheckAndUpdateNeighborChunks(localX, localY, localZ);
        //TODO: 만약 변경된 블록이 청크 경계에 있다면 인접 청크의 메시도 업데이트해야 할 수 있음(이웃 블록의 노출 여부가 있기 때문)

        return true; // 변경 성공
    }
    private void CheckAndUpdateNeighborChunks(int localX, int localY, int localZ)
    {
        // 변경된 블록이 현재 청크의 경계면에 있는지 확인
        bool needsUpdateN = false, needsUpdateE = false, needsUpdateS = false, needsUpdateW = false;

        if (localX == 0) needsUpdateW = true;           // 서쪽 경계
        else if (localX == chunkSize - 1) needsUpdateE = true; // 동쪽 경계

        if (localZ == 0) needsUpdateS = true;           // 남쪽 경계
        else if (localZ == chunkSize - 1) needsUpdateN = true; // 북쪽 경계

        // Y축 경계는 보통 다른 청크와 맞닿지 않으므로 일반적으로는 고려하지 않음 (필요시 추가)

        // 만약 경계면에 있다면, ChunkManager를 통해 해당 방향의 이웃 청크를 찾아 메시 업데이트 요청
        if (needsUpdateN || needsUpdateE || needsUpdateS || needsUpdateW)
        {
            if (ChunkManager.Instance != null) // ChunkManager 인스턴스가 있는지 확인
            {
                if (needsUpdateN) ChunkManager.Instance.RequestChunkMeshUpdate(chunkCoord + Vector2Int.up); // 북쪽 청크
                if (needsUpdateE) ChunkManager.Instance.RequestChunkMeshUpdate(chunkCoord + Vector2Int.right); // 동쪽 청크
                if (needsUpdateS) ChunkManager.Instance.RequestChunkMeshUpdate(chunkCoord + Vector2Int.down); // 남쪽 청크
                if (needsUpdateW) ChunkManager.Instance.RequestChunkMeshUpdate(chunkCoord + Vector2Int.left); // 서쪽 청크
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