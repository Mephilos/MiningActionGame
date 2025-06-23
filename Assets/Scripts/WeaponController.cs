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
        
    private bool _isCharging;
    private float _currentChargeLevel;
    private float _nextFireTime = 1f;
    private bool _isFiringRequested = false;
    private GameObject _chargeEffectInstance;
    
    public bool IsCharging => _isCharging;
    public float CurrentChargeLevel => _currentChargeLevel;
    public Vector3 AimDirection { get; set; }
    // PlayerController를 이용(락온 타켓 정보)
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
            if (_playerData != null && _playerData.hpBarInstance != null)
            {
                _playerData.hpBarInstance.SetAttackGaugeVisibility(false);
            }
            return;
        }
        HandleCharging();
        HandleFiring();
        UpdateAttackGaugeUI();
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
        float weaponBaseAps = currentWeaponData.attackSpeed;
        float playerBonusAps = _playerData.currentAttackSpeed;
        float finalAttacksPerSecond = weaponBaseAps + playerBonusAps;

        if (_playerData.globalAttackCooldownMultiplier > 0)
        {
            finalAttacksPerSecond /= _playerData.globalAttackCooldownMultiplier;
        }
        if (finalAttacksPerSecond <= 0) return;
        float finalCooldown = 1f / finalAttacksPerSecond;
        if (Time.time >= _nextFireTime)
        {
            FireProjectileInternal();
            _nextFireTime = Time.time + finalCooldown;
        }
    }
    private void FireProjectileInternal()
    {
        if (currentWeaponData.projectilePrefab == null) return;
        
        string projectileTag = currentWeaponData.projectilePrefab.name;
        
        Vector3 fireDirection = this.AimDirection;
        Quaternion bulletRotation = Quaternion.LookRotation(fireDirection);
        
        GameObject bullet = ObjectPoolManager.Instance.GetFromPool(projectileTag, firePoint.position, firePoint.rotation);
        bullet.transform.rotation = bulletRotation;

        
        if (this.AimTarget != null) // 락온 타겟이 있다면 그쪽으로 방향을 재계산
        {
            Vector3 targetCenter = AimTarget.GetComponent<Collider>()?.bounds.center ?? AimTarget.position;
            fireDirection = (targetCenter - firePoint.position).normalized;
            // if (fireDirection != Vector3.zero)
            // {
            //     bulletRotation = Quaternion.LookRotation(fireDirection);
            // }
        }
        
        if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
             rb.linearVelocity = fireDirection * bulletSpeed;
        }

        if (bullet.TryGetComponent<Projectile>(out Projectile projectileScript))
        {
            // 플레이어 무기의 기본 데미지 합산값
            float baseTotalDamage = _playerData.currentAttackDamage + currentWeaponData.baseDamage;
            // 퍽, 업그레이드 등으로 인한 순수 보너스 데미지 비율
            float bonusDamageRatio = _playerData.globalDamageMultiplier - 1f;
            // 보너스 데미지 비율에 무기별 계수 계산
            float scaledBonusDamage = baseTotalDamage * bonusDamageRatio * currentWeaponData.damageMultiplierScale;
            // 기본 데미지에 보너스 데미지 합산
            float finalDamage = baseTotalDamage + scaledBonusDamage;
            
            Debug.Log($"무기: {currentWeaponData.weaponName}, 최종 데미지: {finalDamage} (기본: {baseTotalDamage}, 보너스: {scaledBonusDamage})");

            projectileScript.SetDamage(finalDamage);
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
            GameObject muzzleFlash = ObjectPoolManager.Instance.GetFromPool(muzzleFlashPrefab.name, firePoint.position, firePoint.rotation);
            if (muzzleFlash != null)
            {
                muzzleFlash.transform.parent = firePoint;
            }
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
        
        float baseTotalDamage = currentWeaponData.baseDamage + _playerData.currentAttackDamage;
        float bonusDamageRatio = _playerData.globalDamageMultiplier - 1f;
        float scaledBonusDamage = baseTotalDamage * bonusDamageRatio * currentWeaponData.damageMultiplierScale;
        float finalDamage = baseTotalDamage + scaledBonusDamage;
        
        
        if (currentWeaponData.damageScalesWithCharge)
        {
            float chargeRatio = Mathf.Clamp01((_currentChargeLevel - currentWeaponData.minChargeToFire) / (1f - currentWeaponData.minChargeToFire));
            finalDamage *= Mathf.Lerp(1f, currentWeaponData.maxChargeDamageMultiplier, chargeRatio);
        }
        
        else if (_currentChargeLevel >= currentWeaponData.minChargeToFire && currentWeaponData.maxChargeDamageMultiplier > 1f) 
        {
            finalDamage *= currentWeaponData.maxChargeDamageMultiplier;
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
            laserEndPoint = hit.point;
            if (hit.collider.TryGetComponent<EnemyBase>(out var enemy)) enemy.TakeDamage(finalDamage);
            if (hit.collider.TryGetComponent<Destructible>(out var destructible)) destructible.TakeDamage(finalDamage); 
            
            if (muzzleFlashPrefab != null)
            {
                ObjectPoolManager.Instance.GetFromPool(muzzleFlashPrefab.name, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        
        if (muzzleFlashPrefab != null)
        {
            ObjectPoolManager.Instance.GetFromPool(muzzleFlashPrefab.name, firePoint.position, firePoint.rotation);
        }
    }
    private void UpdateAttackGaugeUI()
    {
        if (_playerData == null || _playerData.hpBarInstance == null || currentWeaponData == null) return;
        
        _playerData.hpBarInstance.SetAttackGaugeVisibility(true);
        // 차지 무기, 일반 무기 판별 다른 로직 적용
        if (currentWeaponData.isChargeWeapon)
        {
            _playerData.hpBarInstance.UpdateAttackGauge(_currentChargeLevel);
        }
        else
        {
            float finalAttacksPerSecond = _playerData.currentAttackSpeed / _playerData.globalAttackCooldownMultiplier;
            if (finalAttacksPerSecond <= 0) return;
        
            float finalCooldown = 1f / finalAttacksPerSecond;
            float timeRemaining = _nextFireTime - Time.time;
            
            float fillAmount = (timeRemaining > 0) ? (timeRemaining / finalCooldown) : 0f;
            _playerData.hpBarInstance.UpdateAttackGauge(fillAmount);
        }
    }
}