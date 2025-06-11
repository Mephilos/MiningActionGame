using UnityEngine;
using System.Collections;
public class MidBoss : EnemyBase
{
    private enum BossState
    {
        Idle,
        Chasing,
        RangedAiming,
        RangedFiring,
        MeleeAttacking,
        Cooldown
    }
    private BossState _currentBossState;
    [Header("보스 분노 설정")]
    [Tooltip("체력이 낮아질 경우 최대 몇배까지 빨라질지 설정")]
    public float maxEnrageMultiplier = 1.5f;

    [Header("원거리 공격")]
    public float rangedAttackRange = 25f;
    public float rangedAttackCooldown = 5f;
    public float aimDuration = 1.5f; // 조준 지연시간
    public GameObject projectilePrefab;
    public Transform[] firePoints;
    public float projectileSpeed = 20f;
    public int projectilesPerVolley = 10;
    public float timeBetweenShots = 0.1f;

    [Header("근거리 공격")]
    public float meleeAttackRange = 5f;
    public float meleeAttackCooldown;
    public float shockwaveKnockbackForce;
    public float shockwaveKnockbackDuration;
    public GameObject shockwaveEffectPrefab;

    private float _lastRangedAttackTime;
    private float _lastMeleeAttackTime;
    private float _stateTimer;
    private PlayerController _playerController;

    protected override void Awake()
    {
        base.Awake();
        _currentBossState = BossState.Chasing;
        _lastRangedAttackTime = -rangedAttackCooldown;
        _lastMeleeAttackTime = -meleeAttackCooldown;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        
        if (HpBarInstance != null)
        {
            HpBarInstance.offset = new Vector3(0, 4.0f, 0); 
        }
    }
    public override void Initialize(PlayerData playerData, Transform playerTransform)
    {
        base.Initialize(playerData, playerTransform);
        _playerController = playerTransform.GetComponent<PlayerController>();
    }

    void Update()
    {
        if (IsDead || PlayerTransform == null || !IsAgentActive || _playerController == null)
        {
            if (NavMeshAgent.enabled) NavMeshAgent.isStopped = true;
            return;
        }

        float enrageMultiplier = CalculateEnrageMultiplier();
        NavMeshAgent.speed = enemyBaseData.moveSpeed * enrageMultiplier;

        switch (_currentBossState)
        {
            case BossState.Idle:
            case BossState.Chasing:
                HandleChasingState(enrageMultiplier);
                break;
            case BossState.RangedAiming:
                HandleRangedAimingState();
                break;
        }
    }

    private float CalculateEnrageMultiplier()
    {
        if (MaxHealth <= 0) return 1f;
        float healthPercentageRemaining = CurrentHealth / MaxHealth;
        return Mathf.Lerp(maxEnrageMultiplier, 1f, healthPercentageRemaining);
    }
    private void HandleChasingState(float enrageMultiplier)
    {
        LookAtPlayer();

        float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);

        if (distanceToPlayer <= meleeAttackRange && Time.time > _lastMeleeAttackTime + (meleeAttackCooldown / enrageMultiplier))
        {
            StartCoroutine(MeleeAttackCoroutine());
            return;
        }

        if (distanceToPlayer <= rangedAttackRange && Time.time > _lastRangedAttackTime + (rangedAttackCooldown / enrageMultiplier))
        {
            _currentBossState = BossState.RangedAiming;
            _stateTimer = 0f; // 조준 타이머 초기화
            NavMeshAgent.isStopped = true; // 조준 중에는 멈춤
            return;
        }

        NavMeshAgent.isStopped = false;
        NavMeshAgent.SetDestination(PlayerTransform.position);
    }
    // 원거리 공격 - 조준 상태 로직
    private void HandleRangedAimingState()
    {
        // 조준하는 동안 계속 플레이어 응시
        LookAtPlayer();

        _stateTimer += Time.deltaTime;
        if (_stateTimer >= aimDuration)
        {
            // 조준 시간이 끝나면 발사 코루틴 시작
            StartCoroutine(RangedFiringCoroutine());
        }
    }

    // 원거리 공격 - 발사 코루틴
    private IEnumerator RangedFiringCoroutine()
    {
        _currentBossState = BossState.RangedFiring;
        _lastRangedAttackTime = Time.time;

        for (int i = 0; i < projectilesPerVolley; i++)
        {
            // 복수의 firepoint 사용 가능
            Transform firePoint = firePoints[i % firePoints.Length];
            if (projectilePrefab != null)
            {
                Vector3 direction = (PlayerTransform.position + Vector3.up * 1.5f - firePoint.position).normalized;
                Quaternion rotation = Quaternion.LookRotation(direction);
                
                GameObject projectileGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

                if (projectileGO.TryGetComponent<Projectile>(out var projectileScript))
                {
                    projectileScript.SetDamage(enemyBaseData.attackDamage);
                    projectileScript.isEnemyProjectile = true;
                }
                
                if (projectileGO.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.linearVelocity = direction * projectileSpeed;
                }
            }
            yield return new WaitForSeconds(timeBetweenShots);
        }

        // 발사가 끝나면 쿨다운 상태를 거쳐 다시 추적 시작
        _currentBossState = BossState.Cooldown;
        yield return new WaitForSeconds(1.0f); // 공격 후 짧은 딜레이
        _currentBossState = BossState.Chasing;
    }
    private IEnumerator MeleeAttackCoroutine()
    {
        _currentBossState = BossState.MeleeAttacking;
        _lastMeleeAttackTime = Time.time;
        NavMeshAgent.isStopped = true;

        // TODO: 충격파 시전 애니메이션 추가
        yield return new WaitForSeconds(0.5f); // 선딜레이

        // 충격파 이펙트 생성
        if (shockwaveEffectPrefab != null)
        {
            Instantiate(shockwaveEffectPrefab, transform.position, Quaternion.identity);
        }

        // 범위 내 플레이어에게 넉백 적용
        if (Vector3.Distance(transform.position, PlayerTransform.position) <= meleeAttackRange + 1.0f) // 범위 보정
        {
            Vector3 knockbackDir = (PlayerTransform.position - transform.position).normalized;
            knockbackDir.y = 0.2f; // 살짝 위로 띄우는 효과
            _playerController.ApplyKnockback(knockbackDir.normalized, shockwaveKnockbackForce, shockwaveKnockbackDuration);
        }

        // 공격 후 쿨다운 상태를 거쳐 다시 추적 시작
        _currentBossState = BossState.Cooldown;
        yield return new WaitForSeconds(1.5f); // 공격 후 긴 딜레이
        _currentBossState = BossState.Chasing;
    }
    private void LookAtPlayer()
    {
        Vector3 direction = (PlayerTransform.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
        }
    }
    protected override void PerformUniqueDeathActions()
    {
        // TODO: 죽음 연출 추가
    }

    protected override void Die()
    {
        if (IsDead) return;
        base.Die();
        PerformUniqueDeathActions();
        DestroyEnemy(1.0f);
    }
}
