// EnemySpawner.cs

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; } // 싱글톤 인스턴스

    [Header("Enemy Settings")]
    [Tooltip("스폰할 기본 적 프리팹")]
    public GameObject defaultEnemyPrefab; // 여러 종류의 적을 사용한다면 List로 변경 가능
    [Tooltip("한 번에 스폰할 수 있는 최대 적 숫자 (동시 스폰 수)")]
    public int maxEnemiesToSpawnAtOnce = 1; // 웨이브 방식에서는 한 웨이브당 스폰 수로 해석 가능
    

    [Header("Spawn Area Settings")]
    [Tooltip("플레이어와의 최소 스폰 거리")]
    public float minSpawnDistanceFromPlayer = 10f;
    [Tooltip("플레이어와의 최대 스폰 거리")]
    public float maxSpawnDistanceFromPlayer = 25f;
    [Tooltip("유효한 스폰 위치를 찾기 위한 시도 횟수")]
    public int spawnPositionAttempts = 10;
    [Tooltip("NavMesh.SamplePosition 사용 시 탐색 반경")]
    public float navMeshSampleRadius = 2.0f;

    private Transform _playerTransform;
    public static List<BasicEnemy> ActiveEnemies = new List<BasicEnemy>(); // 현재 활성화된 적 리스트 (BasicEnemy.cs에서 관리)
    private bool _isSpawning; // 현재 스폰 중인지 여부
    private int _currentStageNumberForSpawner; // 현재 스테이지 번호 (StageManager로부터 받음)
    private Coroutine _spawnCoroutine; // 현재 실행 중인 스폰 코루틴

    void Awake() // 싱글톤 패턴 구현
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            _playerTransform = playerObject.transform;
        }
        Debug.LogWarning($"[EnemySpawner] EnemySpawner.Start() 호출됨! ActiveEnemies 리스트를 초기화합니다. 호출 시간: {Time.time}"); 

        ActiveEnemies.Clear(); // 게임 시작 시 리스트 초기화
    }

    void Start()
    {
        // Debug.LogWarning($"[EnemySpawner] EnemySpawner.Start() 호출됨! ActiveEnemies 리스트를 초기화합니다. 호출 시간: {Time.time}"); 
        //
        // ActiveEnemies.Clear(); // 게임 시작 시 리스트 초기화
        
        if (defaultEnemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] Default Enemy Prefab이 할당되어 있지 않음");
            enabled = false;
        }
    }

    /// <summary>
    /// 스테이지 별 적 스폰을 시작
    /// StageManager에서 호출
    /// </summary>
    /// <param name="stageNumber">현재 시작하는 스테이지 번호</param>
    public void StartSpawningForStage(int stageNumber)
    {
        if (_playerTransform == null || defaultEnemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] 플레이어 또는 적 프리팹이 설정되지 않아 스폰을 시작할 수 없음");
            return;
        }
        _currentStageNumberForSpawner = stageNumber;
        _isSpawning = true;
        
        // 이전 스폰 코루틴이 있다면 중지 (안전 장치)
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
        }
        _spawnCoroutine = StartCoroutine(SpawnEnemiesOverTimeCoroutine());
        Debug.Log($"[EnemySpawner] 스테이지 {_currentStageNumberForSpawner} 적 스폰 시작");
    }

    /// <summary>
    /// 현재 진행 중인 모든 적 스폰을 중지하고, 활성화된 모든 적을 제거(또는 비활성화)
    /// StageManager에서 다음 스테이지로 넘어가기 전 또는 게임 오버 시 호출
    /// </summary>
    public void StopAndClearAllEnemies()
    {
        Debug.Log($"[EnemySpawner] 호출 시점 ActiveEnemies.Count: {ActiveEnemies.Count}");
        _isSpawning = false;
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        // activeEnemies 리스트를 복사하여 순회 (리스트 변경 중 순회 오류 방지)
        List<BasicEnemy> enemiesToDestroy = new List<BasicEnemy>(ActiveEnemies);
        Debug.Log($"[EnemySpawner] 파괴할 적 리스트 복사 완료. enemiesToDestroy.Count: {enemiesToDestroy.Count}");
        if (enemiesToDestroy.Count == 0 && ActiveEnemies.Count > 0)
        {
            Debug.LogWarning("[EnemySpawner] enemiesToDestroy는 비었지만 ActiveEnemies에는 적이 남아있습니다. (매우 드문 경우)");
        }
        foreach (BasicEnemy enemy in enemiesToDestroy)
        {
            if (enemy != null) // 이미 파괴된 경우 대비
            {
                // enemy.gameObject.SetActive(false); // 오브젝트 풀링을 사용한다면 비활성화
                Destroy(enemy.gameObject); // 즉시 파괴
            }
        }
        ActiveEnemies.Clear(); // 리스트 비우기 (BasicEnemy의 OnDisable에서도 제거하지만, 확실히 하기 위해)
        Debug.Log("[EnemySpawner] 모든 적 스폰 중지 및 활성 적 제거 완료");
    }

    /// <summary>
    /// 시간에 걸쳐 또는 웨이브 형태로 적 스폰
    /// </summary>
    private IEnumerator SpawnEnemiesOverTimeCoroutine()
    {
        // 예시: 스테이지 번호에 따라 총 스폰할 적의 수와 간격을 다르게 설정
        int totalEnemiesToSpawnForThisStage = 5 + (_currentStageNumberForSpawner * 2); // 스테이지가 높아질수록 더 많이 스폰
        int enemiesSpawnedSoFar = 0;
        float spawnDelayBetweenEnemies = Mathf.Max(0.5f, 3.0f - (_currentStageNumberForSpawner * 0.2f)); // 스테이지가 높아질수록 더 빨리 스폰

        Debug.Log($"[EnemySpawner] 스테이지 {_currentStageNumberForSpawner}: 총 {totalEnemiesToSpawnForThisStage}마리 스폰 예정, 스폰 간격: {spawnDelayBetweenEnemies}초");

        while (_isSpawning && enemiesSpawnedSoFar < totalEnemiesToSpawnForThisStage)
        {
            // 현재 활성화된 적의 수가 너무 많으면 잠시 대기
            // if (activeEnemies.Count >= 15) // 예: 동시에 최대 15마리까지만
            // {
            //     yield return new WaitForSeconds(1.0f);
            //     continue;
            // }

            for (int i = 0; i < maxEnemiesToSpawnAtOnce; i++) // 한 번에 여러 마리 스폰 시도
            {
                if (enemiesSpawnedSoFar >= totalEnemiesToSpawnForThisStage) break; // 목표치 도달 시 중단

                Vector3 spawnPosition = Vector3.zero;
                bool positionFound = false;

                for (int attempt = 0; attempt < spawnPositionAttempts; attempt++)
                {
                    if (_playerTransform == null)
                    {
                        Debug.LogError("[EnemySpawner] 플레레이어 참조 없음");
                        _isSpawning = false;
                        yield break; // 플레이어 없어지면 중단
                    }

                    Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);
                    Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
                    Vector3 potentialSpawnPoint = _playerTransform.position + randomDirection;

                    // 현재 스테이지(청크) 경계 내에서 스폰 위치를 찾도록 추가 검증이 필요할 수 있습니다.
                    // StageManager.Instance.CurrentStageCoord 와 StageManager.Instance.stageSize 를 사용
                    // 여기서는 일단 플레이어 주변 NavMesh 위로 단순화합니다.
                    // TODO: 스폰 위치가 현재 활성화된 스테이지 경계 내인지 확인하는 로직 추가

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(potentialSpawnPoint, out hit, navMeshSampleRadius, NavMesh.AllAreas))
                    {
                        if (IsPositionWithinCurrentStageBounds(hit.position))
                        {
                            spawnPosition = hit.position;
                            positionFound = true;
                            break;
                        }
                    }
                }

                if (positionFound)
                {
                    if (defaultEnemyPrefab != null)
                    {
                        Instantiate(defaultEnemyPrefab, spawnPosition, Quaternion.identity);
                        enemiesSpawnedSoFar++;
                    }
                    else
                    {
                        Debug.LogError("[EnemySpawner] 적 프리팹이 없습니다 스폰 불가");
                        _isSpawning = false;
                        yield break;
                    }
                }
                else
                {
                    // Debug.LogWarning("[EnemySpawner] 유효한 스폰 위치를 찾지 못했습니다. (시도 횟수 초과)");
                }
            }
            
            if (_isSpawning) // 스폰 중지 명령이 없었을 때만 대기
            {
                yield return new WaitForSeconds(spawnDelayBetweenEnemies);
            }
        }

        if (_isSpawning) // 모든 적을 스폰했으나, 스폰 중지 명령이 없었을 경우
        {
            Debug.Log($"[EnemySpawner] 스테이지 {_currentStageNumberForSpawner} 목표 적 스폰 완료 ({enemiesSpawnedSoFar}마리).");
            // 여기서 isSpawning = false; 를 할 수도 있고, StageManager가 명시적으로 Stop하기 전까지 계속 대기할 수도 있습니다.
            // 현재는 StageManager가 시간을 기준으로 클리어하므로, isSpawning은 계속 true로 둘 수 있습니다.
        }
        _spawnCoroutine = null; // 코루틴 종료 시 참조 해제
    }

    private bool IsPositionWithinCurrentStageBounds(Vector3 position)
    {
        if (StageManager.Instance == null)
        {
            Debug.LogError("[EnemySpawner] StageManager 인스턴스 찾을수 없음");
            return false;
        }
        Vector2Int currentStageCoord = StageManager.Instance.CurrentStageCoord;
        float sSize = StageManager.Instance.stageSize; // stageSize;
        
        float stageWorldMinX = currentStageCoord.x * sSize;
        float stageWorldMaxX = currentStageCoord.x * sSize + sSize;
        float stageWorldMinZ = currentStageCoord.y * sSize; // StageManager의 stageCoord.y는 월드 Z
        float stageWorldMaxZ = currentStageCoord.y * sSize + sSize;

        if (position.x >= stageWorldMinX && position.x <= stageWorldMaxX &&
            position.z >= stageWorldMinZ && position.z <= stageWorldMaxZ)
        {
            return true;
        }
        return false;
    }

    // 감지 반경 표시는 플레이어 중심이므로 그대로 사용 가능
    void OnDrawGizmosSelected()
    {
        if (_playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_playerTransform.position, maxSpawnDistanceFromPlayer);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_playerTransform.position, minSpawnDistanceFromPlayer);
        }
    }
}