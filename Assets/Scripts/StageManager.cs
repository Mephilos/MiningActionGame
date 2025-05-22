using UnityEngine;
using System.Collections; 


public class StageManager : MonoBehaviour
{
    // TODO:스테이지 셋팅
    // [System.Serializable]
    // public class StageConfig
    // {
    //     public float timeToSurvive;
    //     // public int enemySpawnConfigId; // 적 스폰 설정 ID 등 추가 가능
    // }
    // public List<StageConfig> stageProgression; // 스테이지 진행 순서 및 설정 리스트
    
    public static StageManager Instance { get; private set; }
    
    [Header("Stage (Chunk) Settings")]
    [SerializeField] private GameObject stageChunkPrefab;
    [SerializeField] private Material stageSharedMaterial;
    [SerializeField] public int stageSize = 16;
    [SerializeField] private int stageBuildHeight = 64;
    [SerializeField] private int baseWorldSeed = 12345;
    
    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;

    [Header("Stage Progression Settings")] [Tooltip("각 스테이지에서 버텨야 하는 시간")]
    [SerializeField] private float timeToSurvivePerStage = 60f;

    [Header("Terrain Variation Settings")]
    [Tooltip("스테이지별 노이즈 최소 스케일")]
    [SerializeField] private float minNoiseScale = 0.02f;
    [Tooltip("스테이지별 노이즈 최대 스케일")]
    [SerializeField] private float maxNoiseScale = 0.04f;
    [Tooltip("스테이지별 최소 지형 높이")]
    [SerializeField] private float minHeightMultiplier = 5f;
    [Tooltip("스테이지별 최대 지형 높이")]
    [SerializeField] private float maxHeightMultiplier = 10f;
    [Tooltip("스테이지 시드 값 계수")]
    [SerializeField] private int seedMultiplier = 100;
    
    private Chunk _currentLoadedStageChunk;
    private float _currentStageTimer; // 현재 스테이지에서 흐른 시간
    private int _currentStageNumber = 1; // 현재 스테이지 번호 (1부터 시작)
    private bool _isLoadingNextStage;// 다음 스테이지 로딩 중인지 여부 (중복 호출 방지)
    private bool _isWaitingForPlayerToProceed;
    private bool _isGameOver; 
    
    private readonly Vector2Int _fixedStageCoordinate = Vector2Int.zero;
    public Vector2Int CurrentStageCoord => _fixedStageCoordinate;
    
    
    private void Awake()
    {
        InitializeSingleton();
        InitializePlayerObj();
    }

    private void Start()
    {
        if (stageChunkPrefab == null || stageSharedMaterial == null)
        {
            Debug.LogError("[StageManager] StageChunkPrefab 또는 StageSharedMaterial이 할당되지 않았습니다");
            enabled = false;
            return;
        }
        if (playerTransform == null)
        {
            Debug.LogError("[StageManager] Player Transform이 없어 게임을 시작할 수 없습니다");
            enabled = false;
            return;
        }
        StartNewGame(); // 새 게임 시작 또는 첫 스테이지 로드
    }

    /// <summary>
    /// 새 게임을 시작하거나 첫 번째 스테이지를 로드
    /// </summary>
    private void StartNewGame()
    {
        Debug.Log("호출언제? StartNewGame()");
        _currentStageNumber = 1;
        _isLoadingNextStage = false;
        _isWaitingForPlayerToProceed = false;
        _isGameOver = false;    // 게임 오버 상태 초기화
        Time.timeScale = 1f;    // 게임 시간 정상화

        if (playerTransform != null)
        {
            PlayerData playerData = playerTransform.GetComponent<PlayerData>();
            if (playerData != null)
            {
                playerData.ReviveAndReset();
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideGameOverScreem();
            UIManager.Instance.UpdateStageNumberUI(_currentStageNumber);
            UIManager.Instance.HideStageClearScreen();
            UIManager.Instance.HideShopPanel();
        }
        LoadStageAndStartTimer(_fixedStageCoordinate);
        Debug.Log("게임 시작 스테이지: " + _currentStageNumber);
    }
    private void Update()
    {
        if (_isGameOver || _isLoadingNextStage || _isWaitingForPlayerToProceed)
        {
            //즉시 재시작 기능 (임시)
            if (_isGameOver && Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("[StageManager] R키 입력 게임 재시작");
                RestartGame();
            }
            return;
        }

        // 현재 스테이지 타이머 업데이트
        _currentStageTimer += Time.deltaTime;
        if (UIManager.Instance != null)
        {
            float timeLeft = timeToSurvivePerStage - _currentStageTimer;
            UIManager.Instance.UpdateStageTimerUI(timeLeft > 0 ? timeLeft : 0f);
        }
        // 버티기 시간 충족 시 다음 스테이지로
        if (_currentStageTimer >= timeToSurvivePerStage)
        {
            ProceedToNextStage();
        }
    }
    /// <summary>
    /// 플레이어 죽음 처리 (EnemySpawner Instance 사용)
    /// </summary>
    public void HandlePlayerDeath()
    {
        if (_isGameOver) return;
        _isGameOver = true;
        _isLoadingNextStage = false;
        _isWaitingForPlayerToProceed = false;
        
        Debug.Log("[StageManager] 플레이어 사망처리 시작]");

        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.StopAndClearAllEnemies();
        }
        else
        {
            Debug.LogWarning("[StageManager] EnemySpawner 인스턴스를 찾을 수 없음");
        }
        
        Time.timeScale = 0f;
        Debug.Log("GameOver");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverScreem();
        }
        else
        {
            Debug.LogWarning("[StageManager] UIManager Instance를 찾을 수 없음");
        }
    }

    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {   
        Debug.Log("[StageManager] 게임 재시작");
        if (_currentLoadedStageChunk != null)
        {
            Destroy(_currentLoadedStageChunk.gameObject);
            _currentLoadedStageChunk = null;
        }
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.StopAndClearAllEnemies();
        }
        StartNewGame();
    }

    /// <summary>
    /// 다음 스테이지로 진행하는 로직을 시작합니다
    /// </summary>
    private void ProceedToNextStage()
    {
        _isLoadingNextStage = true;
        _isWaitingForPlayerToProceed = true;
        _currentStageTimer = 0f; // 타이머 리셋

        Debug.Log($"[StageManager] 스테이지 {_currentStageNumber} 클리어 버튼을 눌러 스테이지로 이동");
        
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.StopAndClearAllEnemies(); // 현재 스테이지의 적들 정리
        }
        else
        {
            Debug.LogWarning("[StageManager] EnemySpawner 인스턴스를 찾을 수 없어 적을 정리할 수 없음");
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowStageClearScreen(); // 스테이지 클리어 UI 표시
        }
    }


    public void PlayerConfirmedShop()
    {
        if (!_isWaitingForPlayerToProceed) // 진행중, 대기 상태 확인
        {
            Debug.LogWarning("[StageManager] 대기 상태가 아닙니다");
            return;
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideStageClearScreen();
            UIManager.Instance.ShowShopPanel();
        }
    }

    public void PlayerConfirmedNextStage()
    {
        if (!_isWaitingForPlayerToProceed) return;
        _isWaitingForPlayerToProceed = false;

        _currentStageNumber++;
        _isLoadingNextStage = true;
        LoadStageAndStartTimer(_fixedStageCoordinate);

    }

    /// <summary>
    /// 지정된 좌표에 스테이지를 로드하고 타이머를 시작
    /// 스테이지 번호 표시
    /// </summary>
    private void LoadStageAndStartTimer(Vector2Int stageCoord)
    {
        if (UIManager.Instance != null && UIManager.Instance.stageClearPanel != null 
                                       && UIManager.Instance.shopPanel != null && UIManager.Instance.shopPanel.activeSelf)
        {
            UIManager.Instance.HideShopPanel();
        }
        // 이전 스테이지 언로드 및 새 스테이지 로드
        if (_currentLoadedStageChunk != null)
        {
            Destroy(_currentLoadedStageChunk.gameObject);
            _currentLoadedStageChunk = null;
        }

        Vector3 stageWorldPosition = new Vector3(stageCoord.x * stageSize, 0f, stageCoord.y * stageSize);
        GameObject stageGO = Instantiate(stageChunkPrefab, stageWorldPosition, Quaternion.identity, this.transform);
        Chunk chunkScript = stageGO.GetComponent<Chunk>();
        if (chunkScript == null) 
        {
            Debug.LogError($"[StageManager] 생성된 스테이지 청크에 Chunk 스크립트가 없음");
            _isLoadingNextStage = false; 
            _isWaitingForPlayerToProceed = false; 
            return;
        }

        float currentNoiseScale = Random.Range(minNoiseScale, maxNoiseScale);
        float currentHeightMultiplier = Random.Range(minHeightMultiplier, maxHeightMultiplier);

        int derivedSeed = baseWorldSeed + (_currentStageNumber * seedMultiplier);
        
        chunkScript.Initialize(stageCoord, stageSize, stageBuildHeight, derivedSeed, 
                               currentNoiseScale, currentHeightMultiplier, stageSharedMaterial);
        
        _currentLoadedStageChunk = chunkScript;
        
        Debug.Log($"스테이지 {_currentStageNumber} ({stageCoord}) 로드 완료 타이머 시작");
        MovePlayerToStageCenter(stageWorldPosition); // 플레이어 이동

        _currentStageTimer = 0f; // 새 스테이지 시작 시 타이머 리셋
        _isLoadingNextStage = false; // 로딩 완료 플래그
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateStageNumberUI(_currentStageNumber);
            UIManager.Instance.UpdateStageClearUI(_currentStageNumber);
        }
        
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.StartSpawningForStage(_currentStageNumber);
        }
        else
        {
            Debug.LogWarning("[StageManager] EnemySpawner 인스턴스를 찾을 수 없음. 적스폰 불가느");
        }
    }

    private void MovePlayerToStageCenter(Vector3 stageBaseWorldPosition)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[StageManager] 플레이어 참조가 없어 위치를 이동시킬 수 없음");
            return;
        }

        CharacterController playerCharacterController = playerTransform.GetComponent<CharacterController>();
        bool wasControllerEnabled = false;
        if (playerCharacterController != null)
        {
            wasControllerEnabled = playerCharacterController.enabled;
            playerCharacterController.enabled = false;
        }

        int spawnLocalX = stageSize / 2;
        int spawnLocalZ = stageSize / 2;

        // _currentLoadedStageChunk가 null이 아닐 때만 표면 높이 계산
        int surfaceY = _currentLoadedStageChunk != null 
            ? _currentLoadedStageChunk.GetSurfaceHeightAt(spawnLocalX, spawnLocalZ) : 0;

        float spawnYPosition = surfaceY + 4.0f;
        float playerHeight = playerCharacterController != null ? playerCharacterController.height : 2.0f;
        
        if (this.stageBuildHeight > 0)
        {
            spawnYPosition = Mathf.Clamp(spawnYPosition, 1.0f, 
                stageBuildHeight - (playerCharacterController != null ? playerCharacterController.height : playerHeight));
        }
        else
        {
            spawnYPosition = Mathf.Max(1.0f, spawnYPosition);
        }


        Vector3 spawnPosition = new Vector3(
            stageBaseWorldPosition.x + spawnLocalX + 0.5f,
            spawnYPosition,
            stageBaseWorldPosition.z + spawnLocalZ + 0.5f
        );

        playerTransform.position = spawnPosition;
        playerTransform.rotation = Quaternion.LookRotation(Vector3.forward);

        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = wasControllerEnabled;
        }

        PlayerController pc = playerTransform.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ResetVelocity();
        }
        Debug.Log($"플레이어를 스테이지 중앙 (로컬: {spawnLocalX},{spawnLocalZ}), 지표면높이: {surfaceY}, 최종스폰높이: {spawnYPosition} (월드좌표: {spawnPosition}) 로 이동 완료");
    }

    /// <summary>
    /// 블록 파괴 업데이트 함수
    /// </summary>
    /// <param name="worldX"></param>
    /// <param name="worldY"></param>
    /// <param name="worldZ"></param>
    /// <returns></returns>
    public bool DestroyBlockAt(int worldX, int worldY, int worldZ)
    {
        if (_currentLoadedStageChunk == null) return false;
        
        Vector2Int targetStageCoord = CurrentStageCoord;

        int chunkMinX = targetStageCoord.x * stageSize;
        int chunkMaxX = chunkMinX + stageSize;
        int chunkMinZ = targetStageCoord.y * stageSize;
        int chunkMaxZ = chunkMinZ + stageSize;

        if (worldX >= chunkMinX && worldX < chunkMaxX && worldZ >= chunkMinZ && worldZ < chunkMaxZ)
        {
            int localX = worldX - chunkMinX;
            int localY = worldY;
            int localZ = worldZ - chunkMinZ;
            return _currentLoadedStageChunk.ChangeBlock(localX, localY, localZ, BlockType.Air);
        }
        return false;
    }

    public void RequestCurrentStageMeshUpdate()
    {
        if (_currentLoadedStageChunk != null)
        {
            _currentLoadedStageChunk.CreateChunkMesh();
        }
    }
    
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[StageManager] 이미 인스턴스가 존재합니다");
            Destroy(gameObject);
        }
    }

    private void InitializePlayerObj()
    {
        if (playerTransform != null) return;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("[StageManager] Player 오브젝트가 씬에 존재하지 않습니다.");
        }
    }
    
    // /// <summary>
    // /// 잠시 대기 후 다음 스테이지를 로드하는 코루틴
    // /// </summary>
    // private IEnumerator LoadNextStageCoroutine()
    // {
    //     yield return new WaitForSeconds(delayBeforeNextStage);
    //
    //     _currentStageNumber++;
    //
    //     Vector2Int nextStageCoord = new Vector2Int(CurrentStageCoord.x + _currentStageNumber -1, CurrentStageCoord.y); // (0,0) -> (1,0) -> (2,0) ...
    //
    //     Debug.Log($"다음 스테이지 ({_currentStageNumber}) 로딩 시작: 좌표 {nextStageCoord}");
    //     LoadStageAndStartTimer(nextStageCoord);
    // }
}