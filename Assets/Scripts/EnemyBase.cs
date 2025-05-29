using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour
{
    public EnemyBaseStatsData enemyBaseData;
    protected float CurrentHealth;
    protected Transform PlayerTransform;
    protected PlayerData PlayerData;
    protected NavMeshAgent NavMeshAgent;
    protected bool IsAgentActive = false; // NavMeshAgent 활성화 상태

    public virtual void Initialize(PlayerData playerData, Transform playerTransform)
    {
        PlayerData = playerData;
        PlayerTransform = playerTransform;
        // NavMeshAgent는 각 자식 클래스 Awake에서 가져옴
    }

    public abstract void TakeDamage(float damageAmount);
    protected abstract void Die();
    public abstract void DeactivateForStageTransition(); // 스테이지 전환 시 안전하게 비활성화하는 함수

    protected virtual void Awake() // NavMeshAgent는 자식 클래스에서 초기화 후 사용
    {
        NavMeshAgent = GetComponent<NavMeshAgent>();
    }

    protected virtual void OnEnable()
    {
        if (enemyBaseData != null) CurrentHealth = enemyBaseData.maxHealth;
        // EnemySpawner.Instance?.RegisterEnemy(this); // 스포너에 등록
    }

    protected virtual void OnDisable()
    {
        // EnemySpawner.Instance?.UnregisterEnemy(this); // 스포너에서 제거
        // NavMeshAgent 관련 정리 (안전하게)
        if (NavMeshAgent != null && NavMeshAgent.enabled && NavMeshAgent.isOnNavMesh)
        {
            NavMeshAgent.isStopped = true;
            NavMeshAgent.ResetPath();
        }
        IsAgentActive = false;
    }
}