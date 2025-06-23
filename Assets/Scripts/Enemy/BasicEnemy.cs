using UnityEngine;
using UnityEngine.AI; 
using System.Collections;

[RequireComponent(typeof(Rigidbody))]    
[RequireComponent(typeof(Collider))]     
public class BasicEnemy : EnemyBase
{
    [Header("추적 설정")]
    [Tooltip("플레이어 감지 거리")]
    public float detectionRadius = 15f;

    [Tooltip("자폭대기 시간")]
    public float selfDestructDelay;
    
    public GameObject explosionEffectPrefab;
    
    private Rigidbody _rb;                 
    
    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody>();

        if (_rb != null)
        {
            _rb.isKinematic = true; // NavMeshAgent가 이동을 제어하므로 Kinematic으로 설정
            _rb.useGravity = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true; // 플레이어와 충돌 감지용
        }

        // enemyBaseData에서 BasicEnemy 고유 스탯(detectionRadius) 로드
        if (enemyBaseData != null)
        {
            this.detectionRadius = enemyBaseData.detectionRadius; // ScriptableObject 값이 우선
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] EnemyBaseStatsData가 없어 인스펙터의 detectionRadius ({this.detectionRadius})를 사용합니다.");
        }
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
            CurrentState = EnemyState.Idle; // 플레이어가 없거나 죽었으면 Idle
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            if (CurrentState != EnemyState.Chasing) CurrentState = EnemyState.Chasing;

            if (NavMeshAgent.isOnNavMesh && NavMeshAgent.enabled)
            {
                NavMeshAgent.SetDestination(PlayerTransform.position);
            }
        }
        else
        {
            if (CurrentState == EnemyState.Chasing) CurrentState = EnemyState.Idle;

            if (NavMeshAgent.isOnNavMesh && NavMeshAgent.enabled && NavMeshAgent.hasPath)
            {
                NavMeshAgent.ResetPath();
            }
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (IsDead) return;

        if (other.CompareTag("Player"))
        {
            if (PlayerData != null && enemyBaseData != null)
            {
                PlayerData.TakeDamage(enemyBaseData.selfDestructDamage);
            }
            if (!IsDead) // 중복 Die 호출 방지
            {
                IsDead = true; // 즉시 죽음 상태로 변경
                CurrentState = EnemyState.Dead;
                PerformUniqueDeathActions(); // 자폭 이펙트
                DestroyEnemy(selfDestructDelay); // 설정된 지연 시간 후 파괴
            }
        }
    }

    protected override void Die() // 일반적인 데미지로 죽었을 때
    {
        if (IsDead) return;
        base.Die(); // 부모의 Die 로직 (자원 제공 등)
        PerformUniqueDeathActions();
        DestroyEnemy(0.1f); // 기본 지연시간으로 파괴 (자폭 딜레이와 다름)
    }

    protected override void PerformUniqueDeathActions()
    {
        if (explosionEffectPrefab != null)
        {
            ObjectPoolManager.Instance.GetFromPool(explosionEffectPrefab.name, transform.position, Quaternion.identity);
        }
    }

    protected override float CurrentAttackGaugeRatio
    {
        get
        {
            return -1f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}