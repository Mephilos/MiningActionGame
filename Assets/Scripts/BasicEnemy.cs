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

    private float currentHealth;

    [Header("추적 설정")]
    [Tooltip("플레이어 감지 거리")]
    public float detectionRadius = 15f;
    [Tooltip("이동 속도")]
    public float moveSpeed = 3.5f;
    [Tooltip("자폭대기 시간")]
    public float selfDestructDelay = 0f; 

    private Transform playerTransform;    
    private NavMeshAgent navMeshAgent;    
    private Rigidbody rb;                 
    private bool isChasing = false;

    public float navMeshSampleRadius = 2.0f;


    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (enemyBaseData == null)
        {
            Debug.LogError($"[{gameObject.name}] EnemyBaseStatsData가 할당되지 않았습니다! 기본값을 사용하거나 스크립트가 비활성화될 수 있습니다.");
            // 기본값으로 초기화하거나, 여기서도 enabled = false; 처리 가능
            currentHealth = 50f; // 예비용 초기화
        }
        else
        {
            // ScriptableObject로부터 스탯 초기화
            currentHealth = enemyBaseData.maxHealth;
            detectionRadius = enemyBaseData.detectionRadius;
            moveSpeed = enemyBaseData.moveSpeed;
        }
        if (navMeshAgent != null)
        {
            navMeshAgent.updateRotation = true; // 이동 방향으로 자동 회전 활성화
        }
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Rigidbody 컴포넌트 없음", this);
        }

        if (navMeshAgent == null)
        {
            Debug.LogError($"[{gameObject.name}] NavMeshAgent 컴포넌트 없음", this);
            enabled = false; // NavMeshAgent 없을경우 스크립트 종료
        }
    }
    void OnEnable()
    {
        if (!EnemySpawner.activeEnemies.Contains(this))
        {
            EnemySpawner.activeEnemies.Add(this);
        }

        if (enemyBaseData != null) currentHealth = enemyBaseData.maxHealth;
        else currentHealth = 50f;
    }

    void OnDisable()
    {
        if(EnemySpawner.activeEnemies.Contains(this))
        {
            EnemySpawner.activeEnemies.Remove(this);
        }
    }
    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] 'Player' 태그를 가진 오브젝트를 찾을 수 없습니다. 추적할 대상이 없습니다.", this);
            enabled = false; // 플레이어가 없으면 스크립트 종료
            return;
        }

        // NavMeshAgent 기본 설정
        if (enemyBaseData != null)
        {
            navMeshAgent.speed = enemyBaseData.moveSpeed; // SO의 moveSpeed 사용
        }
        else
        {
            navMeshAgent.speed = this.moveSpeed; // SO가 없으면 인스펙터 값 사용
        }
        navMeshAgent.stoppingDistance = 0.5f; // 플레이어에게 매우 가까이 다가가도록 설정

        // NavMeshAgent가 NavMesh 위에 확실히 위치 (동적 생성 환경에서 중요)
        StartCoroutine(EnsureAgentOnNavMesh());
    }

    IEnumerator EnsureAgentOnNavMesh()
    {
        //navMesh bake 시간 대기 (5프레임)
        for(int i = 0; i < 5 ; i++)
        {
            yield return null;
        }

        EnsureAgentOnNavMeshLogic();
    }
    void EnsureAgentOnNavMeshLogic()
    {
        if (navMeshAgent != null && !navMeshAgent.isOnNavMesh && navMeshAgent.enabled)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                Debug.LogWarning($"[{gameObject.name}] NavMesh에서 벗어남. {transform.position} -> {hit.position} 위치로 Warp 시도.");
                navMeshAgent.Warp(hit.position);
                if (isChasing && playerTransform != null) // Warp 후 다시 목적지 설정
                {
                    navMeshAgent.SetDestination(playerTransform.position);
                }
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] ({transform.position}) 근처에 유효한 NavMesh를 찾을 수 없어 Warp 실패. 스폰 위치나 NavMesh Bake 상태 확인 필요.", this);
                // gameObject.SetActive(false); // 유효한 위치를 못 찾으면 비활성화 고려
            }
        }
    }

    void Update()
    {
        if (playerTransform == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
        {
            // 필요한 요소가 없거나 NavMesh 위에 있지 않으면 아무것도 하지 않음
            if (navMeshAgent != null && navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath(); // 경로 초기화
            }
            isChasing = false;
            return;
        }
        if (isChasing && !navMeshAgent.isOnNavMesh)
        {
            Debug.LogWarning($"[{gameObject.name}] 추적 중 NavMesh 벗어남 감지!");
            EnsureAgentOnNavMeshLogic();
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            if (navMeshAgent.isOnNavMesh) // NavMesh 위에 있을 때만 목적지 설정
            {
                if (!navMeshAgent.SetDestination(playerTransform.position))
                {
                    Debug.LogError($"[{gameObject.name}] SetDestination 실패! 플레이어 위치: {playerTransform.position}");
                }
                if (!isChasing) // 추적 시작 로그
                {
                    Debug.Log($"[{gameObject.name}] 플레이어 감지, 추적 시작. 거리: {distanceToPlayer}, 목적지: {playerTransform.position}");
                }
            }
            isChasing = true;
        }
        else
        {
            // 플레이어가 감지 반경 이탈 추적 중지
            if (isChasing)
            {
                navMeshAgent.ResetPath(); // 경로 초기화
                isChasing = false;
            }
        }
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[{gameObject.name}] OnTriggerEnter 태그: {other.tag}");

        if (other.CompareTag("Player"))
        {
            Debug.Log($"[{gameObject.name}] 플레이어와 트리거 충돌", this);
            DestroySelf();
        }
    }
    public void TakeDamage(float damageAmount)
    {
        if (enemyBaseData == null && currentHealth <= 0) return;
        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name}: {damageAmount}의 데미지 받음 현재 체력: {currentHealth}/{enemyBaseData.maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        PlayerData playerData = FindFirstObjectByType<PlayerData>();
        if (playerData != null && enemyBaseData != null)
        {
            playerData.GainXP(enemyBaseData.experienceToGive);
        }

        // TODO: 아이템 드랍 로직
        DestroySelf();
    }
    void DestroySelf()
    {
        Debug.Log("DestroySelf 호출됨");
        // NavMeshAgent를 비활성화하여 업데이트를 중지합니다.
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
        }
        // 설정된 딜레이 후 오브젝트를 파괴합니다. 0이면 즉시 파괴됩니다.
        Destroy(gameObject, selfDestructDelay); //selfDestructDelay 이후 OnDisable -> OnDestroy 순으로 호출
    }

    // 감지 반경 표시 (디버깅용)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}