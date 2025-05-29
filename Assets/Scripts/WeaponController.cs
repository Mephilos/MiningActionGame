using UnityEngine;

public class WeaponController : MonoBehaviour
{
    private static readonly int IsAttack = Animator.StringToHash(("IsAttack"));

    [Header("통상 무기 셋팅")]
    public WeaponData currentWeaponData;
    public Transform firePoint; // 발사 위치
    public float bulletSpeed = 30f; // 발사체 속도

    private PlayerData _playerData; // 플레이어 스탯 참조
    private Transform _ownerTransform; // 사용자 본체
    private Animator _animator;
    private float _nextFireTime;

    public LayerMask aimLayerMask;
    
    private PlayerController _playerController;

    void Awake()
    {
        _ownerTransform = transform;
        _playerData = _ownerTransform.GetComponent<PlayerData>();
        _playerController = _ownerTransform.GetComponent<PlayerController>(); // PlayerController 참조
        _animator = _ownerTransform.GetComponent<Animator>();
        
        if (currentWeaponData == null)
        {
            Debug.LogError($"{gameObject.name} WeaponData가 할당 되지 않음.");
        }
        if (_playerData == null)
        {
            Debug.LogError($"{gameObject.name} PlayerData가 할당 되지 않음.");
            enabled = false; 
            return;
        }
        if (_playerController == null) 
        {
            Debug.LogError($"{gameObject.name}의 WeaponController: PlayerController를 찾을 수 없음");

        }
        if (firePoint == null)
        {
            Debug.LogError($"{gameObject.name}의 WeaponController: FirePoint가 할당되지 않음");
            enabled = false;
        }

        if (_animator == null)
        {
            Debug.LogError($"[{gameObject.name}]의 WeaponController: Animator가 할당되지 않음");
        }
        /*if (aimLayerMask == 0)
        {
            aimLayerMask = LayerMask.GetMask("Default");
            Debug.LogWarning("WeaponController: aimLayerMask가 설정되지 않아 Default 레이어로 초기화합니다.");
        }*/
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        if (Input.GetMouseButton(0)) // 마우스 왼쪽 버튼 클릭 시 발사
        {
            if (_animator != null)
            {
                _animator.SetBool(IsAttack, true);
            }
            
            TryFireProjectile();
        }
        else
        {
            if (_animator != null)
            {
                _animator.SetBool(IsAttack, false);    
            }
        }
    }
#endif
    void TryFireProjectile()
    {
        if (_playerData == null || currentWeaponData == null) return; 

        if (Time.time >= _nextFireTime)
        {
            FireProjectile();
            // 공격 속도는 PlayerData에서 가져옴
            _nextFireTime = Time.time + _playerData.currentAttackSpeed;
            Debug.Log($"공격 실행. 다음 공격 가능 시간: {_nextFireTime}, 현재 공격 속도(쿨다운): {_playerData.currentAttackSpeed}");
        }
    }
    private void FireProjectile()
    {
        if (currentWeaponData.projectilePrefab == null)
        {
            Debug.LogError("현재 무기 데이터 또는 발사체 프리팹이 설정되지 않았습니다");
            return;
        }

        // 발사 방향과 회전값을 항상 캐릭터의 정면으로 고정
        Vector3 fireDirection = _ownerTransform.forward;
        Quaternion bulletRotation = _ownerTransform.rotation; // 캐릭터의 현재 회전값을 그대로 사용
        Transform lockedTarget = _playerController != null ? _playerController.GetLockedTarget() : null;

        if (lockedTarget != null)
        {
            // 락온 시 로직
            Vector3 targetPositionToAimAt = lockedTarget.position;
            Collider targetCollider = lockedTarget.GetComponent<Collider>();
            if (targetCollider != null)
            {
                targetPositionToAimAt = targetCollider.bounds.center;
            }
            fireDirection = (targetPositionToAimAt - firePoint.position).normalized;
            // 락온 시 fireDirection이 (0,0,0)이 되는 경우, firePoint의 정면을 사용
            if (fireDirection == Vector3.zero) fireDirection = firePoint.forward;
            bulletRotation = Quaternion.LookRotation(fireDirection);
        }
        else // 락온된 타겟이 없을 때 (firePoint의 정면을 기준으로 수평 발사)
        {
            Vector3 worldForwardOfFirePoint = firePoint.forward;

            // 월드 방향
            fireDirection = new Vector3(worldForwardOfFirePoint.x, 0f, worldForwardOfFirePoint.z);

            // 방향 벡터를 정규화 하여 속도 계산에 일관성 부여
            if (fireDirection.sqrMagnitude < 0.001f)
            {
                Vector3 ownerHorizontalForward = _ownerTransform.forward;
                ownerHorizontalForward.y = 0;
                fireDirection = ownerHorizontalForward.normalized;

                // 만약 캐릭터의 전방도 거의 수직이어서 정규화 결과가 0벡터에 가깝다면,
                // 안전하게 월드 Z축을 사용합니다.
                if (fireDirection.sqrMagnitude < 0.001f)
                {
                    fireDirection = Vector3.forward;
                }
            }
            else
            {
                // 수평 방향 정규화
                fireDirection = fireDirection.normalized;
            }
            bulletRotation = firePoint.rotation;
        }

        // 발사체 생성 및 발사
        GameObject bullet = Instantiate(currentWeaponData.projectilePrefab, firePoint.position, bulletRotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        // 발사체 속도 부여
        if (rb != null)
        {
            rb.linearVelocity = fireDirection * bulletSpeed;
        }

        Projectile projectileScript = bullet.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            float totalDamage = _playerData.currentAttackDamage + currentWeaponData.baseDamage;
            projectileScript.SetDamage(totalDamage);
        }
        Debug.Log($"발사체 데미지: {(_playerData.currentAttackDamage + currentWeaponData.baseDamage)}");
    }
    
    /// <summary>
    /// 모바일용 ui
    /// </summary>
    public void OnAttackButtonPressed()
    {
        Debug.Log($"[{gameObject.name}]Attack Button Pressed"); 
        TryFireProjectile();
    }
}