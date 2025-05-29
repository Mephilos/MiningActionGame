using UnityEngine;
using UnityEngine.AI; 
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))] 
[RequireComponent(typeof(Rigidbody))]    
[RequireComponent(typeof(Collider))]     
public class BasicEnemy : EnemyBase
{
    [Header("적 스탯 데이터")]
    public EnemyBaseStatsData enemyBaseData;

    private float _currentHealth;

    [Header("추적 설정")]
    [Tooltip("플레이어 감지 거리")]
    public float detectionRadius = 15f;
    [Tooltip("이동 속도")]
    public float moveSpeed = 3.5f;
    [Tooltip("자폭대기 시간")]
    public float selfDestructDelay; 

    private Transform _playerTransform;
    private PlayerData _playerData;
    private NavMeshAgent _navMeshAgent;    
    private Rigidbody _rb;                 
    private bool _isChasing;

    public float navMeshSampleRadius = 2.0f;

    // EnemySpawner가 호출하여 PlayerData와 PlayerTransform을 주입(DI디자인)
    public override void Initialize(PlayerData playerData, Transform playerTransform)
    {
        base.Initialize(playerData, playerTransform);
        _playerData = playerData;
        _playerTransform = playerTransform;

        if (_playerData == null)
        {
            Debug.LogError($"[{gameObject.name}] Initialize: PlayerData가 null입니다!");
        }
        if (_playerTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] Initialize: PlayerTransform이 null입니다!");
        }
    }
    protected override void Awake()
    {
        base.Awake();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        if (enemyBaseData == null)
        {
            Debug.LogError($"[{gameObject.name}] EnemyBaseStatsData가 할당되지 않았습니다!");
            _currentHealth = 50f;
        }
        else
        {
            _currentHealth = enemyBaseData.maxHealth;
            this.detectionRadius = enemyBaseData.detectionRadius;
            this.moveSpeed = enemyBaseData.moveSpeed;
        }
        
        if (_navMeshAgent != null)
        {
            _navMeshAgent.updateRotation = true;
        }
        
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Rigidbody 컴포넌트 없음", this);
        }

        if (_navMeshAgent == null)
        {
            Debug.LogError($"[{gameObject.name}] NavMeshAgent 컴포넌트 없음", this);
            enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Collider 컴포넌트가 없습니다");
        }
    }

    void OnEnable()
    {
        if (enemyBaseData != null) _currentHealth = enemyBaseData.maxHealth;
        else _currentHealth = 50f;
        
        if (EnemySpawner.ActiveEnemies == null) {
            Debug.LogError($"[BasicEnemy] {gameObject.name} OnEnable 시점: EnemySpawner.ActiveEnemies 리스트가 null입니다!");
            return;
        }

        if (!EnemySpawner.ActiveEnemies.Contains(this))
        {
            EnemySpawner.ActiveEnemies.Add(this);
        }
    }

    void OnDisable()
    {
        // ActiveEnemies는 static
        if (EnemySpawner.ActiveEnemies != null)
        {
            EnemySpawner.ActiveEnemies.Remove(this);
        }
    }

    void Start()
    {
        if (_playerTransform == null || _playerData == null)
        {
            Debug.LogWarning($"[{gameObject.name}] PlayerData 또는 PlayerTransform이 초기화되지 않았습니다 " +
                             $"EnemySpawner에서 Initialize 호출을 확인하세요");
        }
        if (enemyBaseData != null)
        {
            _navMeshAgent.speed = enemyBaseData.moveSpeed;
        }
        else
        {
            _navMeshAgent.speed = moveSpeed;
        }
        _navMeshAgent.stoppingDistance = 0.5f;

        StartCoroutine(EnsureAgentOnNavMesh());
    }

    IEnumerator EnsureAgentOnNavMesh()
    {
        for(var i = 0; i < 5 ; i++)
        {
            yield return null;
        }
        EnsureAgentOnNavMeshLogic();
    }
    void EnsureAgentOnNavMeshLogic()
    {
        if (_navMeshAgent != null && !_navMeshAgent.isOnNavMesh && _navMeshAgent.enabled)
        {
            Vector3 sampledPosition;
            // NavMeshManager를 사용하여 유효한 위치 검색
            if (NavMeshManager.Instance != null &&
                NavMeshManager.Instance.FindValidPositionOnNavMesh(transform.position, navMeshSampleRadius, out sampledPosition))
            {
                _navMeshAgent.Warp(sampledPosition);
                if (_isChasing && _playerTransform != null)
                {
                    // Warp 후 목적지 재설정 필요 시
                    if(_navMeshAgent.isOnNavMesh) _navMeshAgent.SetDestination(_playerTransform.position);
                }
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] ({transform.position}) 근처에 유효한 NavMesh를 찾을 수 없습니다. NavMeshManager 사용 결과.");
                DestroySelf();
            }
        }
    }

    void Update()
    {
        if (_playerTransform == null || !_navMeshAgent.enabled || !_navMeshAgent.isOnNavMesh)
        {
            if (_navMeshAgent != null && _navMeshAgent.hasPath)
            {
                _navMeshAgent.ResetPath();
            }
            _isChasing = false;
            return;
        }

        if (_isChasing && !_navMeshAgent.isOnNavMesh)
        {
            EnsureAgentOnNavMeshLogic();
        }

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            if (_navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.SetDestination(_playerTransform.position);
            }
            _isChasing = true;
        }
        else
        {
            if (_isChasing)
            {
                _navMeshAgent.ResetPath();
                _isChasing = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_playerData != null && enemyBaseData != null)
            {
                _playerData.TakeDamage(enemyBaseData.selfDestructDamage);
            }
            else if (_playerData != null)
            {
                Debug.LogWarning("[Basic Enemy] PlayerData 참조가 null 입니다");
            }
            DestroySelf();
        }
    }

    public override void TakeDamage(float damageAmount)
    {
        if (_currentHealth <= 0 && damageAmount > 0) return;
        
        _currentHealth -= damageAmount;

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    protected override void Die()
    {
        if (_playerData != null && enemyBaseData != null)
        {
            _playerData.GainResources(enemyBaseData.resourcesToGive);
        }
        else if (_playerData != null)
        {
            Debug.LogWarning($"[{gameObject.name}] PlayerData 참조가 null입니다");
        }

        DestroySelf();
    }

    public override void DeactivateForStageTransition()
    {
        Debug.Log($"[{gameObject.name}] Deactivating for stage transition.");
        IsAgentActive = false; // NavMesh 사용 중지 플래그
        if (_navMeshAgent != null && _navMeshAgent.enabled)
        {
            if (_navMeshAgent.isOnNavMesh) // NavMesh 위에 있을 때만 경로 리셋
            {
                _navMeshAgent.isStopped = true;
                _navMeshAgent.ResetPath();
            }
            _navMeshAgent.enabled = false; // NavMeshAgent 비활성화
        }
        // 필요하다면 다른 컴포넌트(Collider, Renderer 등)도 비활성화
        // 또는 이 스크립트 자체를 비활성화: this.enabled = false;
    }

    void DestroySelfWithoutNavMeshOperations() // NavMesh 작업 없이 오브젝트만 파괴
    {
        IsAgentActive = false;
        Destroy(gameObject, 0.1f);
    }
    void DestroySelf()
    {
        if (_navMeshAgent != null && _navMeshAgent.enabled)
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.ResetPath();
        }
        Destroy(gameObject, selfDestructDelay > 0 ? selfDestructDelay : 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}