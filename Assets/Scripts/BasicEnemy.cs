// BaseEnemy.cs
using UnityEngine;
using UnityEngine.AI; // NavMeshAgent를 사용하기 위해 추가
using System.Collections; // 코루틴 사용을 위해 추가 (선택 사항)

[RequireComponent(typeof(NavMeshAgent))] // 모든 적은 NavMeshAgent를 가져야 함
[RequireComponent(typeof(Rigidbody))] // 충돌 감지를 위해 Rigidbody 필요
public class BaseEnemy : MonoBehaviour
{
    [Header("기본 스탯")]
    [Tooltip("플레이어 감지 범위")]
    public float detectionRange = 15f;
    [Tooltip("이동 속도 (NavMeshAgent에 의해 제어됨)")]
    public float moveSpeed = 3.5f;
    [Tooltip("적 체력")]
    public int maxHealth = 50;
    protected int currentHealth;

    [Header("공격 설정 (자폭형 기본)")]
    [Tooltip("자폭 시 플레이어에게 줄 기본 데미지")]
    public int attackDamage = 10;
    [Tooltip("자폭 시 폭발 반경 (데미지 판정용)")]
    public float explosionRadius = 2.5f;
    [Tooltip("자폭 시 생성될 폭발 이펙트 프리팹")]
    public GameObject explosionEffectPrefab;
    [Tooltip("플레이어와 충돌 후 자폭까지의 딜레이 (0이면 즉시)")]
    public float explosionDelay = 0f;


    [Header("참조")]
    [Tooltip("플레이어 오브젝트 (자동으로 'Player' 태그를 찾아 할당)")]
    protected Transform playerTarget;
    protected NavMeshAgent agent;
    protected Rigidbody rb; // 물리적 상호작용이나 충격에 사용될 수 있음

    [Header("상태")]
    protected bool isPlayerDetected = false;
    protected bool isDead = false;
    protected bool isPathfindingActive = false; // NavMesh 준비 및 경로 탐색 활성화 여부

    /// <summary>
    /// 모든 자식 클래스에서 공통적으로 사용될 초기화 로직.
    /// NavMeshAgent, Rigidbody 등의 컴포넌트를 가져옵니다.
    /// </summary>
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (agent == null)
        {
            Debug.LogError($"{gameObject.name}: NavMeshAgent 컴포넌트를 찾을 수 없습니다!", gameObject);
            enabled = false; // 필수 컴포넌트 없으면 비활성화
            return;
        }
        if (rb == null)
        {
            Debug.LogError($"{gameObject.name}: Rigidbody 컴포넌트를 찾을 수 없습니다!", gameObject);
            // Rigidbody는 충돌 감지에 중요하므로, 없다면 추가하거나 경고 후 비활성화 할 수 있습니다.
            // rb = gameObject.AddComponent<Rigidbody>();
            // rb.isKinematic = true; // NavMeshAgent와 충돌하지 않도록 기본 설정
            enabled = false;
            return;
        }

        // Rigidbody 기본 설정 (NavMeshAgent와 함께 사용할 때)
        // NavMeshAgent가 이동을 주로 담당하므로, Rigidbody는 물리 효과나 충돌 감지에 보조적으로 사용
        rb.isKinematic = true; // 일반적으로 NavMeshAgent 이동 시 true로 설정하여 물리 간섭 최소화
                               // 충돌 기반으로 밀려나거나 하는 효과를 원하면 false로 하고 NavMeshAgent 업데이트와 조율 필요
        rb.useGravity = false; // NavMeshAgent가 Y축 이동을 처리하거나, isKinematic=true면 불필요
    }

    /// <summary>
    /// 게임 시작 시 호출. 플레이어 탐색, NavMeshAgent 설정, 체력 초기화 등을 수행합니다.
    /// 자식 클래스에서 특정 초기화 로직을 추가할 수 있도록 virtual로 선언합니다.
    /// </summary>
    protected virtual void Start()
    {
        currentHealth = maxHealth;

        // 플레이어 탐색
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: 플레이어 ('Player' 태그)를 찾을 수 없습니다. 추적 기능이 제한됩니다.", gameObject);
            // 플레이어가 없어도 씬에 존재할 수 있도록 enabled = false; 주석 처리
        }

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = 1.0f; // 기본 멈춤 거리 (자폭형은 더 짧게 설정 가능)

            // 에이전트가 NavMesh 위에 성공적으로 배치되었는지 확인 후 경로 탐색 시작
            StartCoroutine(InitializePathfinding());
        }
    }

    /// <summary>
    /// 에이전트가 NavMesh에 올바르게 위치할 때까지 기다린 후 경로 탐색을 활성화하는 코루틴입니다.
    /// </summary>
    protected virtual IEnumerator InitializePathfinding()
    {
        // NavMesh가 Bake될 시간을 약간 기다립니다 (특히 런타임 Bake 시).
        // 더 정교한 방법은 NavMeshSurface.activeSurfaces 등을 확인하는 것입니다.
        yield return new WaitForSeconds(0.1f); // 짧은 딜레이

        if (agent.isOnNavMesh)
        {
            isPathfindingActive = true;
        }
        else
        {
            // NavMesh 위에 없다면, 가까운 유효 지점으로 Warp 시도
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, agent.height * 2, NavMesh.AllAreas))
            {
                if (agent.Warp(hit.position))
                {
                    isPathfindingActive = true;
                    // Debug.Log($"{gameObject.name} warped to NavMesh and started pathfinding.");
                }
                else
                {
                    Debug.LogError($"{gameObject.name} failed to warp to NavMesh. Pathfinding disabled.", gameObject);
                }
            }
            else
            {
                Debug.LogError($"{gameObject.name} could not find a valid NavMesh position nearby. Pathfinding disabled.", gameObject);
            }
        }
    }


    /// <summary>
    /// 매 프레임 호출. 적의 주요 행동 로직 (플레이어 감지, 추적, 공격 조건 확인 등)을 담당합니다.
    /// 자식 클래스에서 이 부분을 확장하거나 변경할 수 있습니다.
    /// </summary>
    protected virtual void Update()
    {
        if (isDead || !isPathfindingActive || playerTarget == null)
        {
            // 죽었거나, 경로 탐색이 준비되지 않았거나, 플레이어가 없으면 아무것도 하지 않음
            if (agent != null && agent.isOnNavMesh && agent.hasPath)
            {
                agent.ResetPath(); // 불필요한 경로 이동 중지
            }
            return;
        }

        HandleDetection();
        HandleMovement();
        // 공격 조건 확인 및 실행은 HandleMovement 또는 OnCollisionEnter 등에서 처리될 수 있습니다.
        // 또는 별도의 HandleAttack() 메소드를 만들어 Update에서 호출할 수 있습니다.
    }

    /// <summary>
    /// 플레이어 감지 로직. detectionRange 내에 플레이어가 있는지 확인합니다.
    /// </summary>
    protected virtual void HandleDetection()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer <= detectionRange)
        {
            if (!isPlayerDetected)
            {
                isPlayerDetected = true;
                OnPlayerDetected(); // 플레이어 첫 감지 시 호출될 함수
            }
        }
        else
        {
            if (isPlayerDetected)
            {
                isPlayerDetected = false;
                OnPlayerLost(); // 플레이어를 놓쳤을 때 호출될 함수
            }
        }
    }

    /// <summary>
    /// 플레이어가 처음 감지되었을 때 호출되는 함수입니다.
    /// 자식 클래스에서 특정 행동(예: 포효, 상태 변경)을 추가할 수 있습니다.
    /// </summary>
    protected virtual void OnPlayerDetected()
    {
        // Debug.Log($"{gameObject.name}: Player detected!");
    }

    /// <summary>
    /// 플레이어를 놓쳤을 때 호출되는 함수입니다.
    /// 자식 클래스에서 특정 행동(예: 추적 중지, 경계 상태로 전환)을 추가할 수 있습니다.
    /// </summary>
    protected virtual void OnPlayerLost()
    {
        // Debug.Log($"{gameObject.name}: Player lost.");
        if (agent.isOnNavMesh)
        {
            agent.ResetPath(); // 플레이어를 놓치면 현재 경로 초기화
        }
    }


    /// <summary>
    /// 이동 로직. 플레이어가 감지되면 추적합니다.
    /// </summary>
    protected virtual void HandleMovement()
    {
        if (isPlayerDetected && agent.isOnNavMesh)
        {
            // 목표 지점이 NavMesh 위에 있는지 확인하는 것이 더 안전합니다.
            NavMeshHit hit;
            if (NavMesh.SamplePosition(playerTarget.position, out hit, 1.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                // 플레이어 위치가 NavMesh 밖이라면, 현재 경로를 유지하거나 멈출 수 있습니다.
                // 여기서는 간단히 플레이어의 직접 위치를 목표로 합니다.
                 agent.SetDestination(playerTarget.position);
            }
        }
    }

    /// <summary>
    /// 다른 Collider와 충돌 시 호출 (물리 기반).
    /// 자폭형 적은 여기서 플레이어와 충돌 시 자폭 로직을 실행할 수 있습니다.
    /// Rigidbody의 IsKinematic이 false이거나, Kinematic Rigidbody라도 다른 non-kinematic Rigidbody와 충돌 시 호출.
    /// </summary>
    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        // 자폭형 적의 경우, 플레이어와 직접 충돌했을 때 폭발
        if (collision.gameObject.CompareTag("Player"))
        {
            // Debug.Log($"{gameObject.name} collided with Player.");
            // 자폭 전 약간의 딜레이를 줄 수 있음 (explosionDelay 사용)
            if (explosionDelay > 0)
            {
                StartCoroutine(DelayedExplode(explosionDelay));
            }
            else
            {
                PerformAttack(collision.gameObject); // 즉시 공격/자폭
            }
        }
    }

    /// <summary>
    /// 특정 시간 후 자폭을 실행하는 코루틴입니다.
    /// </summary>
    protected virtual IEnumerator DelayedExplode(float delay)
    {
        // 자폭 대기 중에는 이동을 멈추거나 다른 행동을 할 수 있습니다.
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true; // 이동 중지
        }
        yield return new WaitForSeconds(delay);
        PerformAttack(playerTarget.gameObject); // 딜레이 후 자폭 (이때 playerTarget이 유효한지 확인 필요)
    }


    /// <summary>
    /// 공격 (또는 자폭)을 수행하는 핵심 로직입니다.
    /// 자식 클래스에서 다양한 형태의 공격을 구현할 수 있습니다.
    /// </summary>
    /// <param name="target">공격 대상 (자폭형은 보통 플레이어)</param>
    protected virtual void PerformAttack(GameObject target)
    {
        if (isDead) return; // 이미 죽었으면 실행 안함

        // 자폭형 적의 기본 공격: 폭발
        // Debug.Log($"{gameObject.name} is exploding near {target.name}!");

        // 폭발 이펙트 생성
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 주변에 데미지 주기 (플레이어 및 다른 대상 포함 가능)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            // 플레이어에게 데미지 주기 (PlayerHealth 스크립트가 있다고 가정)
            PlayerController playerHealth = hitCollider.GetComponent<PlayerController>(); // PlayerController 대신 PlayerHealth 같은 스크립트 사용 권장
            if (playerHealth != null && hitCollider.gameObject.CompareTag("Player")) // 태그도 함께 확인
            {
                Debug.Log($"{gameObject.name} deals {attackDamage} damage to {hitCollider.name}");
                // playerHealth.TakeDamage(attackDamage); // 실제 데미지 함수 호출
            }

            // TODO: 다른 적이나 파괴 가능한 오브젝트에게도 데미지를 줄 수 있도록 확장 가능
            // BaseEnemy otherEnemy = hitCollider.GetComponent<BaseEnemy>();
            // if (otherEnemy != null && otherEnemy != this) // 자기 자신은 제외
            // {
            //     otherEnemy.TakeDamage(attackDamage / 2); // 예: 동료에게는 절반의 데미지
            // }
        }

        // 자폭 후 자기 자신 파괴
        Die();
    }

    /// <summary>
    /// 적이 데미지를 입었을 때 호출되는 함수입니다.
    /// </summary>
    /// <param name="amount">받은 데미지 양</param>
    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        // Debug.Log($"{gameObject.name} took {amount} damage. Current health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 피격 애니메이션, 사운드 등 재생 가능
            OnDamaged();
        }
    }

    /// <summary>
    /// 데미지를 입었지만 아직 죽지 않았을 때 호출됩니다. (예: 피격 효과)
    /// </summary>
    protected virtual void OnDamaged()
    {
         // Debug.Log($"{gameObject.name} damaged effect!");
        // 여기에 피격 시 시각/청각 효과 로직 추가
    }


    /// <summary>
    /// 적이 죽었을 때 호출되는 함수입니다.
    /// </summary>
    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // Debug.Log($"{gameObject.name} has died.");

        // 죽음 애니메이션, 사운드, 아이템 드랍 등의 로직 추가 가능
        OnDeathEffects();


        // NavMeshAgent 비활성화 및 오브젝트 파괴
        if (agent != null)
        {
            agent.enabled = false; // NavMeshAgent 비활성화 중요
        }

        // 특정 시간 후 오브젝트 파괴 (죽음 애니메이션 등을 보여줄 시간)
        Destroy(gameObject, 0.1f); // 자폭의 경우 즉시 파괴되었으므로, 일반 사망 시에는 딜레이 고려
    }

    /// <summary>
    /// 죽음 시 발생하는 효과 (파티클, 사운드 등)를 처리합니다.
    /// PerformAttack에서 자폭으로 죽는 경우와, 체력이 다해 죽는 경우 모두 이 함수를 통해 일관된 처리가 가능합니다.
    /// 자폭의 경우 이미 PerformAttack에서 폭발 이펙트를 생성했으므로, 여기서는 추가적인 효과만 담당하거나 중복을 피해야 합니다.
    /// </summary>
    protected virtual void OnDeathEffects()
    {
        // 예: 작은 폭발 파편, 사라지는 효과 등
        // 만약 PerformAttack에서 이미 폭발 이펙트를 생성했다면, 여기서는 일반 사망 시의 효과만 처리하도록 구분 필요
        // if (explosionEffectPrefab != null && !gameObject.name.Contains("ExplodedViaAttack")) // 임시적인 구분 방법
        // {
        //     Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        // }
    }


    /// <summary>
    /// 에디터에서 선택했을 때 감지 범위와 폭발 반경을 시각적으로 표시합니다.
    /// </summary>
    protected virtual void OnDrawGizmosSelected()
    {
        // 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 공격/폭발 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // NavMeshAgent 경로 그리기 (활성화 시)
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.blue;
            var path = agent.path;
            Vector3 prevCorner = transform.position;
            foreach (var corner in path.corners)
            {
                Gizmos.DrawLine(prevCorner, corner);
                Gizmos.DrawSphere(corner, 0.1f);
                prevCorner = corner;
            }
        }
    }
}