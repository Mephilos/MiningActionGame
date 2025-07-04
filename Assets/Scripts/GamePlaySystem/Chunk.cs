using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 메쉬 데이터 구조체
/// </summary>
public struct MeshData
{
    public List<Vector3> Vertices;
    public List<int> Triangles;
    public List<Vector2> Uvs;

    public MeshData(int initialCapacity = 0)
    {
        Vertices = new List<Vector3>(initialCapacity);
        Triangles = new List<int>(initialCapacity * 2);
        Uvs = new List<Vector2>(initialCapacity);
    }

    public void Clear()
    {
        Vertices.Clear();
        Triangles.Clear();
        Uvs.Clear();
    }
}

/// <summary>
/// 하나의 청크(Chunk)를 구성하는 클래스
/// Perlin Noise 기반으로 블록을 배치
/// 메쉬 결합 사용
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(NavMeshSurface))]
public class Chunk : MonoBehaviour
{
    private Vector2Int _chunkCoordinates;
    private int _chunkSize; // X, Z 크기
    private int _chunkHeight; //청크의 최대 높이
    private int _seed;
    private float _noiseScale;
    private float _heightMultiplier;//월드 굴곡 조절
    private BlockType[,,] _blockData;
    private int[,] _surfaceHeights; // 각 (x,z) 위치의 표면 높이를 저장할 배열
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private NavMeshSurface _navMeshSurface; // 네비메쉬 컴포넌트 참조
    private StageThemeData _currentThemeData;
    private List<Bounds> _objectBounds = new List<Bounds>(); // 겹침 방지위한 바운딩 박스 리스트
    
    [Header("청크 형태, 아이템 매몰 설정")] [Tooltip("함정 설정")]
    public bool enableWideTrapHoles = false;
    [Tooltip("함정 구멍 크기")]
    public float trapHoleNoiseScale = 0.02f;
    [Tooltip("함정 구멍 생성 빈도")]
    [Range(0f, 1f)]
    public float trapHoleThreshold = 0.45f;
    public int trapHoleSeedOffset = 3000;
    [Tooltip("청크 바닥 설정")]
    public float terrainExistenceThreshold = 0.2f; // 노이즈 값에 따라 지형이 생성되지 않는 부분 생기게
    public GameObject waterPlane;
    [Tooltip("청크 안에 매설될 프리팹")]
    public GameObject itemPrefab;
    [Tooltip("청크내에 매설될 확률")] [Range(0f, 1f)]
    public float itemSpawnChance = 0.1f;
    public int minItemSpawnY = 1;
    public int maxItemSpawnY = 30;
    




    //------------------------------메쉬 정점 데이터-----------------------------
    private const float AtlasTotalTilesX = 4f; // 가로로 4칸 (1024 / 256 = 4)
    private const float AtlasTotalTilesY = 4f; // 세로로 4칸 (1024 / 256 = 4)

    // 타일 하나의 U, V 크기 (0~1 UV 공간 기준)
    private const float TileU = 1f / AtlasTotalTilesX; // 1 / 4 = 0.25
    private const float TileV = 1f / AtlasTotalTilesY; // 1 / 4 = 0.25
    //메시 생성에 쓰일 타일면의정점 데이터
    private static readonly Vector3[] FaceVerticesBack = { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0) }; // Z-
    private static readonly Vector3[] FaceVerticesFront = { new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 0, 1) }; // Z+
    private static readonly Vector3[] FaceVerticesTop = { new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0) }; // Y+
    private static readonly Vector3[] FaceVerticesBottom = { new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 0, 0) }; // Y-
    private static readonly Vector3[] FaceVerticesLeft = { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0) }; // X-
    private static readonly Vector3[] FaceVerticesRight = { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1) }; // X+
    //면을 구성하는 삼각형 인덱스
    private static readonly int[] FaceTriangles = { 0, 1, 2, 0, 2, 3 };
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
        FaceVerticesBack, FaceVerticesFront,
        FaceVerticesTop, FaceVerticesBottom,
        FaceVerticesLeft, FaceVerticesRight
    };
    //------------------------------메쉬 정점 데이터 끝-----------------------------

    /// <summary>
    /// 청크 초기화 및 블록 생성
    /// </summary>
    public void Initialize(Vector2Int coord, int chunkSize, int buildHeight, int seed,
     float noiseScale, float heightMultiplier, Material chunkMaterial, StageThemeData themeData)
    {
        this._chunkCoordinates = coord;
        this._chunkSize = chunkSize;
        this._chunkHeight = buildHeight; // 청크의 높이
        this._seed = seed;
        this._noiseScale = noiseScale;
        this._heightMultiplier = heightMultiplier;
        this._currentThemeData = themeData;

        //청크 이름
        // 테마 데이터
        gameObject.name = $"StageChunk";
        
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _navMeshSurface = GetComponent<NavMeshSurface>();

        if (themeData != null) 
        {
            gameObject.name = $"{themeData.themeName}_Chunk_{coord.x}_{coord.y}";
            if (themeData.chunkMaterial != null) 
            { 
                // 테마의 머티리얼 사용
                _meshRenderer.material = themeData.chunkMaterial;
            } 
            else if (chunkMaterial != null) 
            { 
                // 테마 머티리얼 없으면 기본 머티리얼
                _meshRenderer.material = chunkMaterial;
            }
        } 
        else if (chunkMaterial != null) 
        { 
            // 테마도 기본도 없으면 경고
            _meshRenderer.material = chunkMaterial;
        }
        else 
        {
            Debug.LogError($"[{gameObject.name}] Material이 지정되지 않았습니다 (테마 및 기본 모두).");
        }

        _navMeshSurface.collectObjects = CollectObjects.Volume;
        _navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;

        _navMeshSurface.size = new Vector3(this._chunkSize, this._chunkHeight, this._chunkSize);
        _navMeshSurface.center = new Vector3(this._chunkSize / 2.0f, this._chunkHeight / 2.0f, this._chunkSize / 2.0f);

        int groundLayer = LayerMask.NameToLayer("Ground");

        _navMeshSurface.layerMask = 1 << groundLayer;
        
        gameObject.layer = groundLayer;

        if (chunkMaterial != null)
        {
            _meshRenderer.material = chunkMaterial;
        }
        else
        {
            Debug.LogError($"[Chunk {gameObject.name}] Material 지정되지 않음");
        }
        this._surfaceHeights = new int[chunkSize, chunkSize];
        _blockData = new BlockType[chunkSize, _chunkHeight, chunkSize];
        _objectBounds.Clear();

        PopulateBlockData();
        CreateBedrockPlane();
        
        if (_currentThemeData != null)
        {
            SpawnObjectsFromTheme(_currentThemeData.destructibleObjects, true);
            SpawnObjectsFromTheme(_currentThemeData.decorativeObjects, false);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] CurrentThemeData null");
        }
    }

    /// <summary>
    /// 노이즈 기반 높이에 따라 블록 데이터 베열 기록
    /// GameObject 베제 데이터만 기록
    /// </summary>
    private void PopulateBlockData()
    {
        for (int x = 0; x < _chunkSize; x++)
        {
            for (int z = 0; z < _chunkSize; z++)
            {
                //월드 좌표 (노이즈 계산시에 사용)
                int worldX = _chunkCoordinates.x * _chunkSize + x;
                int worldZ = _chunkCoordinates.y * _chunkSize + z;
                
                bool createTrapHole = false;
                if (enableWideTrapHoles)
                {
                    // 함정 구멍용 Perlin Noise 값 계산
                    // 지형 높이 노이즈와 다른 시드 오프셋 및 스케일 사용
                    float trapNoiseVal = Mathf.PerlinNoise(
                        (worldX + (_seed + trapHoleSeedOffset) * 0.4f) * trapHoleNoiseScale, // 시드 조합 및 스케일 적용
                        (worldZ + (_seed + trapHoleSeedOffset) * 0.7f) * trapHoleNoiseScale  // 다른 축에도 적용
                    );

                    if (trapNoiseVal < trapHoleThreshold)
                    {
                        createTrapHole = true;
                    }
                }

                if (createTrapHole) // 넓은 함정 구멍을 생성해야 한다면, 이 열은 전부 공기
                {
                    _surfaceHeights[x, z] = -1; // 표면 없음으로 표시
                    for (int y = 0; y < _chunkHeight; y++)
                    {
                        _blockData[x, y, z] = BlockType.Air;
                    }
                    continue;
                }
                // 지형 높이 및 존재 여부를 위한 Perlin Noise 값 계산
                float heightNoiseValue = Mathf.PerlinNoise(
                    (worldX + _seed * 0.7385f) * _noiseScale, // 기존 _noiseScale 사용 (지형 높이용)
                    (worldZ + _seed * 0.1934f) * _noiseScale  
                );

                // 지형 높이 노이즈 값이 terrainExistenceThreshold보다 낮은 경우, 해당 (x,z) 열은 전부 공기 (자연스러운 물가/저지대)
                if (heightNoiseValue < terrainExistenceThreshold) 
                {
                    _surfaceHeights[x, z] = -1; 
                    for (int y = 0; y < _chunkHeight; y++)
                    {
                        _blockData[x, y, z] = BlockType.Air; 
                    }
                }
                else // 지형 생성
                {
                    int calculatedSurfaceHeight = Mathf.FloorToInt(heightNoiseValue * _heightMultiplier); 
                    calculatedSurfaceHeight = Mathf.Clamp(calculatedSurfaceHeight, 0, _chunkHeight - 1); 
                    _surfaceHeights[x, z] = calculatedSurfaceHeight; 

                    for (int y = 0; y < _chunkHeight; y++) 
                    {
                        if (y > calculatedSurfaceHeight) _blockData[x, y, z] = BlockType.Air; 
                        else if (y == calculatedSurfaceHeight) _blockData[x, y, z] = BlockType.Grass; 
                        else _blockData[x, y, z] = BlockType.Stone; 
                    }
                }
            }
        }
        CreateChunkMesh();
    }
    /// <summary>
    /// 청크 내의 랜덤한 위치에 아이템 메설
    /// </summary>
    private void SpawnItemInChunk()
    {
        // 아이템 프리팹 설정 확인
        if (itemPrefab == null)
        {
            return; // 프리팹 없으면 실행 중단
        }

        if (Random.value > itemSpawnChance) // Random.value는 0.0과 1.0 사이의 랜덤 실수를 반환
        {
            return; // 확률에 당첨되지 않으면 실행 중단
        }
        
        // 아이템을 매설할 랜덤한 청크 내부 X, Z 좌표 선택
        int x = Random.Range(1, _chunkSize - 1); // 1 이상 (chunkSize-1) 미만의 정수
        int z = Random.Range(1, _chunkSize - 1);


        // 지정된 Y 범위 안에서 위에서 아래로 스캔하여 Stone 블록이 처음 나오는 위치를 찾아서 해당 좌표에 아이템을 숨김
        int spawnY = -1; // 매설될 Y 좌표 변수

        // minItemSpawnY 부터 maxItemSpawnY 까지 반복 탐색
        // (안전하게 chunkBuildHeight 미만으로도 제한)
        for (int y = Mathf.Clamp(minItemSpawnY, 0, _chunkHeight - 1); y <= Mathf.Clamp(maxItemSpawnY, 0, _chunkHeight - 1); y++)
        {
            // 현재 (x, y, z) 위치의 블록 타입 확인
            if (_blockData[x, y, z] == BlockType.Stone) // 만약 돌 블록이라면
            {
                // 그리고 그 바로 위 칸이 공기가 아니라면 (즉, 완전히 땅 속에 묻히도록) - 선택 사항
                if (y + 1 < _chunkHeight && _blockData[x, y + 1, z] != BlockType.Air)
                {
                    spawnY = y; // 매설
                    break;
                }
            }
        }
        if (spawnY != -1)
        {
            // 실제 아이템 오브젝트 생성
            Vector3 itemPosition = new Vector3(x + 0.5f, spawnY + 0.5f, z + 0.5f); // 블록 중앙에 위치하도록 0.5f 더함
            GameObject spawnedItem = Instantiate(itemPrefab, this.transform); // 청크의 자식으로 생성
            spawnedItem.transform.localPosition = itemPosition; // 로컬 위치 설정
            spawnedItem.name = $"{itemPrefab.name}_({_chunkCoordinates.x * _chunkSize + x},{spawnY},{_chunkCoordinates.y * _chunkSize + z})"; // 월드 좌표로 이름 설정
            Debug.Log($"아이템 생성{spawnedItem.name}");
        }
    }

    ///<summary>
    /// 블록데이터 배열을 이용해서 렌더링
    /// </summary>
    public void CreateChunkMesh()
    {
        MeshData meshData = new MeshData(_chunkSize * _chunkHeight * _chunkSize / 2);

        for (int y = 0; y < _chunkHeight; y++)
        {
            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    BlockType currentBlockType = _blockData[x, y, z];
                    //블록 위치가 빈공간이라면 메쉬 만들필요 없음
                    if (currentBlockType == BlockType.Air)
                        continue;

                    AddVisibleFacesToMeshData(x, y, z, currentBlockType, ref meshData);
                }
            }
        }
        ApplyMeshDataToFilter(meshData);
        ApplyMeshToCollider(_meshFilter.sharedMesh);

        // NavMeshManager를 통해 NavMesh 빌드/리빌드 요청
        if (NavMeshManager.Instance != null)
        {
            if (_navMeshSurface != null)
            {
                // Chunk가 생성/업데이트될 때마다 NavMeshManager에 등록하고 빌드를 요청
                NavMeshManager.Instance.RegisterAndBakeSurface(_navMeshSurface);
            }
        }
    }
    /// <summary>
    /// 블록의 보이는 면에 대한 메시 데이터를 추가합니다
    /// </summary>
    /// <param name="x">블록 로컬 X</param>
    /// <param name="y">블록 로컬 Y</param>
    /// <param name="z">블록 로컬 Z</param>
    /// <param name="blockType">블록의 타입</param>
    /// <param name="meshData">업데이트 할 MeshData 참조</param>
    private void AddVisibleFacesToMeshData(int x, int y, int z, BlockType blockType, ref MeshData meshData)
    {   
        // 6방향의 이웃 블록을 확인하여 노출된 면만 메쉬 데이터 입력
        for (int i = 0; i < 6; i++)
        {
            Vector3Int neighborPos = new Vector3Int(x, y, z) + FaceCheckDirections[i];

            if (IsNeighborBlockTransparent(neighborPos))
            {
                AddFaceData(new Vector3Int(x, y, z), i, blockType, ref meshData);
            }
        }
    }
    /// <summary>
    /// 단일 면 정점, 삼각형 인덱스, UV 데이터를 meshData에 추가
    /// </summary>
    /// <param name="blockLocalPos">블록의 로컬 좌표 (x,y,z)</param>
    /// <param name="faceIndex">면의 인덱스 6개</param>
    /// <param name="blockType">블록의 타입</param>
    /// <param name="meshData">업데이트할 MeshData 참조</param>
    private void AddFaceData(Vector3Int blockLocalPos, int faceIndex, BlockType blockType, ref MeshData meshData)
    {
        int currentVertexCount = meshData.Vertices.Count; 

        // 면의 정점 4개 추가 (로컬 좌표)
        foreach (Vector3 vertexOffset in AllFaceVertices[faceIndex]) 
        {
            meshData.Vertices.Add(blockLocalPos + vertexOffset); 
        }

        // 면의 UV 좌표 추가(아틀라스 기준)
        meshData.Uvs.AddRange(GetFaceUVs(blockType, faceIndex)); 

        // 면의 삼각형 인덱스 2개 추가
        foreach (int triangleIndexOffset in FaceTriangles) 
        {
            meshData.Triangles.Add(currentVertexCount + triangleIndexOffset); 
        }
    }
    /// <summary>
    /// 수집된 MeshData를 사용, 실제 Mesh 객체를 만들고 MeshFilter에 적용
    /// </summary>
    /// <param name="meshData">적용할 메시 데이터</param>
    private void ApplyMeshDataToFilter(MeshData meshData)
    {
        Mesh mesh = new Mesh(); 

        if (meshData.Vertices.Count > 65535) 
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 
        }

        mesh.vertices = meshData.Vertices.ToArray(); 
        mesh.triangles = meshData.Triangles.ToArray(); 
        mesh.uv = meshData.Uvs.ToArray(); 

        mesh.RecalculateNormals(); 
        mesh.RecalculateBounds(); 

        _meshFilter.mesh = mesh; 
    }
    /// <summary>
    /// 생성된 메시를 MeshCollider에 적용
    /// </summary>
    /// <param name="meshToApply">MeshCollider에 적용할 메시</param>
    private void ApplyMeshToCollider(Mesh meshToApply)
    {
        MeshCollider meshCollider = GetComponent<MeshCollider>(); 
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null; 
            meshCollider.sharedMesh = meshToApply; 
        }
    }
    /// <summary>
    /// 청크 내 로컬 좌표 기준 이웃 블록의 상태를 확인(판별용)
    /// </summary>
    private bool IsNeighborBlockTransparent(Vector3Int localNeighborPos)
    {
        // 청크 경계 바깥인지 확인
        if (localNeighborPos.x < 0 || localNeighborPos.x >= _chunkSize ||
            localNeighborPos.y < 0 || localNeighborPos.y >= _chunkHeight ||
            localNeighborPos.z < 0 || localNeighborPos.z >= _chunkSize)
        {
            return true; // 경계 바깥은 항상 노출된 면으로 간주
        }
        // 청크 내부라면 해당 위치의 블록이 Air 타입인지 확인
        return _blockData[localNeighborPos.x, localNeighborPos.y, localNeighborPos.z]
                == BlockType.Air;
    }
    /// <summary>
    /// 특정 블록 타입과 면 방향에 대한 UV 좌표 배열을 반환(텍스처를 잘라서 면판별)
    /// </summary>
    /// <param name="blockType">현재 블록의 타입</param>
    /// <param name="faceIndex">면의 인덱스</param>
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
        if (_meshFilter != null && _meshFilter.sharedMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_meshFilter.sharedMesh);
            }
        }
    }
    /// <summary>
    /// 청크 내의 지정된 로컬 좌표의 블록 타입을 변경하고 메시를 업데이트(맵 파괴시 파괴된 부분 업데이트)
    /// </summary>
    /// <param name="localX">변경할 블록의 청크 내 X 좌표 (0 ~ chunkSize-1)</param>
    /// <param name="localY">변경할 블록의 청크 내 Y 좌표 (0 ~ chunkBuildHeight-1)</param>
    /// <param name="localZ">변경할 블록의 청크 내 Z 좌표 (0 ~ chunkSize-1)</param>
    /// <param name="newType">새로 설정할 블록 타입</param>
    /// <returns>변경 성공 여부</returns>
    public bool ChangeBlock(int localX, int localY, int localZ, BlockType newType)
    {
        // 좌표 유효성 검사
        if (localX < 0 || localX >= _chunkSize ||
            localY < 0 || localY >= _chunkHeight ||
            localZ < 0 || localZ >= _chunkSize)
        {
            return false; // 청크 범위 밖
        }

        // 변경할 위치의 현재 블록 타입 확인 (이미 같은 타입이거나 Air인데 Air로 바꾸려는 경우는 무시)
        if (_blockData[localX, localY, localZ] == newType)
        {
            return false; // 변경 사항 없음
        }

        // 블록 데이터 업데이트
        _blockData[localX, localY, localZ] = newType;

        // 메시 재생성 요청
        CreateChunkMesh(); // 변경된 데이터로 메시를 다시 만듬

        return true;
    }

    /// <summary>
    /// 청크의 가장 밑바닥에 Plane을 생성
    /// </summary>
    private void CreateBedrockPlane()
    {
        GameObject bedrockPlane = Instantiate(waterPlane, this.transform);
        bedrockPlane.name = "WaterPlaneInstance";
        //청크 가운데에 Plane 위치 시킴
        bedrockPlane.transform.localPosition = new Vector3(_chunkSize / 2.0f, 0f, _chunkSize / 2.0f);
        //청크 크기에 맞춰 조정 (UnityPlane은 1이 = 10유닛임)
        bedrockPlane.transform.localScale = new Vector3(_chunkSize / 10.0f, 1.0f, _chunkSize / 10.0f);
        //최적화 관련 설정
        bedrockPlane.isStatic = true;
    }

    /// <summary>
    /// 청크 내 로컬 x, z 좌표에 해당하는 지표면의 Y 높이를 반환합니다.
    /// </summary>
    public int GetSurfaceHeightAt(int localX, int localZ)
    {
        if (_surfaceHeights == null)
        {
            Debug.LogError($"[{gameObject.name}] surfaceHeights 배열이 초기화되지 않음 Chunk.Initialize() 또는 PopulateBlockData()를 확인");
            return 0; // 또는 적절한 기본값, 혹은 예외 발생
        }
        if (localX >= 0 && localX < _chunkSize && localZ >= 0 && localZ < _chunkSize)
        {
            return _surfaceHeights[localX, localZ];
        }
        Debug.LogWarning($"GetSurfaceHeightAt: 유효하지 않은 로컬 좌표 ({localX}, {localZ}). 청크 크기: {_chunkSize}. 기본 높이 0을 반환");
        return 0;
    }

    /// <summary>
    /// 테마 데이터를 기반으로 파괴 가능 오브젝트, 장식용 오브젝트 스폰
    /// </summary>
    /// <param name="objectDataArray">스폰할 오브젝트 데이터 배열</param>
    /// <param name="isDestructible">파괴 가능한 오브젝트인지 여부</param>
    /// <typeparam name="T">DestructibleObjectData or DecorativeObjectData</typeparam>
    private void SpawnObjectsFromTheme<T>(T[] objectDataArray, bool isDestructible) where T : ScriptableObject, IWeightedItem
    {
        if (objectDataArray == null || objectDataArray.Length == 0 || _currentThemeData == null) return;

        int maxAttempts = isDestructible
            ? _currentThemeData.maxDestructibleSpawnAttempts
            : _currentThemeData.maxDecorativeSpawnAttempts;
        float chancePerAttempt = isDestructible
            ? _currentThemeData.destructibleSpawnChancePerAttempt
            : _currentThemeData.decorativeSpawnChancePerAttempt;

        int spawnedCount = 0;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (Random.value > chancePerAttempt) continue; // 각 시도별 스폰 확률

            T selectedData = GetWeightedRandomObject(objectDataArray);
            if (selectedData == null) continue;

            // ScriptableObject에서 공통 속성 가져오기 (리플렉션이나 인터페이스 사용 가능, 여기서는 캐스팅)
            GameObject prefabToSpawn = null;
            float yOffset = 0f;
            float collisionRadius = 0.5f;
            BlockType[] placeableTypes = null;

            if (selectedData is DestructibleObjectData dod)
            {
                prefabToSpawn = dod.prefab;
                yOffset = dod.yOffset;
                collisionRadius = dod.collisionRadius;
                placeableTypes = dod.placeableOnBlockTypes;
            }
            else if (selectedData is DecorativeObjectData decOd)
            {
                prefabToSpawn = decOd.prefab;
                yOffset = decOd.yOffset;
                collisionRadius = decOd.collisionRadius;
                placeableTypes = decOd.placeableOnBlockTypes;
            }

            if (prefabToSpawn == null || placeableTypes == null || placeableTypes.Length == 0) continue;

            // 스폰 위치 찾기 (최대 10번 시도)
            for (int posAttempt = 0; posAttempt < 10; posAttempt++)
            {
                int x = Random.Range(0, _chunkSize);
                int z = Random.Range(0, _chunkSize);
                int surfaceY = GetSurfaceHeightAt(x, z);

                if (surfaceY != -1) // 유효한 표면 높이
                {
                    BlockType surfaceBlockType = _blockData[x, surfaceY, z];
                    bool canPlaceOnBlock = System.Array.Exists(placeableTypes, type => type == surfaceBlockType);

                    if (canPlaceOnBlock)
                    {
                        Vector3 potentialSpawnPosition = new Vector3(x + 0.5f, surfaceY + yOffset, z + 0.5f);
                        Bounds newObjectBounds =
                            new Bounds(potentialSpawnPosition, Vector3.one * collisionRadius * 2); // 반지름 기반 바운드

                        // 겹침 방지 확인
                        bool overlaps = false;
                        foreach (Bounds placedBound in _objectBounds)
                        {
                            if (placedBound.Intersects(newObjectBounds))
                            {
                                overlaps = true;
                                break;
                            }
                        }

                        if (!overlaps)
                        {
                            GameObject objInstance = Instantiate(prefabToSpawn, this.transform);
                            objInstance.transform.localPosition = potentialSpawnPosition;
                            objInstance.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                            // objInstance.layer = LayerMask.NameToLayer("PlacedObjects"); // 겹침 방지 레이어 설정

                            if (isDestructible && selectedData is DestructibleObjectData destructibleData)
                            {
                                Destructible destructibleComp = objInstance.AddComponent<Destructible>();
                                destructibleComp.Initialize(destructibleData);
                            }

                            _objectBounds.Add(newObjectBounds); // 배치된 오브젝트 목록에 추가 (로컬 좌표 기준 바운드)
                            spawnedCount++;
                            
                            break;
                        }
                    }
                }
            }
        }
    }
    private T GetWeightedRandomObject<T>(T[] objects) where T : ScriptableObject, IWeightedItem // 제네릭 제약 조건에 IWeightedItem 추가
    {
        if (objects == null || objects.Length == 0) return null;

        // 가중치가 0보다 큰 오브젝트만 필터링하여 사용
        List<T> weightedObjects = objects.Where(obj => obj.SpawnWeight > 0).ToList();
        if (weightedObjects.Count == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        foreach (T objData in weightedObjects)
        {
            totalWeight += objData.SpawnWeight;
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        float randomPoint = Random.value * totalWeight;
        foreach (T objData in weightedObjects)
        {
            if (randomPoint < objData.SpawnWeight)
            {
                return objData;
            }
            randomPoint -= objData.SpawnWeight;
        }
        return weightedObjects.Count > 0 ? weightedObjects[weightedObjects.Count - 1] : null;
    }
}