using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }
    
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public GameObject enemyPrefab;
        [Range(0f, 1f)] public float spawnWeight = 1f;
        public int minStageToSpawn = 1;
    }

    [Header("Enemy Settings")] 
    public List<EnemySpawnInfo> enemyTypes = new List<EnemySpawnInfo>();

    [Header("Boss Settings")]
    public GameObject bossPrefab;

    [Header("Spawn Area Settings")]
    public float minSpawnDistanceFromPlayer = 10f;
    public float maxSpawnDistanceFromPlayer = 25f;
    public int spawnPositionAttempts = 10;
    public float navMeshSampleRadius = 2.0f;

    private Transform _playerTransform;
    private PlayerData _playerData;
    public static List<EnemyBase> ActiveEnemies = new List<EnemyBase>();
    private bool _isSpawning;
    private int _currentStageNumberForSpawner;
    private Coroutine _spawnCoroutine;
    private bool _isBossStage;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        ActiveEnemies.Clear();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            _playerTransform = playerObject.transform;
            _playerData = playerObject.GetComponent<PlayerData>();
        }
    }
    void Start() 
    {
        if (StageManager.Instance != null)
        {
            // 이벤트 구독
            StageManager.Instance.OnStageStarted += HandleStageStart;
            StageManager.Instance.OnStageCleared += StopAndClearAllEnemies; 
            StageManager.Instance.OnGameOver += StopAndClearAllEnemies; 
            StageManager.Instance.OnGameRestart += StopAndClearAllEnemies; 
        }
    }
    void OnDestroy() // 오브젝트 파괴 시 반드시 구독 해제하여 메모리 누수 방지
    {
        if (StageManager.Instance != null)
        {
            // 이벤트 구독 해제
            StageManager.Instance.OnStageStarted -= HandleStageStart;
            StageManager.Instance.OnStageCleared -= StopAndClearAllEnemies;
            StageManager.Instance.OnGameOver -= StopAndClearAllEnemies;
            StageManager.Instance.OnGameRestart -= StopAndClearAllEnemies;
        }
    }
    private void HandleStageStart(int stageNumber)
    {
        StartSpawningForStage(stageNumber);
    }
    
    public void StartSpawningForStage(int stageNumber)
    {
        if (_playerTransform == null || _playerData == null)
        {
            Debug.LogError($"[{gameObject.name}] 플레이어가 설정되지 않아 스폰을 시작할 수 없음");
            return;
        }

        _currentStageNumberForSpawner = stageNumber;
        _isSpawning = true;

        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
        }

        // 전달받은 stageNumber를 기반으로 보스 스테이지인지 판단하여 플래그를 설정합니다.
        _isBossStage = (stageNumber > 0 && stageNumber % 5 == 0);

        if (_isBossStage)
        {
            if (bossPrefab != null)
            {
                _spawnCoroutine = StartCoroutine(SpawnBossCoroutine());
                Debug.Log($"[{gameObject.name}] 스테이지 {stageNumber}: 보스전 시작");
            }
            else
            {
                Debug.LogError("[EnemySpawner] 보스 프리팹이 할당되지 않았습니다");
            }
        }
        else
        {
            _spawnCoroutine = StartCoroutine(SpawnEnemiesOverTimeCoroutine());
            Debug.Log($"[{gameObject.name}] 스테이지 {_currentStageNumberForSpawner} 적 스폰 시작");
        }
    }

    public void StopAndClearAllEnemies()
    {
        _isSpawning = false;
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
        {
            if (ActiveEnemies[i] != null)
            {
                ActiveEnemies[i].DeactivateForStageTransition();
                Destroy(ActiveEnemies[i].gameObject);
            }
        }
        ActiveEnemies.Clear();
    }

    private IEnumerator SpawnBossCoroutine()
    {
        yield return new WaitForSeconds(2.0f);

        Vector3 spawnPosition = Vector3.zero;
        bool positionFound = FindValidSpawnPosition(out spawnPosition);

        if (positionFound)
        {
            GameObject bossGO = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
            if (bossGO.TryGetComponent<EnemyBase>(out var bossScript))
            {
                bossScript.Initialize(_playerData, _playerTransform);
                bossScript.SetStageScaling(_currentStageNumberForSpawner);
            }
            Debug.Log($"보스 [{bossPrefab.name}] 소환 완료!");
        }
        else
        {
            Debug.LogError("[EnemySpawner] 보스를 소환할 유효한 위치를 찾지 못했습니다.");
        }
        
        _isSpawning = false;
        _spawnCoroutine = null;
        
        if (StageManager.Instance != null)
        {
            StageManager.Instance.NotifySpawningCompleted();
        }
    }
    
    private IEnumerator SpawnEnemiesOverTimeCoroutine()
    {
        int totalEnemiesToSpawnForThisStage = 5 + (_currentStageNumberForSpawner * 6);
        int enemiesSpawnedSoFar = 0;
        float spawnDelayBetweenEnemies = Mathf.Max(0.5f, 3.0f - (_currentStageNumberForSpawner * 0.2f));

        while (_isSpawning && enemiesSpawnedSoFar < totalEnemiesToSpawnForThisStage)
        {
            if (_playerTransform == null || _playerData == null)
            {
                _isSpawning = false;
                yield break;
            }
            
            GameObject enemyPrefabToSpawn = GetRandomEnemyPrefabForCurrentStage();
            if (enemyPrefabToSpawn == null) continue;
            
            if (FindValidSpawnPosition(out Vector3 spawnPosition))
            {
                GameObject enemyGO = Instantiate(enemyPrefabToSpawn, spawnPosition, Quaternion.identity);
                if (enemyGO.TryGetComponent<EnemyBase>(out EnemyBase enemyScript))
                {
                    enemyScript.Initialize(_playerData, _playerTransform);
                    enemyScript.SetStageScaling(_currentStageNumberForSpawner);
                }
                enemiesSpawnedSoFar++;
            }

            if (_isSpawning)
            {
                yield return new WaitForSeconds(spawnDelayBetweenEnemies);
            }
        }
        
        _spawnCoroutine = null;
        if (StageManager.Instance != null)
        {
            StageManager.Instance.NotifySpawningCompleted();
        }
    }
    
    private bool FindValidSpawnPosition(out Vector3 result)
    {
        for (int attempt = 0; attempt < spawnPositionAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized *
                                   Random.Range(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);
            Vector3 potentialSpawnPoint = _playerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            if (NavMeshManager.Instance != null && 
                NavMeshManager.Instance.FindValidPositionOnNavMesh(potentialSpawnPoint, navMeshSampleRadius, out Vector3 sampledPosition) &&
                IsPositionWithinCurrentStageBounds(sampledPosition))
            {
                result = sampledPosition;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }
    
    GameObject GetRandomEnemyPrefabForCurrentStage()
    {
        List<EnemySpawnInfo> availableEnemies = new List<EnemySpawnInfo>();
        float totalWeight = 0f;

        foreach (var enemyInfo in enemyTypes)
        {
            if (_currentStageNumberForSpawner >= enemyInfo.minStageToSpawn)
            {
                availableEnemies.Add(enemyInfo);
                totalWeight += enemyInfo.spawnWeight;
            }
        }

        if (availableEnemies.Count == 0) return null;

        float randomPoint = Random.value * totalWeight;

        foreach (var enemyInfo in availableEnemies)
        {
            if (randomPoint < enemyInfo.spawnWeight)
            {
                return enemyInfo.enemyPrefab;
            }
            else
            {
                randomPoint -= enemyInfo.spawnWeight;
            }
        }
        return availableEnemies[availableEnemies.Count - 1].enemyPrefab;
    }

    private bool IsPositionWithinCurrentStageBounds(Vector3 position)
    {
        if (StageManager.Instance == null) return false;
        
        Vector2Int currentStageCoord = StageManager.Instance.CurrentStageCoord;
        float sSize = StageManager.Instance.stageSize;
        
        float stageWorldMinX = currentStageCoord.x * sSize;
        float stageWorldMaxX = currentStageCoord.x * sSize + sSize;
        float stageWorldMinZ = currentStageCoord.y * sSize;
        float stageWorldMaxZ = currentStageCoord.y * sSize + sSize;

        return position.x >= stageWorldMinX && position.x <= stageWorldMaxX &&
               position.z >= stageWorldMinZ && position.z <= stageWorldMaxZ;
    }
}