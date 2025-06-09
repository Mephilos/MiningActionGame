using UnityEngine;

public class WeaponController : MonoBehaviour
{
    private static readonly int IsAttack = Animator.StringToHash(("IsAttack"));
    private static readonly int IsChargingAnim = Animator.StringToHash(("IsCharging"));
    
    [Header("통상 무기 셋팅")]
    public WeaponData currentWeaponData;
    public Transform firePoint; // 발사 위치
    public float bulletSpeed = 30f; // 발사체 속도
    public GameObject muzzleFlashPrefab;
    public LayerMask aimLayerMask;
    
    // 참조 컴포넌트
    private PlayerData _playerData; // 플레이어 스탯 참조
    private Transform _ownerTransform; // 사용자 본체
    private Animator _animator;
    
    
    private float _nextFireTime; // 공격속도 관련 변수
    private bool _isFiringRequested = false; 
    private bool _isCharging;
    private float _currentChargeLevel = 0f;
    private GameObject _chargeEffectInstance;
    
    // PlayerController 이용 에임 방향
    public Vector3 AimDirection { get; set; }
    // PlayerController 이용한 락온 타켓 정보
    public Transform AimTarget { get; set; }
    void Awake()
    {
        _ownerTransform = transform;
        _playerData = _ownerTransform.GetComponent<PlayerData>();
        _animator = _ownerTransform.GetComponent<Animator>();
        
        if (currentWeaponData == null) Debug.LogError($"{gameObject.name} WeaponData가 할당 되지 않음.");
        
        if (_playerData == null)
        {
            Debug.LogError($"{gameObject.name} PlayerData가 할당 되지 않음.");
            enabled = false; 
            return;
        }
        if (firePoint == null)
        {
            Debug.LogError($"{gameObject.name}의 WeaponController: FirePoint가 할당되지 않음");
            enabled = false;
        }

        if (_animator == null) Debug.LogError($"[{gameObject.name}]의 WeaponController: Animator가 할당되지 않음");

        AimDirection = _ownerTransform.forward;
    }
    void Start()
    {
        // 게임 플레이 씬이 시작될 때 GameManager에 저장된 무기 정보를 가져옴
        if (GameManager.Instance != null && GameManager.Instance.SelectedWeapon != null)
        {
            // GameManager에서 가져온 WeaponData로 현재 무기를 설정
            this.currentWeaponData = GameManager.Instance.SelectedWeapon;
            Debug.Log($"[{gameObject.name}] '{this.currentWeaponData.weaponName}' 무기 장착");
        }
        else
        {
            if(this.currentWeaponData != null)
            {
                Debug.LogWarning($"[{gameObject.name}] GameManager로부터 무기 정보를 받지 못함 인스펙터에 설정된 '{this.currentWeaponData.weaponName}'을 사용");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] 장착할 무기 정보가 없음");
                this.enabled = false;
            }
        }
    }
    void Update()
    {
        if (_playerData == null || _playerData.isDead)
        {
            if (_isCharging) CancelCharging(); // 죽으면 차징 캔슬
            _isFiringRequested = false;
            return;
        }
        HandleCharging();
        HandleFiring();
    }

    #region 외부입력처리(PlayerController)
    /// <summary>
    /// 발사입력 호출용 (PlayerController)
    /// </summary>
    /// <param name="isPressed">발사 명령시 트루</param>
    public void HandleFireInput(bool isPressed)
    {
        _isFiringRequested = isPressed;
    }
    public void HandleChargeStart()
    {
        if (currentWeaponData != null && currentWeaponData.isChargeWeapon)
        {
            StartCharging();
        }
    }
    public void HandleChargeRelease()
    {
        if (_isCharging)
        {
            if (currentWeaponData.fireOnRelease)
            {
                TryFireChargedLaser();
            }
            CancelCharging();
        }
    }
    #endregion
    
    /// <summary>
    /// 무기 발사 방식 처리
    /// </summary>
    private void HandleFiring()
    {
        // 차지형 무기가 아닐 경우 + 발사 요청
        if (!currentWeaponData.isChargeWeapon && _isFiringRequested)
        {
            if (_animator != null) _animator.SetBool(IsAttack, true);
            TryFireProjectile();
        }
        else
        {
            if (_animator != null) _animator.SetBool(IsAttack, false);
        }
    }
    /// <summary>
    /// 차지 레벨 업데이트
    /// </summary>
    private void HandleCharging()
    {
        if (!_isCharging) return;

        if (currentWeaponData.chargeTime > 0)
        {
            _currentChargeLevel += Time.deltaTime / currentWeaponData.chargeTime;
            _currentChargeLevel = Mathf.Clamp01(_currentChargeLevel);
        }
        else
        {
            _currentChargeLevel = 1f;
        }
    }
    private void StartCharging()
    {
        if (Time.time < _nextFireTime) return; // 이전 발사 후 쿨다운 중이면 차징 시작 안함

        _isCharging = true;
        _currentChargeLevel = 0f;
        if (_animator != null)
        {
            _animator.SetBool(IsAttack, true); // 공격 시작을 알리는 애니메이션
            _animator.SetBool(IsChargingAnim, true); // 특정 차징 애니메이션이 있다면 활성화
        }

        if (currentWeaponData.chargeEffectPrefab != null && _chargeEffectInstance == null)
        {
            _chargeEffectInstance = Instantiate(currentWeaponData.chargeEffectPrefab, firePoint.position, firePoint.rotation, firePoint);
        }
        Debug.Log("차징 시작");
    }
    private void CancelCharging()
    {
        _isCharging = false;
        _currentChargeLevel = 0f;
        if (_animator != null)
        {
            _animator.SetBool(IsAttack, false);
            _animator.SetBool(IsChargingAnim, false);
        }

        if (_chargeEffectInstance != null)
        {
            Destroy(_chargeEffectInstance);
            _chargeEffectInstance = null;
        }
    }

    private void TryFireChargedLaser()
    {
        if (!_isCharging || _currentChargeLevel < currentWeaponData.minChargeToFire) return;

        if (Time.time >= _nextFireTime)
        {
            FireLaser();
            _nextFireTime = Time.time + currentWeaponData.attackSpeed;
        }
    }

    private void TryFireProjectile()
    {
        if (Time.time >= _nextFireTime)
        {
            FireProjectileInternal();
            _nextFireTime = Time.time + _playerData.currentAttackSpeed;
        }
    }private void FireProjectileInternal()
    {
        if (currentWeaponData.projectilePrefab == null) return;
        
        Vector3 fireDirection = this.AimDirection;
        Quaternion bulletRotation = Quaternion.LookRotation(fireDirection);

        if (this.AimTarget != null) // 락온 타겟이 있다면 그쪽으로 방향을 재계산
        {
            Vector3 targetCenter = AimTarget.GetComponent<Collider>()?.bounds.center ?? AimTarget.position;
            fireDirection = (targetCenter - firePoint.position).normalized;
            if (fireDirection != Vector3.zero)
            {
                bulletRotation = Quaternion.LookRotation(fireDirection);
            }
        }

        GameObject bullet = Instantiate(currentWeaponData.projectilePrefab, firePoint.position, bulletRotation);
        
        if (bullet.TryGetComponent<Rigidbody>(out var rb))
        {
             rb.linearVelocity = fireDirection * bulletSpeed;
        }

        if (bullet.TryGetComponent<Projectile>(out var projectileScript))
        {
            float totalDamage = _playerData.currentAttackDamage + currentWeaponData.baseDamage;
            projectileScript.SetDamage(totalDamage);
            projectileScript.isEnemyProjectile = false;

            if (currentWeaponData.explosionRadius > 0f)
            {
                projectileScript.InitializeExplosion(
                    currentWeaponData.explosionRadius,
                    currentWeaponData.explosionEffectPrefab,
                    currentWeaponData.explosionDamageLayerMask
                );
            }
        }
        
        if (muzzleFlashPrefab != null && firePoint != null)
        {
            Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
        }
    }
    private void FireLaser()
    {
        Vector3 fireDirection = this.AimDirection;
        if (this.AimTarget != null)
        {
            Vector3 targetCenter = AimTarget.GetComponent<Collider>()?.bounds.center ?? AimTarget.position;
            fireDirection = (targetCenter - firePoint.position).normalized;
        }
        
        if (fireDirection == Vector3.zero) fireDirection = firePoint.forward;

        float actualDamage = currentWeaponData.baseDamage + _playerData.currentAttackDamage;
        if (currentWeaponData.damageScalesWithCharge)
        {
            float chargeRatio = Mathf.Clamp01((_currentChargeLevel - currentWeaponData.minChargeToFire) / (1f - currentWeaponData.minChargeToFire));
            actualDamage *= Mathf.Lerp(1f, currentWeaponData.maxChargeDamageMultiplier, chargeRatio);
        }
        else if (_currentChargeLevel >= currentWeaponData.minChargeToFire && currentWeaponData.maxChargeDamageMultiplier > 1f && !currentWeaponData.damageScalesWithCharge) 
        {
            actualDamage *= currentWeaponData.maxChargeDamageMultiplier;
        }

        Vector3 laserEndPoint = firePoint.position + fireDirection * currentWeaponData.range;
        if (currentWeaponData.laserEffectPrefab != null)
        {
            GameObject laserEffect = Instantiate(currentWeaponData.laserEffectPrefab, firePoint.position, Quaternion.LookRotation(fireDirection));
            if (laserEffect.TryGetComponent<LaserBeam>(out var beamScript))
            {
                beamScript.Show(firePoint.position, laserEndPoint);
            }
            else
            {
                Destroy(laserEffect, 0.2f);
            }
        }

        if (Physics.Raycast(firePoint.position, fireDirection, out RaycastHit hit, currentWeaponData.range, aimLayerMask))
        {
            // 충돌 이펙트 생성용
            laserEndPoint = hit.point;
            if (hit.collider.TryGetComponent<EnemyBase>(out var enemy)) enemy.TakeDamage(actualDamage);
            if (hit.collider.TryGetComponent<Destructible>(out var destructible)) destructible.TakeDamage(actualDamage);
            // 충돌 지점 이펙트 생성
            if (muzzleFlashPrefab != null)
            {
                // TODO: 레이저 전용 이펙트 추가 WeaponData를 이용
                Instantiate(muzzleFlashPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        // 발사지점 이펙트
        if (muzzleFlashPrefab != null)
        {
            // TODO: 레이저 전용 이펙트 추가 
            Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
        }
    }
}