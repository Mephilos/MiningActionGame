using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class RangedEnemy : EnemyBase
{
    [Header("추적 및 공격 설정")]
    [Tooltip("플레이어 감지 거리")]
    public float detectionRadius = 20f;
    [Tooltip("공격 사거리 (이 거리 안으로 들어오면 공격 시작)")]
    public float attackRange = 5f;
    [Tooltip("공격 딜레이 (초)")]
    public float attackCooldown = 2f;
    [Tooltip("투사체 발사 위치")]
    public Transform firePoint;
	public float aimHeightOffset = 1.0f;
    [Tooltip("투사체 속도")]
    public float projectileSpeed = 15f;

    private Rigidbody _rb;
    private GameObject _projectilePrefab;
    private float _lastAttackTime;
    

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody>();
        
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true; // BasicEnemy와 달리 플레이어와 직접 충돌하여 자폭하지 않음

        if (enemyBaseData != null)
        {
            //RangedEnemy 고유 스탯 (EnemyBaseStatsData)
            this.detectionRadius = enemyBaseData.detectionRadius;
            this.attackRange = enemyBaseData.rangedAttackRange;
            this.attackCooldown = enemyBaseData.attackCooldown;
            this._projectilePrefab = enemyBaseData.projectilePrefab;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] EnemyBaseStatsData 가 없습니다");
        }

        if (NavMeshAgent != null)
        {
            NavMeshAgent.stoppingDistance = this.attackRange * 0.8f;
        }

        if (firePoint == null)
        {
            Debug.LogError($"[{gameObject.name}] FirePoint가 할당되지 않음");
        }

        if (_projectilePrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] projectilePrefab가 할당 되지 않음");
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable(); 
        _lastAttackTime = -attackCooldown; // 처음에는 바로 공격 가능하도록
    }

    protected override IEnumerator Start()
    {
        yield return base.Start();
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead || !IsAgentActive || PlayerTransform == null || PlayerData == null || PlayerData.isDead)
        {
            if (NavMeshAgent != null && NavMeshAgent.enabled && NavMeshAgent.hasPath)
            {
                NavMeshAgent.ResetPath();
            }
            if (!IsDead) CurrentState = EnemyState.Idle;
            return;
        }
        HandleStateMachine();
    }

    private void HandleStateMachine()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);

        if (CurrentState != EnemyState.Attacking && CurrentState != EnemyState.Dead)
        {
            LookAtPlayer();
        }

        switch (CurrentState)
        {
            case EnemyState.Idle:
                if (distanceToPlayer <= detectionRadius)
                {
                    CurrentState = EnemyState.Chasing;
                }
                break;
            case EnemyState.Chasing:
                if (NavMeshAgent.isOnNavMesh && NavMeshAgent.enabled)
                {
                    NavMeshAgent.SetDestination(PlayerTransform.position);
                }

                if (distanceToPlayer <= attackRange)
                {
                    if (!NavMeshAgent.pathPending && NavMeshAgent.remainingDistance <= NavMeshAgent.stoppingDistance)
                    {
                        if (NavMeshAgent.isOnNavMesh && NavMeshAgent.enabled)
                        {
                            NavMeshAgent.ResetPath();
                        }
                        CurrentState = EnemyState.Attacking;
                    }
                }
                else if (distanceToPlayer > detectionRadius)
                {
                    CurrentState = EnemyState.Idle;
                    if (NavMeshAgent.isOnNavMesh && NavMeshAgent.enabled)
                    {
                        NavMeshAgent.ResetPath();
                    }
                }
                break;
            case EnemyState.Attacking:
                LookAtPlayer();
                if (Time.time >= _lastAttackTime + attackCooldown)
                {
                    Attack();
                }
                else if (distanceToPlayer > attackRange * 1.0f)
                {
                    CurrentState = EnemyState.Chasing;
                }
                break;
            case EnemyState.Cooldown:
                if (Time.time >= _lastAttackTime + attackCooldown)
                {
                    CurrentState = EnemyState.Idle;
                }
                break;
        }
    }

    void LookAtPlayer()
    {
        if (PlayerTransform == null) return;
        Vector3 directionToPlayer = (PlayerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            float rotationSpeed = NavMeshAgent.angularSpeed > 0 ? NavMeshAgent.angularSpeed : 360f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void Attack()
    {
        if (_projectilePrefab == null || firePoint == null || PlayerTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] 투사체 프리팹, FirePoint 설정 필요");
            CurrentState = EnemyState.Idle;
            return;
        }
        _lastAttackTime = Time.time;
        CurrentState = EnemyState.Cooldown;
        
        Vector3 directionToPlayerWithOffset = (PlayerTransform.position + Vector3.up * aimHeightOffset - firePoint.position).normalized;
        if (directionToPlayerWithOffset == Vector3.zero) directionToPlayerWithOffset = firePoint.forward;
        firePoint.rotation = Quaternion.LookRotation(directionToPlayerWithOffset);
        
        string projectileTag = _projectilePrefab.name;
        GameObject projGO = ObjectPoolManager.Instance.GetFromPool(projectileTag, firePoint.position, firePoint.rotation);
        Projectile projectileScript = projGO.GetComponent<Projectile>();

        if (projectileScript != null)
        {
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
            projRb.linearVelocity = directionToPlayerWithOffset * projectileSpeed;
        }
        Debug.Log($"[{gameObject.name}] 공격. 다음 공격 시간: {_lastAttackTime + attackCooldown}");
    }

    protected override void Die()
    {
        if (IsDead) return;
        base.Die(); // 부모의 Die 로직 (자원 제공 등)
        PerformUniqueDeathActions();
        DestroyEnemy(0.1f);
    }
    protected override void PerformUniqueDeathActions()
    {
        // TODO: 죽음 연출용    
    }

    protected override float CurrentAttackGaugeRatio
    {
        get
        {
            if (attackCooldown <= 0) return 0f;
            float timeRemaining = (_lastAttackTime + attackCooldown) - Time.time;
            return (timeRemaining > 0) ? (timeRemaining / attackCooldown) : 0f;
        }
        
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (firePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(firePoint.position, firePoint.forward * attackRange);
        }
    }
}