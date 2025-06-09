using UnityEngine;
using System.Collections;
public class MidBoss : EnemyBase
{
    [Header("보스 패턴 설정")]
    public float detectionRadius = 40f; // 플레이어 감지 범위
    public float attackRange = 25f; // 공격 사거리
    public float stoppingDistance = 20f; // 플레이어와 유지할 거리
    public float attackCooldown = 4f; // 공격 쿨다운
    public GameObject projectilePrefab; // 발사할 투사체
    public Transform[] firePoints; // 여러 발사 지점
    public int projectilesPerVolley = 3; // 한 번에 발사할 투사체 수
    public float timeBetweenShots = 0.2f; // 연속 발사 시 간격

    private float _lastAttackTime;

    protected override void Awake()
    {
        base.Awake();
        if (enemyBaseData != null)
        {
            this.MaxHealth = enemyBaseData.maxHealth;
            this.CurrentHealth = MaxHealth;
            if (NavMeshAgent != null) NavMeshAgent.speed = enemyBaseData.moveSpeed;

            // 추가적인 보스 전용 스탯 로드
            this.projectilePrefab = enemyBaseData.projectilePrefab;
            
        }

        if (firePoints == null || firePoints.Length == 0)
        {
            // 발사 지점이 없으면 자기 자신을 사용
            firePoints = new Transform[] { transform };
        }
    }

    void Update()
    {
        if (IsDead || PlayerTransform == null || !IsAgentActive)
        {
            if (NavMeshAgent.enabled) NavMeshAgent.isStopped = true;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);

        // 항상 플레이어를 바라보도록 설정
        if (PlayerTransform != null)
        {
            Vector3 direction = (PlayerTransform.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
            }
        }

        // 상태 머신
        switch (CurrentState)
        {
            case EnemyState.Idle:
            case EnemyState.Chasing:
                if (distanceToPlayer <= detectionRadius)
                {
                    if (distanceToPlayer > stoppingDistance)
                    {
                        CurrentState = EnemyState.Chasing;
                        NavMeshAgent.SetDestination(PlayerTransform.position);
                        NavMeshAgent.isStopped = false;
                    }
                    else
                    {
                        CurrentState = EnemyState.Attacking;
                        NavMeshAgent.isStopped = true;
                    }
                }
                break;
            
            case EnemyState.Attacking:
                if (Time.time > _lastAttackTime + attackCooldown)
                {
                    StartCoroutine(AttackCoroutine());
                }
                // 공격 후 다시 거리 조절
                if (distanceToPlayer > attackRange)
                {
                    CurrentState = EnemyState.Chasing;
                }
                break;
            
            case EnemyState.Cooldown:
                // 공격 코루틴이 끝나면 Chasing 상태로 돌아가 거리를 다시 재도록 함
                 if (Time.time > _lastAttackTime + attackCooldown)
                {
                    CurrentState = EnemyState.Chasing;
                }
                break;
        }
    }
    
    private IEnumerator AttackCoroutine()
    {
        CurrentState = EnemyState.Cooldown; // 중복 공격 방지
        _lastAttackTime = Time.time;

        // TODO: 공격 전 예비 동작 애니메이션 실행
        // _animator.SetTrigger("PrepareAttack");
        // yield return new WaitForSeconds(1f);

        for (int i = 0; i < projectilesPerVolley; i++)
        {
            // 각 발사 지점을 순환하며 발사
            Transform firePoint = firePoints[i % firePoints.Length];

            if (projectilePrefab != null)
            {
                GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                // TODO: 투사체에 데미지, 속도 등 설정
                // proj.GetComponent<Projectile>().SetDamage(enemyBaseData.attackDamage);
            }
            yield return new WaitForSeconds(timeBetweenShots);
        }
        
        // 코루틴이 끝나면 Update에서 다시 Chasing 상태로 전환됨
    }
    
    // 이펙트, 사운드 등 보스 전용 죽음 연출
    protected override void PerformUniqueDeathActions()
    {
        Debug.Log("보스가 처치되었습니다! 특별한 폭발 효과를 보여줍니다.");
        // TODO: 거대한 폭발 이펙트 생성, 아이템 드랍 등
    }
}

