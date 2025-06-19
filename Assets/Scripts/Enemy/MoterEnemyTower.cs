using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MortarEnemyTower : EnemyBase
{
    [Header("박격포 설정")]
    public float detectionRadius = 25f;
    public float attackRange = 20f;
    public float minAttackRange = 5f;
    public float aimDuration = 2.0f;
    public float fireDelay = 0.5f;
    public float attackCooldown = 5.0f;
    public GameObject mortarProjectilePrefab;
    public Transform projectileSpawnPoint;
    public GameObject targetIndicatorPrefab;
    public float projectileFlightTime = 2.5f;

    private float _lastAttackTime;
    private GameObject _currentTargetIndicatorInstance;
    private Vector3 _predictedTargetPosition;

    protected override void Awake()
    {
        base.Awake();
        
        // 터렛형이므로 NavMeshAgent 비활성화
        if (NavMeshAgent != null)
        {
            NavMeshAgent.enabled = false;
        }
        IsAgentActive = false;

        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
            Debug.LogWarning($"[{gameObject.name}] projectileSpawnPoint가 설정되지 않았습니다.");
        }
        if (mortarProjectilePrefab == null) Debug.LogError($"[{gameObject.name}] MortarProjectilePrefab이 설정되지 않았습니다!");
        if (targetIndicatorPrefab == null) Debug.LogError($"[{gameObject.name}] TargetIndicatorPrefab이 설정되지 않았습니다!");
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _lastAttackTime = -attackCooldown;
        CurrentState = EnemyState.Idle;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        if (_currentTargetIndicatorInstance != null) Destroy(_currentTargetIndicatorInstance);        
    }

    protected override void Update()
    {
        base.Update();
        
        if (IsDead || PlayerTransform == null || PlayerData == null || PlayerData.isDead)
        {
            if (_currentTargetIndicatorInstance != null) Destroy(_currentTargetIndicatorInstance);
            CurrentState = EnemyState.Idle;
            return;
        }
        HandleStateMachine();
    }

    private void HandleStateMachine()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
        
        void RotateTowardsPlayer()
        {
            if (PlayerTransform == null) return;
            Vector3 directionToPlayer = (PlayerTransform.position - transform.position).normalized;
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
        }

        switch (CurrentState)
        {
            case EnemyState.Idle:
                if (distanceToPlayer <= detectionRadius)
                {
                    RotateTowardsPlayer();
                }
                if (distanceToPlayer <= attackRange && distanceToPlayer >= minAttackRange)
                {
                    if (Time.time >= _lastAttackTime + attackCooldown)
                    {
                        StartCoroutine(AimAndFireCoroutine());
                    }
                }
                break;
            case EnemyState.Cooldown:
                RotateTowardsPlayer();
                if (Time.time >= _lastAttackTime + attackCooldown && CurrentState == EnemyState.Cooldown)
                {
                     CurrentState = EnemyState.Idle;
                }
                break;
            case EnemyState.Attacking:
                break;
        }
    }

    private IEnumerator AimAndFireCoroutine()
    {
        CurrentState = EnemyState.Attacking;

        // 조준 및 낙하 지점 표시
        _predictedTargetPosition = PlayerTransform.position;

        // 바닥 확인
        RaycastHit hitInfo;
        Vector3 indicatorPosition;

        if (Physics.Raycast(_predictedTargetPosition + Vector3.up * 10f, Vector3.down, out hitInfo, 20f, LayerMask.GetMask("Ground")))
        {
            _predictedTargetPosition = hitInfo.point;
            indicatorPosition = _predictedTargetPosition + hitInfo.normal * 0.05f;
        }
        else
        {
            _predictedTargetPosition.y = 0;
            indicatorPosition = _predictedTargetPosition + Vector3.up * 0.05f;
             Debug.LogWarning($"[{gameObject.name}] 박격포 조준 시 바닥을 찾지 못했습니다.");
        }

        if (targetIndicatorPrefab != null)
        {
            if (_currentTargetIndicatorInstance != null) Destroy(_currentTargetIndicatorInstance);
            _currentTargetIndicatorInstance = Instantiate(targetIndicatorPrefab, indicatorPosition, Quaternion.Euler(90, 0, 0));
        }

        yield return new WaitForSeconds(aimDuration);

        // 발사 전 딜레이
        if (fireDelay > 0)
        {
            yield return new WaitForSeconds(fireDelay);
        }

        // 발사
        if (mortarProjectilePrefab != null && projectileSpawnPoint != null)
        {
            GameObject projectileGO = Instantiate(mortarProjectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
            MortarProjectile projectileScript = projectileGO.GetComponent<MortarProjectile>();
            if (projectileScript != null)
            {
                projectileScript.Initialize(_predictedTargetPosition, projectileFlightTime);
            }
        }

        _lastAttackTime = Time.time;

        // 표시기 제거
        if (_currentTargetIndicatorInstance != null)
        {
            Destroy(_currentTargetIndicatorInstance, projectileFlightTime);
            _currentTargetIndicatorInstance = null;
        }

        CurrentState = EnemyState.Cooldown;
    }

    protected override void Die()
    {
        if (IsDead) return;
        base.Die();
        if (_currentTargetIndicatorInstance != null) Destroy(_currentTargetIndicatorInstance);
        PerformUniqueDeathActions();
        DestroyEnemy(0.1f);
    }

    protected override void PerformUniqueDeathActions()
    {
        // TODO:파괴 이펙트/사운드 추가 필요
    }

    protected override IEnumerator EnsureAgentOnNavMeshCoroutine()
    {
        yield break;
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minAttackRange);

        if (projectileSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(projectileSpawnPoint.position, projectileSpawnPoint.position + projectileSpawnPoint.forward * 2f);
        }
    }
}