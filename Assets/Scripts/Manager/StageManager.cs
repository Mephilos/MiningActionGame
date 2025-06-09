using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage (Chunk) Settings")]
    [SerializeField] private GameObject stageChunkPrefab;
    [SerializeField] public int stageSize = 16;
    [SerializeField] private int stageBuildHeight = 64;
    [SerializeField] private int baseWorldSeed = 12345;

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;
    private PlayerData _playerData;

    [Header("System References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Stage Progression Settings")]
    [SerializeField] private float timeToSurvivePerStage = 60f; // 테마별로 오버라이드 가능

    [Header("Terrain Variation Settings (테마에 없을 경우 기본값)")]
    [Tooltip("스테이지별 노이즈 최소 스케일")]
    [SerializeField] private float defaultMinNoiseScale = 0.02f;
    [Tooltip("스테이지별 노이즈 최대 스케일")]
    [SerializeField] private float defaultMaxNoiseScale = 0.04f;
    [Tooltip("스테이지별 최소 지형 높이")]
    [SerializeField] private float defaultMinHeightMultiplier = 5f;
    [Tooltip("스테이지별 최대 지형 높이")]
    [SerializeField] private float defaultMaxHeightMultiplier = 10f;
    [Tooltip("스테이지 시드 값 계수")]
    [SerializeField] private int seedMultiplier = 100;

    [Header("스테이지 테마")]
    public StageThemeData[] StageTheme; // 다양한 스테이지 테마 목록
    private StageThemeData _currentActiveTheme; // 현재 활성화된 테마

    private Chunk _currentLoadedStageChunk;
    private float _currentStageTimer;
    private int _currentStageNumber = 1;
    private bool _isLoadingNextStage;
    private bool _isWaitingForPlayerToProceed;
    private bool _isGameOver;
    private Coroutine _loadStageCoroutine;

    private bool _isBossStage;
    private bool _spawningCompleted;

    private readonly Vector2Int _fixedStageCoordinate = Vector2Int.zero;
    public Vector2Int CurrentStageCoord => _fixedStageCoordinate;
    public int GetCurrentStageNumber() => _currentStageNumber;


    private void Awake()
    {
        Application.targetFrameRate = 60;
        InitializeSingleton();
        if (uiManager == null) uiManager = UIManager.Instance;
        if (enemySpawner == null) enemySpawner = EnemySpawner.Instance;
    }

    private void Start()
    {
        if (stageChunkPrefab == null)
        {
            Debug.LogError("[StageManager] StageChunkPrefab이 할당되지 않았습니다. StageManager를 비활성화합니다.");
            enabled = false;
            return;
        }

        if (StageTheme == null || StageTheme.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] 스테이지 테마 배열(StageTheme)이 비어있거나 할당되지 않았습니다! StageManager를 비활성화합니다.");
            enabled = false;
            return;
        }

        if (!InitializePlayerAndDependencies())
        {
            Debug.LogError("[StageManager] 플레이어 및 의존성 초기화 실패. StageManager를 비활성화합니다.");
            enabled = false;
            return;
        }
        StartNewGame();
    }

    private bool InitializePlayerAndDependencies()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError("[StageManager] Player 태그를 가진 오브젝트를 찾을 수 없습니다.");
                return false;
            }
        }

        _playerData = playerTransform.GetComponent<PlayerData>();
        if (_playerData == null)
        {
            Debug.LogError("[StageManager] Player 오브젝트에서 PlayerData 컴포넌트를 찾을 수 없습니다.");
            return false;
        }

        if (uiManager != null)
        {
            uiManager.Initialize(_playerData);
        }
        else
        {
            Debug.LogWarning("[StageManager] UIManager 참조가 없어 PlayerData를 UIManager에 주입할 수 없습니다.");
        }

        if (enemySpawner != null)
        {
            enemySpawner.Initialize(_playerData, playerTransform);
        }
        else
        {
            Debug.LogWarning("[StageManager] EnemySpawner 참조가 없어 PlayerData를 EnemySpawner에 주입할 수 없습니다.");
        }
        return true;
    }

    private void Update()
    {
        if (_isGameOver || _isLoadingNextStage || (uiManager != null && uiManager.shopPanel != null && uiManager.shopPanel.activeSelf))
        {
            if (_isGameOver && Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
            return;
        }
        if (_isWaitingForPlayerToProceed)
        {
            return;
        }

        if (_isBossStage)
        {
            if (_spawningCompleted && EnemySpawner.ActiveEnemies.Count == 0)
            {
                InitiateStageClearSequence();
            }
        }
        else
        {
            _currentStageTimer += Time.deltaTime;
            if (uiManager != null)
            {
                float currentStageSurvivalTime = (_currentActiveTheme != null && _currentActiveTheme.timeToSurvivePerStage > 0)
                        ? _currentActiveTheme.timeToSurvivePerStage
                        : this.timeToSurvivePerStage;
                float timeLeft = currentStageSurvivalTime - _currentStageTimer;
                uiManager.UpdateStageTimerUI(timeLeft > 0 ? timeLeft : 0f);
            }

            if (_currentStageTimer >= (_currentActiveTheme != null && _currentActiveTheme.timeToSurvivePerStage > 0
                    ? _currentActiveTheme.timeToSurvivePerStage : this.timeToSurvivePerStage))
            {
                InitiateStageClearSequence();
            }
        }
    }

    
    private void StartNewGame()
    {
        Debug.Log("[StageManager] 새 게임 시작 요청.");
        _currentStageNumber = 1;
        _isLoadingNextStage = true;
        _isWaitingForPlayerToProceed = false;
        _isGameOver = false;
        Time.timeScale = 1f;

        if (_playerData != null)
        {
            _playerData.ReviveAndReset();
        }
        else
        {
            Debug.LogError("[StageManager] StartNewGame: PlayerData가 null입니다. ReviveAndReset 호출 불가.");
        }

        if (uiManager != null)
        {
            uiManager.HideGameOverScreem();
            uiManager.UpdateStageNumberUI(_currentStageNumber);
            uiManager.HideShopPanel();
            if (_playerData != null) uiManager.UpdateResourceDisplayUI(_playerData.currentResources);
        }

        if (_loadStageCoroutine != null) StopCoroutine(_loadStageCoroutine);
        _loadStageCoroutine = StartCoroutine(LoadStageAndStartTimerCoroutine(_fixedStageCoordinate));
        Debug.Log($"[StageManager] 게임 시작 스테이지 {_currentStageNumber} 로딩 시작.");
    }

    public void HandlePlayerDeath()
    {
        if (_isGameOver) return;
        _isGameOver = true;
        _isLoadingNextStage = false;
        _isWaitingForPlayerToProceed = false;

        Debug.Log("[StageManager] 플레이어 사망 처리 시작.");

        if (enemySpawner != null)
        {
            enemySpawner.StopAndClearAllEnemies();
        }
        else
        {
            Debug.LogWarning("[StageManager] EnemySpawner 인스턴스를 찾을 수 없어 적 정리를 스킵합니다.");
        }

        Time.timeScale = 0f;
        Debug.Log("[StageManager] 게임 오버.");
        if (uiManager != null)
        {
            uiManager.ShowGameOverScreem();
        }
        else
        {
            Debug.LogWarning("[StageManager] UIManager 인스턴스를 찾을 수 없어 게임 오버 화면을 표시할 수 없습니다.");
        }
    }

    public void RestartGame()
    {
        Debug.Log("[StageManager] 게임 재시작 요청.");
        if (_loadStageCoroutine != null)
        {
            StopCoroutine(_loadStageCoroutine);
            _loadStageCoroutine = null;
        }
        if (_currentLoadedStageChunk != null)
        {
            Destroy(_currentLoadedStageChunk.gameObject);
            _currentLoadedStageChunk = null;
        }
        if (enemySpawner != null)
        {
            enemySpawner.StopAndClearAllEnemies();
        }
        StartNewGame();
    }

    private void InitiateStageClearSequence()
    {
        if (_isLoadingNextStage || _isWaitingForPlayerToProceed) return;

        _isLoadingNextStage = true;
        _isWaitingForPlayerToProceed = true;
        _currentStageTimer = 0f;
        Debug.Log($"[StageManager] 스테이지 {_currentStageNumber} 클리어!");

        if (enemySpawner != null)
        {
            enemySpawner.StopAndClearAllEnemies();
        }
        else
        {
            Debug.LogWarning("[StageManager] EnemySpawner 인스턴스를 찾을 수 없어 스테이지 클리어 시 적 정리를 스킵합니다.");
        }

        if (uiManager != null)
        {
            uiManager.ShowStageClearScreen();
        }
    }

    public void PlayerConfirmedShop()
    {
        if (!_isWaitingForPlayerToProceed)
        {
            Debug.LogWarning("[StageManager] PlayerConfirmedShop: 플레이어 확인 대기 상태가 아닙니다.");
            return;
        }

        if (_currentLoadedStageChunk != null)
        {
            Debug.Log($"[StageManager] 상점 진입 전 이전 청크 ({_currentLoadedStageChunk.name}) 파괴.");
            Destroy(_currentLoadedStageChunk.gameObject);
            _currentLoadedStageChunk = null;

            if (NavMeshManager.Instance != null)
            {
                NavMeshManager.Instance.ClearAllNavMeshData();
            }
        }

        if (uiManager != null)
        {
            uiManager.HideStageClearScreen();
            uiManager.ShowShopPanel();
        }
    }

    public void PlayerConfirmedNextStage()
    {
        if (!_isWaitingForPlayerToProceed)
        {
            Debug.LogWarning($"[{gameObject.name}] PlayerConfirmedNextStage 호출 문제");
            return;
        }

        _isWaitingForPlayerToProceed = false;
        _currentStageNumber++;
        _isLoadingNextStage = true; // 다음 스테이지 로딩 시작
        Debug.Log($"[StageManager] 다음 스테이지 {_currentStageNumber} 로딩 준비.");

        if (_loadStageCoroutine != null) StopCoroutine(_loadStageCoroutine);
        _loadStageCoroutine = StartCoroutine(LoadStageAndStartTimerCoroutine(_fixedStageCoordinate));
    }

    private IEnumerator LoadStageAndStartTimerCoroutine(Vector2Int stageCoord)
    {
        _isLoadingNextStage = true;
        _spawningCompleted = false;
        _isBossStage = (_currentStageNumber > 0 && _currentStageNumber % 5 == 0);
        Debug.Log($"[{gameObject.name}_Coroutine] LoadStageAndStartTimerCoroutine 시작 - 스테이지: {_currentStageNumber}");
        
        if (uiManager != null && uiManager.shopPanel != null && uiManager.shopPanel.activeSelf)
        {
            uiManager.HideShopPanel();
        }

        if (_currentLoadedStageChunk != null)
        {
            Destroy(_currentLoadedStageChunk.gameObject);
            _currentLoadedStageChunk = null;
            Debug.Log("[StageManager_Coroutine] 이전 청크 파괴 완료.");
        }

        if (NavMeshManager.Instance != null)
        {
            NavMeshManager.Instance.ClearAllNavMeshData();
            Debug.Log("[StageManager_Coroutine] NavMesh 데이터 클리어 요청 완료.");
        }
        yield return null;

        if (StageTheme != null && StageTheme.Length > 0)
        {
            _currentActiveTheme = StageTheme[Mathf.Clamp(_currentStageNumber - 1, 0, StageTheme.Length - 1)];
            Debug.Log($"[StageManager_Coroutine] 현재 활성 테마: {_currentActiveTheme.themeName}");
        }
        else
        {
            Debug.LogError("[StageManager_Coroutine] 사용할 수 있는 StageTheme이 없습니다!");
            _isLoadingNextStage = false;
            yield break;
        }

        if (_currentActiveTheme != null && _currentActiveTheme.timeToSurvivePerStage > 0)
        {
            this.timeToSurvivePerStage = _currentActiveTheme.timeToSurvivePerStage;
        }

        Debug.Log("[StageManager_Coroutine] 새 청크 인스턴스화 시작.");
        Vector3 stageWorldPosition = new Vector3(stageCoord.x * stageSize, 0f, stageCoord.y * stageSize);
        GameObject stageGO = Instantiate(stageChunkPrefab, stageWorldPosition, Quaternion.identity, this.transform);
        Chunk chunkScript = stageGO.GetComponent<Chunk>();

        if (chunkScript == null)
        {
            Debug.LogError($"[StageManager_Coroutine] 생성된 스테이지 청크에 Chunk 스크립트가 없습니다! 로딩 중단.");
            _isLoadingNextStage = false;
            Destroy(stageGO);
            yield break;
        }
        Debug.Log($"[StageManager_Coroutine] 청크 오브젝트 ({stageGO.name}) 생성 완료. 초기화 시작.");

        float currentNoiseScale, currentHeightMultiplier;
        Material chunkMat = null;

        if (_currentActiveTheme != null)
        {
            chunkMat = _currentActiveTheme.chunkMaterial;
            if (_currentActiveTheme.overrideTerrainParameters)
            {
                currentNoiseScale = Random.Range(_currentActiveTheme.minNoiseScale, _currentActiveTheme.maxNoiseScale);
                currentHeightMultiplier = Random.Range(_currentActiveTheme.minHeightMultiplier, _currentActiveTheme.maxHeightMultiplier);
            }
            else
            {
                currentNoiseScale = Random.Range(this.defaultMinNoiseScale, this.defaultMaxNoiseScale);
                currentHeightMultiplier = Random.Range(this.defaultMinHeightMultiplier, this.defaultMaxHeightMultiplier);
            }
        }
        else
        {
            currentNoiseScale = Random.Range(this.defaultMinNoiseScale, this.defaultMaxNoiseScale);
            currentHeightMultiplier = Random.Range(this.defaultMinHeightMultiplier, this.defaultMaxHeightMultiplier);
            Debug.LogWarning("[StageManager_Coroutine] _currentActiveTheme이 null 상태로 Chunk 초기화를 진행합니다.");
        }

        int derivedSeed = baseWorldSeed + (_currentStageNumber * seedMultiplier);
        chunkScript.Initialize(stageCoord, stageSize, stageBuildHeight, derivedSeed,
                               currentNoiseScale, currentHeightMultiplier,
                               chunkMat, _currentActiveTheme);
        _currentLoadedStageChunk = chunkScript;
        Debug.Log($"[StageManager_Coroutine] 청크 초기화 완료: {_currentLoadedStageChunk.name}.");

        if (NavMeshManager.Instance != null)
        {
            Debug.Log("[StageManager_Coroutine] NavMesh 빌드 대기 시작...");
            float navMeshWaitStartTime = Time.time;
            while (!NavMeshManager.Instance.IsSurfaceBaked && (Time.time - navMeshWaitStartTime < 10f))
            {
                yield return null;
            }
            if (NavMeshManager.Instance.IsSurfaceBaked)
            {
                Debug.Log("[StageManager_Coroutine] NavMesh 빌드 완료 확인!");
            }
            else
            {
                Debug.LogError("[StageManager_Coroutine] NavMesh 빌드 시간 초과 또는 실패!");
            }
        }
        else
        {
            Debug.LogError("[StageManager_Coroutine] NavMeshManager 인스턴스가 없습니다!");
        }

        Debug.Log($"[StageManager_Coroutine] 스테이지 {_currentStageNumber} 로드 완료 처리 중.");
        MovePlayerToStageCenter(stageWorldPosition);
        _currentStageTimer = 0f;

        if (uiManager != null)
        {
            uiManager.UpdateStageNumberUI(_currentStageNumber);
            uiManager.UpdateStageClearUI(_currentStageNumber);
        }

        if (enemySpawner != null && NavMeshManager.Instance != null && NavMeshManager.Instance.IsSurfaceBaked)
        {
            Debug.Log("[StageManager_Coroutine] 적 스폰 시작.");
            enemySpawner.StartSpawningForStage(_currentStageNumber);
        }
        else
        {
            if (enemySpawner == null) Debug.LogWarning("[StageManager_Coroutine] EnemySpawner가 할당되지 않아 적 스폰을 스킵합니다.");
            else Debug.LogWarning("[StageManager_Coroutine] NavMesh가 준비되지 않아 적 스폰을 스킵합니다.");
        }

        _isLoadingNextStage = false;
        _isWaitingForPlayerToProceed = false;
        Debug.Log($"[StageManager_Coroutine] LoadStageAndStartTimerCoroutine 정상 종료 - 스테이지: {_currentStageNumber}");
    }

    private void MovePlayerToStageCenter(Vector3 stageBaseWorldPosition)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[StageManager] 플레이어 참조가 없어 위치를 이동시킬 수 없습니다.");
            return;
        }

        CharacterController playerCharacterController = playerTransform.GetComponent<CharacterController>();
        bool wasControllerEnabled = false;
        if (playerCharacterController != null)
        {
            wasControllerEnabled = playerCharacterController.enabled;
            if (wasControllerEnabled) playerCharacterController.enabled = false;
        }

        int spawnLocalX = stageSize / 2;
        int spawnLocalZ = stageSize / 2;
        int surfaceY = 0;

        if (_currentLoadedStageChunk != null)
        {
            surfaceY = _currentLoadedStageChunk.GetSurfaceHeightAt(spawnLocalX, spawnLocalZ);
            if (surfaceY == -1)
            {
                Debug.LogWarning($"[StageManager] 스폰 위치 ({spawnLocalX}, {spawnLocalZ})의 표면 높이를 찾을 수 없습니다. 기본 높이 0 사용.");
                surfaceY = 0;
            }
        }
        else
        {
            Debug.LogError("[StageManager] MovePlayerToStageCenter: _currentLoadedStageChunk가 null입니다.");
        }

        float playerHeightOffset = 2.0f;
        float spawnYPosition = surfaceY + playerHeightOffset;
        float playerControllerHeight = playerCharacterController != null ? playerCharacterController.height : 2.0f;

        if (this.stageBuildHeight > 0)
        {
            spawnYPosition = Mathf.Clamp(spawnYPosition, 1.0f, stageBuildHeight - playerControllerHeight * 0.5f);
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

        if (playerCharacterController != null && wasControllerEnabled) // 변수명 오타 수정
        {
            playerCharacterController.enabled = true;
        }

        PlayerController pc = playerTransform.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ResetVelocity();
        }
        Debug.Log($"플레이어를 스테이지 중앙 (로컬: {spawnLocalX},{spawnLocalZ}), 지표면높이: {surfaceY}, 최종스폰높이: {spawnYPosition} (월드좌표: {spawnPosition}) 로 이동 완료");
    }

    public bool DestroyBlockAt(int worldX, int worldY, int worldZ)
    {
        if (_currentLoadedStageChunk == null) return false;

        Vector2Int targetStageCoord = CurrentStageCoord;
        int chunkMinX = targetStageCoord.x * stageSize;
        int chunkMaxX = chunkMinX + stageSize -1; 
        int chunkMinZ = targetStageCoord.y * stageSize;
        int chunkMaxZ = chunkMinZ + stageSize -1; 

        if (worldX >= chunkMinX && worldX < (chunkMinX + stageSize) && 
            worldZ >= chunkMinZ && worldZ < (chunkMinZ + stageSize))
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
            Debug.LogWarning("[StageManager] 이미 인스턴스가 존재합니다. 새로 생성된 인스턴스를 파괴합니다.");
            Destroy(gameObject);
        }
    }
    /// <summary>
    /// 지정된 월드 좌표를 중심으로 일정 영역의 블록을 파괴하고 메시를 업데이트합니다.
    /// </summary>
    /// <param name="worldImpactPosition">충격 지점의 월드 좌표</param>
    /// <param name="areaRadius">중심으로부터 파괴할 반경 (0이면 1x1, 1이면 3x3, 2면 5x5)</param>
    /// <param name="depth">파괴할 깊이 (Y축으로 몇 칸 아래로)</param>
    public void DestroyBlocksInArea(Vector3 worldImpactPosition, int areaRadius, int depth)
    {
        if (_currentLoadedStageChunk == null)
        {
            Debug.LogWarning("[StageManager] DestroyBlocksInArea: 현재 로드된 청크가 없습니다.");
            return;
        }

        bool changedAnyBlock = false;

        // 월드 좌표를 청크 로컬 좌표로 변환 준비
        Vector3 chunkBaseWorldPosition = _currentLoadedStageChunk.transform.position;

        for (int xOffset = -areaRadius; xOffset <= areaRadius; xOffset++)
        {
            for (int zOffset = -areaRadius; zOffset <= areaRadius; zOffset++)
            {
                // 충격 지점을 기준으로 각 오프셋에 해당하는 블록의 월드 X, Z 좌표 계산
                int targetWorldX = Mathf.FloorToInt(worldImpactPosition.x + xOffset);
                int targetWorldZ = Mathf.FloorToInt(worldImpactPosition.z + zOffset);

                // 청크 로컬 좌표로 변환
                int localX = targetWorldX - Mathf.FloorToInt(chunkBaseWorldPosition.x);
                int localZ = targetWorldZ - Mathf.FloorToInt(chunkBaseWorldPosition.z);

                // 충격 지점의 Y좌표를 기준으로 파괴 시작 (또는 표면부터 파괴)
                // 여기서는 충격받은 블록부터 아래로 파괴한다고 가정
                int startY = Mathf.FloorToInt(worldImpactPosition.y);
                
                // 또는 해당 (localX, localZ)의 표면 높이를 가져와서 거기서부터 팔 수도 있음
                // int surfaceY = _currentLoadedStageChunk.GetSurfaceHeightAt(localX, localZ);
                // if (surfaceY == -1 && startY < 0) continue; // 파괴할 표면이 없는 경우 (허공)
                // if (surfaceY != -1) startY = surfaceY;


                for (int d = 0; d < depth; d++)
                {
                    int targetLocalY = startY - d;

                    // 좌표 유효성 검사 (청크 내부인지, 높이가 유효한지)
                    if (localX >= 0 && localX < stageSize &&
                        localZ >= 0 && localZ < stageSize &&
                        targetLocalY >= 0 && targetLocalY < stageBuildHeight) // stageBuildHeight는 Chunk의 최대 높이
                    {
                        // ChangeBlock은 로컬 좌표를 사용
                        if (_currentLoadedStageChunk.ChangeBlock(localX, targetLocalY, localZ, BlockType.Air))
                        {
                            changedAnyBlock = true;
                        }
                    }
                }
            }
        }

        // 모든 변경이 끝난 후 메시 업데이트 (Chunk.ChangeBlock 내부에서 이미 호출될 수 있음)
        // 만약 ChangeBlock이 메시 업데이트를 즉시 하지 않는다면 여기서 한번만 호출
        if (changedAnyBlock && _currentLoadedStageChunk != null)
        {
             // _currentLoadedStageChunk.CreateChunkMesh(); // ChangeBlock 내부에서 이미 호출되므로 중복 호출 피해야 함
             Debug.Log($"[StageManager] {worldImpactPosition} 주변 지형 파괴 완료 및 메시 업데이트 요청됨.");
        }
    }
    public void NotifySpawningCompleted()
    {
        _spawningCompleted = true;
        Debug.Log("[StageManager] 이 스테이지의 모든 스폰이 완료되었습니다.");
    }
}