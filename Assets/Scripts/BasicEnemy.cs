using UnityEngine;
using UnityEngine.AI; 
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))] 
[RequireComponent(typeof(Rigidbody))]    
[RequireComponent(typeof(Collider))]     
public class BasicEnemy : MonoBehaviour
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

    // EnemySpawner가 호출하여 PlayerData와 PlayerTransform을 주입합니다.
    public void Initialize(PlayerData playerData, Transform playerTransform)
    {
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
    void Awake()
    {
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
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                _navMeshAgent.Warp(hit.position);
                if (_isChasing && _playerTransform != null)
                {
                    _navMeshAgent.SetDestination(_playerTransform.position);
                }
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] ({transform.position}) 근처에 유효한 NavMesh를 찾을 수 없습니다.", this);
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

    public void TakeDamage(float damageAmount)
    {
        if (_currentHealth <= 0 && damageAmount > 0) return;
        
        _currentHealth -= damageAmount;

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
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