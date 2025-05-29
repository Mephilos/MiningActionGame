using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class RangedEnemy : EnemyBase
{
    [Header("적 스탯 데이터")]
    public EnemyBaseStatsData enemyBaseData; // 공격 사거리, 발사체 데미지, 공격 딜레이 등 여기에 추가 필요

    private float _currentHealth;

    [Header("추적 및 공격 설정")]
    [Tooltip("플레이어 감지 거리")]
    public float detectionRadius = 20f;
    [Tooltip("공격 사거리 (이 거리 안으로 들어오면 공격 시작)")]
    public float attackRange = 15f;
    [Tooltip("공격 딜레이 (초)")]
    public float attackCooldown = 2f;
    [Tooltip("발사할 투사체 프리팹")]
    public GameObject projectilePrefab;
    [Tooltip("투사체 발사 위치")]
    public Transform firePoint;
	public float aimHeightOffset = 2.0f;
    [Tooltip("투사체 속도")]
    public float projectileSpeed = 15f;

    private Transform _playerTransform;
    private PlayerData _playerData;
    private NavMeshAgent _navMeshAgent;
    private Rigidbody _rb;
    private bool _isChasing;
    private float _lastAttackTime;

    public float navMeshSampleRadius = 2.0f;

    public override void Initialize(PlayerData playerData, Transform playerTransform)
    {
        base.Initialize(playerData, playerTransform);
        _playerData = playerData;
        _playerTransform = playerTransform;
        if (_playerData == null) Debug.LogError($"[{gameObject.name}] Initialize: PlayerData가 null입니다!");
        if (_playerTransform == null) Debug.LogError($"[{gameObject.name}] Initialize: PlayerTransform이 null입니다!");
    }

    protected override void Awake()
    {
        base.Awake();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        if (enemyBaseData == null)
        {
            Debug.LogError($"[{gameObject.name}] EnemyBaseStatsData가 할당되지 않음");
        }
        else
        {
            _currentHealth = enemyBaseData.maxHealth;
            this.detectionRadius = enemyBaseData.detectionRadius;
            _navMeshAgent.speed = enemyBaseData.moveSpeed;
            this.attackRange = enemyBaseData.rangedAttackRange; 
            this.attackCooldown = enemyBaseData.attackCooldown; 
        }

        if (_navMeshAgent != null) _navMeshAgent.updateRotation = true;
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true; // BasicEnemy와 달리 플레이어와 직접 충돌하여 자폭하지 않음
    }

    void OnEnable()
    {
        if (enemyBaseData != null) _currentHealth = enemyBaseData.maxHealth;
        else _currentHealth = 50f; // 기본값

        _lastAttackTime = -attackCooldown; // 처음에는 바로 공격 가능하도록

        if (EnemySpawner.ActiveEnemies == null) {
             Debug.LogError($"[RangedEnemy] {gameObject.name} OnEnable: EnemySpawner.ActiveEnemies 리스트가 null!");
             return;
        }
    }
    
    void Start()
    {
        if (_playerTransform == null || _playerData == null)
        {
            Debug.LogWarning($"[{gameObject.name}] PlayerData 또는 PlayerTransform이 초기화되지 않았습니다. EnemySpawner에서 Initialize 호출을 확인하세요.");
        }
        _navMeshAgent.stoppingDistance = attackRange * 0.8f; // 공격 사거리보다 약간 안쪽에서 멈추도록 설정
        StartCoroutine(EnsureAgentOnNavMesh());
    }

    IEnumerator EnsureAgentOnNavMesh()
    {
        for(var i = 0; i < 5 ; i++) yield return null; // 잠시 대기 후 NavMesh 확인
        EnsureAgentOnNavMeshLogic();
    }

    void EnsureAgentOnNavMeshLogic()
    {
        if (_navMeshAgent != null && !_navMeshAgent.isOnNavMesh && _navMeshAgent.enabled)
        {
            if (NavMeshManager.Instance != null &&
                NavMeshManager.Instance.FindValidPositionOnNavMesh(transform.position, navMeshSampleRadius, out Vector3 sampledPosition))
            {
                _navMeshAgent.Warp(sampledPosition);
                if (_isChasing && _playerTransform != null && _navMeshAgent.isOnNavMesh)
                {
                    _navMeshAgent.SetDestination(_playerTransform.position);
                }
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] ({transform.position}) 근처에 유효한 NavMesh를 찾을 수 없습니다.");
                DestroySelf();
            }
        }
    }

    void Update()
    {
        if (_playerTransform == null || !_navMeshAgent.enabled || _playerData.isDead)
        {
            if (_navMeshAgent != null && _navMeshAgent.hasPath) _navMeshAgent.ResetPath();
            _isChasing = false;
            return;
        }

        if (_isChasing && !_navMeshAgent.isOnNavMesh) EnsureAgentOnNavMeshLogic();

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            _isChasing = true;
            if (_navMeshAgent.isOnNavMesh) _navMeshAgent.SetDestination(_playerTransform.position);

            // 플레이어를 바라보도록 설정
            Vector3 directionToPlayer = (_playerTransform.position - transform.position).normalized;
            directionToPlayer.y = 0; // 수평 회전만
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * _navMeshAgent.angularSpeed / 30); // NavMeshAgent 회전 속도 활용
            }


            if (distanceToPlayer <= attackRange) // 공격 사거리 내에 있고
            {
                if (_navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance || !_navMeshAgent.hasPath) // 이동을 멈췄거나 목적지에 거의 도달했고
                {
                     if (Time.time >= _lastAttackTime + attackCooldown) // 공격 쿨타임이 지났으면
                     {
                        Attack();
                     }
                }
            }
        }
        else
        {
            if (_isChasing)
            {
                if (_navMeshAgent.hasPath) _navMeshAgent.ResetPath();
                _isChasing = false;
            }
        }
    }

    void Attack()
    {
        _lastAttackTime = Time.time;

        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogError($"[{gameObject.name}] 투사체 프리팹, FirePoint 설정 필요");
            return;
        }
        // 타켓 조준 위치 보정
		Vector3 targetPosition = _playerTransform.position + Vector3.up * aimHeightOffset;
        // 플레이어를 향해 투사체 발사
        GameObject projGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile projectileScript = projGO.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            // 적의 공격력을 투사체에 설정
            projectileScript.SetDamage(enemyBaseData != null ? enemyBaseData.attackDamage : 10f); // 기본값 10
            projectileScript.isEnemyProjectile = true;
        }
        else
        {
             Debug.LogWarning($"[{gameObject.name}] 발사된 투사체에 Projectile 스크립트가 없습니다.");
        }
        
        Rigidbody projRb = projGO.GetComponent<Rigidbody>();
        if (projRb != null)
        {
            Vector3 directionToPlayer = (targetPosition - firePoint.position).normalized;
            projGO.transform.rotation = Quaternion.LookRotation(directionToPlayer);
            projRb.linearVelocity = directionToPlayer * projectileSpeed;
        }

        // TODO: 발사 애니메이션, 사운드 등 추가
        Debug.Log($"[{gameObject.name}] 공격. 다음 공격 시간: {_lastAttackTime + attackCooldown}");
    }
    public override void TakeDamage(float damageAmount)
    {
        if (_currentHealth <= 0 && damageAmount > 0) return;
        _currentHealth -= damageAmount;
        if (_currentHealth <= 0) Die();
    }

    protected override void Die()
    {
        if (_playerData != null && enemyBaseData != null)
        {
            _playerData.GainResources(enemyBaseData.resourcesToGive);
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
        Destroy(gameObject, 0.1f); // 약간의 딜레이 후 파괴
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}