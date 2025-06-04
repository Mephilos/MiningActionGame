using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour
{
    public EnemyBaseStatsData enemyBaseData;
    public float MaxHealth { get; protected set; }
    public float CurrentHealth { get; protected set; }
    public bool IsDead { get; protected set; }
    
    protected Transform PlayerTransform;
    protected PlayerData PlayerData;
    protected NavMeshAgent NavMeshAgent;
    protected bool IsAgentActive = true; // NavMeshAgent 활성화 상태

    public enum EnemyState
    {
        Idle,
        Attacking,
        Cooldown,
        Chasing,
        Dead
    }

    public EnemyState CurrentState { get; protected set; } = EnemyState.Idle;

    [Header("추적 셋팅")] [Tooltip("네비메쉬 사용시 탐색 반경")]
    public float navMeshSampleRadius = 2.0f;

    public virtual void Initialize(PlayerData playerData, Transform playerTransform)
    {
        this.PlayerData = playerData;
        this.PlayerTransform = playerTransform;
        // NavMeshAgent는 각 자식 클래스 Awake에서 가져옴
    }

    protected virtual void Awake()
    {
        NavMeshAgent = GetComponent<NavMeshAgent>();

        if (enemyBaseData != null)
        {
            MaxHealth = enemyBaseData.maxHealth;
            CurrentHealth = MaxHealth;
            if (NavMeshAgent != null)
            {
                NavMeshAgent.speed = enemyBaseData.moveSpeed;
            }
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] EnemyBaseData가 없습니다.");
        }
        IsDead = false;
    }

    protected virtual void OnEnable()
    {
        IsDead = false;
        CurrentState = EnemyState.Idle;
        if (enemyBaseData != null)
        {
            CurrentHealth = MaxHealth;
        }

        if (EnemySpawner.Instance != null && EnemySpawner.ActiveEnemies != null)
        {
            if (!EnemySpawner.ActiveEnemies.Contains(this))
            {
                EnemySpawner.ActiveEnemies.Add(this);
            }
        }
        IsAgentActive = true;
        if (NavMeshAgent != null && !NavMeshAgent.enabled)
        {
            NavMeshAgent.enabled = true;
        }
    }

    protected virtual void OnDisable()
    {
        if (EnemySpawner.Instance != null && EnemySpawner.ActiveEnemies != null)
        {
            EnemySpawner.ActiveEnemies.Remove(this);
        }
        if (NavMeshAgent != null && NavMeshAgent.enabled && NavMeshAgent.isOnNavMesh)
        {
            NavMeshAgent.isStopped = true;
            NavMeshAgent.ResetPath();
        }
        IsAgentActive = false;
    }

    protected virtual IEnumerator Start()
    {
        yield return null;
        for (int i = 0; i < 3; ++i)
        {
            yield return null;
        }
        if (IsAgentActive && NavMeshAgent != null && NavMeshAgent.enabled)
        {
            StartCoroutine(EnsureAgentOnNavMeshCoroutine());
        }
        else if (NavMeshAgent == null)
        {
            Debug.LogError($"[{gameObject.name}] 네비메쉬 에이전트가 없음");
        }
        else if (!NavMeshAgent.enabled)
        {
            Debug.LogWarning($"[{gameObject.name}] 네비메쉬가 활성화 되지 않음");
        }
    }
    
    public virtual void TakeDamage(float damageAmount)
    {
        if (IsDead || damageAmount <= 0) return;

        CurrentHealth -= damageAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (IsDead) return;

        IsDead = true;
        CurrentState = EnemyState.Dead;

        if (PlayerData != null && enemyBaseData != null)
        {
            PlayerData.GainResources(enemyBaseData.resourcesToGive);
        }
    }
    /// <summary>
    /// 적 케릭터 고유의 이펙트 사운드 관련
    /// </summary>
    protected abstract void PerformUniqueDeathActions();
    /// <summary>
    /// 스테이지 전환 등 적을 안전하게 비활성화(네비메쉬 등 처리)
    /// </summary>
    public virtual void DeactivateForStageTransition()
    {
        IsAgentActive = false;
        if (NavMeshAgent != null && NavMeshAgent.enabled)
        {
            if (NavMeshAgent.isOnNavMesh)
            {
                NavMeshAgent.isStopped = true;
                NavMeshAgent.ResetPath();
            }
        }
    }
    protected virtual void DestroyEnemy(float delay = 0.1f)
    {
        if (!IsDead)
        {
            IsDead = true;
            CurrentState = EnemyState.Dead;
        }
        DeactivateForStageTransition();
        Destroy(gameObject, delay);
    }

    protected virtual IEnumerator EnsureAgentOnNavMeshCoroutine()
    {
        int stabilityWaitFrames = 10;
        for (var i = 0; i < stabilityWaitFrames; i++)
        {
            if (NavMeshAgent != null && NavMeshAgent.isOnNavMesh)
            {
                yield break;
            }
            yield return null;
        }

        if (NavMeshAgent != null && NavMeshAgent.enabled && !NavMeshAgent.isOnNavMesh)
        {
            Vector3 currentPosition = transform.position;
            
            if (NavMeshManager.Instance != null)
            {
                if (NavMeshManager.Instance.FindValidPositionOnNavMesh(currentPosition, navMeshSampleRadius, out Vector3 sampledPosition))
                {
                    if (NavMeshAgent.Warp(sampledPosition))
                    {
                        NavMeshAgent.enabled = true; // Warp 후 간혹 비활성화 되는 경우 대비
                        NavMeshAgent.isStopped = false;
                    }
                    else
                    {
                        transform.position = sampledPosition; // 위치 강제 설정
                        NavMeshAgent.enabled = false;
                        yield return null; // 한 프레임 대기
                        NavMeshAgent.enabled = true;
                        yield return null; // 한 프레임 더 대기
                    }
                }
                else
                {
                    IsAgentActive = false;
                    DestroyEnemy();
                }
            }
            else
            {
                IsAgentActive = false;
                DestroyEnemy();
            }
        }
    }
}