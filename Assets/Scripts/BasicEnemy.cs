using UnityEngine;
using UnityEngine.AI; 
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))] 
[RequireComponent(typeof(Rigidbody))]    
[RequireComponent(typeof(Collider))]     
public class BasicEnemy : MonoBehaviour
{
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

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

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
        if(!EnemySpawner.activeEnemies.Contains(this))
        {
            EnemySpawner.activeEnemies.Add(this);
        }
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
        navMeshAgent.speed = moveSpeed;
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

        if (navMeshAgent.isOnNavMesh == false && navMeshAgent.enabled)
        {
            NavMeshHit hit;
            // 현재 위치에서 2.0f 반경 내 가장 가까운 NavMesh 위치 찾기
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                navMeshAgent.Warp(hit.position); // 찾은 위치로 Warp
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] ({transform.position}) 근처에 유효한 NavMesh를 찾을 수 없습니다. 스폰 위치나 NavMesh Bake 상태를 확인하세요.", this);
                gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (playerTransform == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
        {
            // 필요한 요소가 없거나 NavMesh 위에 있지 않으면 아무것도 하지 않음
            if(navMeshAgent != null && navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath(); // 경로 초기화
            }
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            // 플레이어가 감지 반경 내에 있으면 추적 시작
            navMeshAgent.SetDestination(playerTransform.position);
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