using System.Collections;
using UnityEngine;
using UnityEngine.AI; 
using System;
using Random = UnityEngine.Random;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    public event Action<int> OnStageStarted; // 스테이지가 시작될 때 (번호 전달)
    public event Action OnStageCleared; // 스테이지가 클리어
    public event Action OnGameOver; // 게임이 오버
    public event Action OnGameRestart; // 게임이 재시작

    [Header("Stage (Chunk) Settings")]
    [SerializeField] private GameObject stageChunkPrefab;
    [SerializeField] public int stageSize = 16;
    [SerializeField] private int stageBuildHeight = 64;
    [SerializeField] private int baseWorldSeed = 12345;

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;
    private PlayerData _playerData;
    
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
    }

    private void Start()
    {
        if (stageChunkPrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] StageChunkPrefab 할당 필요");
            enabled = false;
            return;
        }

        if (StageTheme == null || StageTheme.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] 스테이지 테마 배열 확인");
            enabled = false;
            return;
        }

        if (!InitializePlayerAndDependencies())
        {
            Debug.LogError($"[{gameObject.name}] 플레이어 및 의존성 초기화 불가");
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
                Debug.LogError($"[{gameObject.name}] Player 태그를 가진 오브젝트가 없습니다.");
                return false;
            }
        }

        _playerData = playerTransform.GetComponent<PlayerData>();
        if (_playerData == null)
        {
            Debug.LogError($"[{gameObject.name}] Player 오브젝트에서 PlayerData 컴포넌트를 찾을 수 없습니다.");
            return false;
        }
        return true;
    }

    private void Update()
    {
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
            float currentStageSurvivalTime = (_currentActiveTheme != null && _currentActiveTheme.timeToSurvivePerStage > 0)
                ? _currentActiveTheme.timeToSurvivePerStage : this.timeToSurvivePerStage;
            float timeLeft = currentStageSurvivalTime - _currentStageTimer;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateStageTimerUI(timeLeft);
            }
            
            // 일반 스테이지의 타임 만료, 모든적 섬멸 확인
            bool timerCompleted = (_currentStageTimer >= currentStageSurvivalTime);
            bool allEnemiesDefeated = (_spawningCompleted && EnemySpawner.ActiveEnemies.Count == 0);
            
            // 타임만료, 모든 적 섬멸 둘중하나 달성시 일반 스테이지 클리어
            if (timerCompleted || allEnemiesDefeated)
            {
                InitiateStageClearSequence();
            }
        }
    }

    
    private void StartNewGame()
    {
        _currentStageNumber = 1;
        _isLoadingNextStage = true;
        _isWaitingForPlayerToProceed = false;
        _isGameOver = false;
        Time.timeScale = 1f;

        if (_playerData != null)
        {
            _playerData.ReviveAndReset();
        }
        if (_loadStageCoroutine != null) StopCoroutine(_loadStageCoroutine);
        _loadStageCoroutine = StartCoroutine(LoadStageAndStartTimerCoroutine(_fixedStageCoordinate));
    }

    public void HandlePlayerDeath()
    {
        if (_isGameOver) return;
        _isGameOver = true;
        _isLoadingNextStage = false;
        _isWaitingForPlayerToProceed = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordResults(_currentStageNumber);
            GameManager.Instance.Leaderboard.AddScore(_currentStageNumber);
            GameManager.Instance.LoadResultsScene();
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] GameManager 인스턴스를 찾을 수 없어 결과 씬 로드 불가");
            Time.timeScale = 0f;
        }
        OnGameOver?.Invoke();
    }

    public void RestartGame()
    {
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
        
        OnGameRestart?.Invoke();
        StartNewGame();
    }

    private void InitiateStageClearSequence()
    {
        if (_isLoadingNextStage || _isWaitingForPlayerToProceed) return;

        _isLoadingNextStage = true;
        _isWaitingForPlayerToProceed = true;
        _currentStageTimer = 0f;
        
        OnStageCleared?.Invoke();
    }

    public void PlayerConfirmedShop()
    {
        if (!_isWaitingForPlayerToProceed)
        {
            Debug.LogWarning($"[{gameObject.name}] PlayerConfirmedShop: 플레이어 대기 상태가 아닙니다.");
            return;
        }

        if (_currentLoadedStageChunk != null)
        {
            Destroy(_currentLoadedStageChunk.gameObject);
            _currentLoadedStageChunk = null;

            if (NavMeshManager.Instance != null)
            {
                NavMeshManager.Instance.ClearAllNavMeshData();
            }
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideStageClearScreen();
            UIManager.Instance.ShowShopPanel();
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

        if (_loadStageCoroutine != null) StopCoroutine(_loadStageCoroutine);
        _loadStageCoroutine = StartCoroutine(LoadStageAndStartTimerCoroutine(_fixedStageCoordinate));
    }

    private IEnumerator LoadStageAndStartTimerCoroutine(Vector2Int stageCoord)
    {
        _isLoadingNextStage = true;
        _spawningCompleted = false;
        _isBossStage = (_currentStageNumber > 0 && _currentStageNumber % 5 == 0);
        
        if (_currentLoadedStageChunk != null)
        {
            Destroy(_currentLoadedStageChunk.gameObject);
            _currentLoadedStageChunk = null;
        }

        if (NavMeshManager.Instance != null)
        {
            NavMeshManager.Instance.ClearAllNavMeshData();
        }
        yield return null;

        if (StageTheme != null && StageTheme.Length > 0)
        {
            _currentActiveTheme = StageTheme[Mathf.Clamp(_currentStageNumber - 1, 0, StageTheme.Length - 1)];
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] 사용할 수 있는 StageTheme가 없음");
            _isLoadingNextStage = false;
            yield break;
        }

        if (_currentActiveTheme != null && _currentActiveTheme.timeToSurvivePerStage > 0)
        {
            this.timeToSurvivePerStage = _currentActiveTheme.timeToSurvivePerStage;
        }

        Vector3 stageWorldPosition = new Vector3(stageCoord.x * stageSize, 0f, stageCoord.y * stageSize);
        GameObject stageGO = Instantiate(stageChunkPrefab, stageWorldPosition, Quaternion.identity, this.transform);
        Chunk chunkScript = stageGO.GetComponent<Chunk>();

        if (chunkScript == null)
        {
            Debug.LogError($"[StageManager_Coroutine] 생성된 스테이지 청크에 Chunk 스크립트가 없습니다");
            _isLoadingNextStage = false;
            Destroy(stageGO);
            yield break;
        }

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
            Debug.LogWarning("[StageManager_Coroutine] _currentActiveTheme이 null 상태로 Chunk 초기화 진행");
        }

        int derivedSeed = baseWorldSeed + (_currentStageNumber * seedMultiplier);
        chunkScript.Initialize(stageCoord, stageSize, stageBuildHeight, derivedSeed,
                               currentNoiseScale, currentHeightMultiplier,
                               chunkMat, _currentActiveTheme);
        _currentLoadedStageChunk = chunkScript;

        if (NavMeshManager.Instance != null)
        {
            float navMeshWaitStartTime = Time.time;
            while (!NavMeshManager.Instance.IsSurfaceBaked && (Time.time - navMeshWaitStartTime < 10f))
            {
                yield return null;
            }
        }

        MovePlayerToStageCenter(stageWorldPosition);
        _currentStageTimer = 0f;
        _isLoadingNextStage = false;
        _isWaitingForPlayerToProceed = false;
        
        OnStageStarted?.Invoke(_currentStageNumber);
    }

    private void MovePlayerToStageCenter(Vector3 stageBaseWorldPosition)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 플레이어 참조가 없어 위치를 이동시킬 수 없음");
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
                Debug.LogWarning($"[{gameObject.name}] 스폰 위치 ({spawnLocalX}, {spawnLocalZ})의 표면 높이를 찾을 수 없음");
                surfaceY = 0;
            }
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
              
                int targetWorldX = Mathf.FloorToInt(worldImpactPosition.x + xOffset);
                int targetWorldZ = Mathf.FloorToInt(worldImpactPosition.z + zOffset);
                int localX = targetWorldX - Mathf.FloorToInt(chunkBaseWorldPosition.x);
                int localZ = targetWorldZ - Mathf.FloorToInt(chunkBaseWorldPosition.z);
                int startY = Mathf.FloorToInt(worldImpactPosition.y);


                for (int d = 0; d < depth; d++)
                {
                    int targetLocalY = startY - d;
                    if (localX >= 0 && localX < stageSize &&
                        localZ >= 0 && localZ < stageSize &&
                        targetLocalY >= 0 && targetLocalY < stageBuildHeight)
                    {
                        if (_currentLoadedStageChunk.ChangeBlock(localX, targetLocalY, localZ, BlockType.Air))
                        {
                            changedAnyBlock = true;
                        }
                    }
                }
            }
        }
    }
    public void NotifySpawningCompleted()
    {
        _spawningCompleted = true;
        Debug.Log($"[{gameObject.name}] 이 스테이지의 모든 스폰이 완료되었습니다");
    }

    public void RequestFireSupportStrike(Vector3 position, FireSupportSkillData skillData)
    {
        if (skillData.fireSupportProjectilePrefab == null)
        {
            Debug.LogError($"[{gameObject.name}]화력지원 프리팹 설정 필요");
            return;
        }
        StartCoroutine(FireSupportStrikeCoroutine(position, skillData));
    }
    private IEnumerator FireSupportStrikeCoroutine(Vector3 centerPosition, FireSupportSkillData skillData)
    {
        int waves = skillData.waves;
        int projectilesPerWave = skillData.projectilesPerWave;
        float waveDelay = 0.8f;
        float spawnRadius = skillData.spawnRadius;
        float aimDuration = skillData.aimDuration;
        float fallDuration = skillData.fallDuration;
        float spawnHeight = 50f;

        WaitForSeconds waveWait = new WaitForSeconds(waveDelay);

        for (int i = 0; i < waves; i++)
        {
            for (int j = 0; j < projectilesPerWave; j++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 targetPos = centerPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

                if (Physics.Raycast(targetPos + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f, LayerMask.GetMask("Ground")))
                {
                    targetPos = hit.point;
                }

                GameObject indicator = null;
                if (skillData.targetIndicatorPrefab != null)
                {
                    indicator = Instantiate(skillData.targetIndicatorPrefab, targetPos + new Vector3(0, 0.1f, 0), Quaternion.Euler(90, 0, 0));
                }

                Vector3 startPos = targetPos + Vector3.up * spawnHeight;

                yield return new WaitForSeconds(aimDuration);

                if (indicator != null) Destroy(indicator);

                GameObject projectileGO = Instantiate(skillData.fireSupportProjectilePrefab, startPos, Quaternion.identity);
                FireSupportProjectile projectileScript = projectileGO.GetComponent<FireSupportProjectile>();
                if (projectileScript != null)
                {
                    projectileScript.Initialize(startPos, targetPos, fallDuration);
                }
            }
            yield return waveWait;
        }
    }
}