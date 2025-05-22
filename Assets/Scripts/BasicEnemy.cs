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
    private NavMeshAgent _navMeshAgent;    
    private Rigidbody _rb;                 
    private bool _isChasing;

    public float navMeshSampleRadius = 2.0f;

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
            detectionRadius = enemyBaseData.detectionRadius;
            moveSpeed = enemyBaseData.moveSpeed;
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
        EnemySpawner.ActiveEnemies.Remove(this);
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            _playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] 'Player' 태그를 가진 오브젝트를 찾을 수 없습니다.");
            enabled = false;
            return;
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
            PlayerData playerData = other.GetComponent<PlayerData>();
            if (playerData != null && enemyBaseData != null)
            {
                playerData.TakeDamage(enemyBaseData.selfDestructDamage);
            }
            DestroySelf();
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (enemyBaseData == null && _currentHealth <= 0 && damageAmount > 0) return;
        
        _currentHealth -= damageAmount;

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        PlayerData playerData = FindFirstObjectByType<PlayerData>();
        if (playerData != null && enemyBaseData != null)
        {
            playerData.GainResources(enemyBaseData.resourcesToGive);
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
        Destroy(gameObject, selfDestructDelay);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}