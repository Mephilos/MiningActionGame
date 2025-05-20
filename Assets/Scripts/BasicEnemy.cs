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

    private float _currentHealth;

    [Header("추적 설정")]
    [Tooltip("플레이어 감지 거리")]
    public float detectionRadius = 15f;
    [Tooltip("이동 속도")]
    public float moveSpeed = 3.5f;
    [Tooltip("자폭대기 시간")]
    public float selfDestructDelay; 

    private Transform _playerTransform;    
    private NavMeshAgent _navMeshAgent;    
    private Rigidbody _rb;                 
    private bool _isChasing;

    public float navMeshSampleRadius = 2.0f;


    void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        if (enemyBaseData == null)
        {
            Debug.LogError($"[{gameObject.name}] EnemyBaseStatsData가 할당되지 않았습니다! 기본값을 사용하거나 스크립트가 비활성화될 수 있습니다.");
            // 기본값으로 초기화하거나, 여기서도 enabled = false; 처리 가능
            _currentHealth = 50f; // 예비용 초기화
        }
        else
        {
            // ScriptableObject로부터 스탯 초기화
            _currentHealth = enemyBaseData.maxHealth;
            detectionRadius = enemyBaseData.detectionRadius;
            moveSpeed = enemyBaseData.moveSpeed;
        }
        if (_navMeshAgent != null)
        {
            _navMeshAgent.updateRotation = true; // 이동 방향으로 자동 회전 활성화
        }
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Rigidbody 컴포넌트 없음", this);
        }

        if (_navMeshAgent == null)
        {
            Debug.LogError($"[{gameObject.name}] NavMeshAgent 컴포넌트 없음", this);
            enabled = false; // NavMeshAgent 없을경우 스크립트 종료
        }


        // 트리거 안전장치
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true; // 트리거 강제 설정
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Collider 컴포넌트가 없습니다");
        }
    }
    void OnEnable()
    {
        if (!EnemySpawner.ActiveEnemies.Contains(this))
        {
            EnemySpawner.ActiveEnemies.Add(this);
        }

        if (enemyBaseData != null) _currentHealth = enemyBaseData.maxHealth;
        else _currentHealth = 50f;
    }

    void OnDisable()
    {
        if(EnemySpawner.ActiveEnemies.Contains(this))
        {
            EnemySpawner.ActiveEnemies.Remove(this);
        }
    }
    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            _playerTransform = playerObject.transform;
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
            _navMeshAgent.speed = enemyBaseData.moveSpeed; // SO의 moveSpeed 사용
        }
        else
        {
            _navMeshAgent.speed = moveSpeed; // SO가 없으면 인스펙터 값 사용
        }
        _navMeshAgent.stoppingDistance = 0.5f; // 플레이어에게 매우 가까이 다가가도록 설정

        // NavMeshAgent가 NavMesh 위에 확실히 위치 (동적 생성 환경에서 중요)
        StartCoroutine(EnsureAgentOnNavMesh());
    }

    IEnumerator EnsureAgentOnNavMesh()
    {
        //navMesh bake 시간 대기 (5프레임)
        for(var i = 0; i < 5 ; i++)
        {
            yield return null;
        }

        EnsureAgentOnNavMeshLogic();
    }
    void EnsureAgentOnNavMeshLogic()
    {
        if (_navMeshAgent != null && !_navMeshAgent.isOnNavMesh && _navMeshAgent.enabled)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                Debug.LogWarning($"[{gameObject.name}] NavMesh에서 벗어남. {transform.position} -> {hit.position} 위치로 Warp 시도.");
                _navMeshAgent.Warp(hit.position);
                if (_isChasing && _playerTransform != null) // Warp 후 다시 목적지 설정
                {
                    _navMeshAgent.SetDestination(_playerTransform.position);
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
        if (_playerTransform == null || !_navMeshAgent.enabled || !_navMeshAgent.isOnNavMesh)
        {
            // 필요한 요소가 없거나 NavMesh 위에 있지 않으면 아무것도 하지 않음
            if (_navMeshAgent != null && _navMeshAgent.hasPath)
            {
                _navMeshAgent.ResetPath(); // 경로 초기화
            }
            _isChasing = false;
            return;
        }
        if (_isChasing && !_navMeshAgent.isOnNavMesh)
        {
            Debug.LogWarning($"[{gameObject.name}] 추적 중 NavMesh 벗어남 감지!");
            EnsureAgentOnNavMeshLogic();
        }

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            if (_navMeshAgent.isOnNavMesh) // NavMesh 위에 있을 때만 목적지 설정
            {
                if (!_navMeshAgent.SetDestination(_playerTransform.position))
                {
                    Debug.LogError($"[{gameObject.name}] SetDestination 실패! 플레이어 위치: {_playerTransform.position}");
                }
                if (!_isChasing) // 추적 시작 로그
                {
                    Debug.Log($"[{gameObject.name}] 플레이어 감지, 추적 시작. 거리: {distanceToPlayer}, 목적지: {_playerTransform.position}");
                }
            }
            _isChasing = true;
        }
        else
        {
            // 플레이어가 감지 반경 이탈 추적 중지
            if (_isChasing)
            {
                _navMeshAgent.ResetPath(); // 경로 초기화
                _isChasing = false;
            }
        }
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[{gameObject.name}] OnTriggerEnter 태그: {other.tag}");

        if (other.CompareTag("Player"))
        {
            PlayerData playerData = other.GetComponent<PlayerData>();
            if (playerData != null)
            {
                if (enemyBaseData != null)
                {
                    float damageToPlayer = enemyBaseData.selfDestructDamage; //자폭 데미지

                    Debug.Log($"[{gameObject.name}]이(가) 플레이어에게 {damageToPlayer}의 데미지를 입힙니다.");
                    playerData.TakeDamage(damageToPlayer);
                }
            }
            Debug.Log($"[{gameObject.name}] 플레이어와 트리거 충돌", this);
            DestroySelf();
        }
    }
    public void TakeDamage(float damageAmount)
    {
        if (enemyBaseData == null && _currentHealth <= 0 && damageAmount > 0)
        {
            Debug.LogWarning($"[{gameObject.name}] EnemyBaseStatsData가 없고 체력이 0 이하인데 데미지를 받으려고 합니다. 이미 죽었을 수 있습니다.");
            return;
        }
        float maxHp = enemyBaseData != null ? enemyBaseData.maxHealth : _currentHealth; // 임시 최대 체력

        _currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name}: {damageAmount}의 데미지 받음. 현재 체력: {_currentHealth}/{maxHp}");

        if (_currentHealth <= 0)
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
        // NavMeshAgent를 비활성화하여 업데이트를 중지합니다
        if (_navMeshAgent != null && _navMeshAgent.enabled)
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.ResetPath();
        }
        // 설정된 딜레이 후 오브젝트를 파괴합니다. 0이면 즉시 파괴됩니다
        Destroy(gameObject, selfDestructDelay); //selfDestructDelay 이후 OnDisable -> OnDestroy 순으로 호출
    }

    // 감지 반경 표시 (디버깅용)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}