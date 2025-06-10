using UnityEngine;

public class HelpDroneController : MonoBehaviour
{
    public PlayerHelpDroneData droneData;
    public Transform playerTransform;
    public Transform firePoint;
    public LayerMask enemyLayerMask;

    public Vector3 positionOffset = new Vector3(1.5f, 2.0f, 0);
    public float followSpeed = 8f;
    public float rotationSpeed = 10f;

    private Transform _currentTarget;
    private float _attackCooldownTimer;

    void Update()
    {
        if (playerTransform == null || playerTransform.GetComponent<PlayerData>().isDead)
        {
            return;
        }
        HandleTargeting();
        HandleAttackCooldown();
        HandleRotation();
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;
        HandleFollowing();
    }

    void HandleFollowing()
    {
        Vector3 targetPosition = playerTransform.position + positionOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }

    void HandleRotation()
    {
        if (_currentTarget != null)
        {
            Vector3 directionToTarget = (_currentTarget.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void HandleTargeting()
    {
        Transform closestEnemy = null;
        float minDistanceSqr = float.MaxValue;

        Collider[] enemies = Physics.OverlapSphere(transform.position, droneData.detectionRadius, enemyLayerMask);

        foreach (Collider enemyCollider in enemies)
        {
            float distanceSqr = (transform.position - enemyCollider.transform.position).sqrMagnitude;
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                closestEnemy = enemyCollider.transform;
            }
        }
        _currentTarget = closestEnemy;
    }

    void HandleAttackCooldown()
    {
        if (_attackCooldownTimer > 0)
        {
            _attackCooldownTimer -= Time.deltaTime;
        }
        else if (_currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
            if (distanceToTarget <= droneData.attackRange)
            {
                Attack();
            }
        }
    }

    void Attack()
    {
        Debug.Log("공격");
        if (droneData.projectilePrefab == null || firePoint == null) return;

        // 쿨다운 초기화
        _attackCooldownTimer = droneData.attackCooldown;

        // 투사체 생성
        GameObject projectileGO = Instantiate(droneData.projectilePrefab, firePoint.position, firePoint.rotation);
        
        // 투사체에 데미지 설정 (플레이어의 공격력이 아닌 드론 자체의 공격력 사용)
        if (projectileGO.TryGetComponent<Projectile>(out var projectileScript))
        {
            projectileScript.SetDamage(droneData.attackDamage);
            projectileScript.isEnemyProjectile = false;
        }

        // 투사체에 속도 부여
        if (projectileGO.TryGetComponent<Rigidbody>(out var rb))
        {
            Vector3 direction = (_currentTarget.position - firePoint.position).normalized;
            rb.linearVelocity = direction * droneData.projectileSpeed;
            projectileGO.transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
