using System;
using UnityEngine;

[RequireComponent(typeof(PlayerData), typeof(Animator))]
public class WeaponController : MonoBehaviour
{
    public event Action<float> OnChargeChanged;
    public event Action<float, float> OnCooldownChanged;

    private static readonly int IsAttack = Animator.StringToHash("IsAttack");

    [Header("무기 설정")]
    public WeaponData currentWeaponData;
    public Transform firePoint; 
    public float bulletSpeed = 30f;
    public GameObject muzzleFlashPrefab;
    public LayerMask aimLayerMask;

    // 컴포넌트 참조
    private PlayerData _playerData;
    private Animator _animator;

    // 내부 상태 변수
    private bool _isCharging;
    private float _currentChargeLevel;
    private float _nextFireTime = 1f;
    private bool _isFiringRequested = false;
    private GameObject _chargeEffectInstance;
    private Vector3 _aimDirection;
    private Transform _aimTarget;
    private IWeaponStrategy _currentStrategy;

    // 프로퍼티
    public bool IsCharging => _isCharging;
    public float CurrentChargeLevel => _currentChargeLevel;

    void Awake()
    {
        _playerData = GetComponent<PlayerData>();
        _animator = GetComponent<Animator>();

        if (_playerData == null) Debug.LogError($"[{gameObject.name}] PlayerData가 할당 되지 않음.");
        if (firePoint == null) Debug.LogError($"[{gameObject.name}]의 WeaponController: FirePoint가 할당되지 않음.");

        _aimDirection = transform.forward;
    }

    void Start()
    {
        // GameManager에서 선택된 무기 정보로 초기화
        if (GameManager.Instance != null && GameManager.Instance.SelectedWeapon != null)
        {
            EquipWeapon(GameManager.Instance.SelectedWeapon);
        }
        else if (currentWeaponData != null)
        {
            Debug.LogWarning($"[{gameObject.name}] GameManager로부터 무기 정보를 받지 못함. 인스펙터에 설정된 '{currentWeaponData.weaponName}'을 사용합니다.");
            EquipWeapon(currentWeaponData);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] 장착할 무기 정보가 없습니다.");
            this.enabled = false;
        }
    }

    /// <summary>
    /// 지정된 무기를 장착하고 발사 형식 설정
    /// </summary>
    public void EquipWeapon(WeaponData newWeaponData)
    {
        currentWeaponData = newWeaponData;
        
        // 무기 데이터에 따라 적절한 발사 형식을 선택
        // TODO: 향후에는 WeaponData에 직접 전략 타입을 명시하는 방식으로 개선할 수 있습니다.
        if (currentWeaponData.isChargeWeapon) // 임시로 차지 여부로 레이저/투사체 구분
        {
            _currentStrategy = new LaserStrategy();
        }
        else
        {
            _currentStrategy = new ProjectileStrategy();
        }
        
        Debug.Log($"[{gameObject.name}] '{currentWeaponData.weaponName}' 무기 장착. 발사 전략: {_currentStrategy.GetType().Name}");
    }

    void Update()
    {
        if (_playerData == null || _playerData.isDead)
        {
            if (_isCharging) CancelCharging();
            _isFiringRequested = false;
            return;
        }
        HandleCharging();
        HandleFiring();
        UpdateCooldowns();
    }

    public void SetAimingData(Vector3 direction, Transform target)
    {
        _aimDirection = direction;
        _aimTarget = target;
    }

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
                TryFire();
            }
            CancelCharging();
        }
    }

    private void HandleFiring()
    {
        if (!currentWeaponData.isChargeWeapon && _isFiringRequested)
        {
            if (_animator != null) _animator.SetBool(IsAttack, true);
            TryFire();
        }
        else
        {
            if (_animator != null) _animator.SetBool(IsAttack, false);
        }
    }

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
        OnChargeChanged?.Invoke(_currentChargeLevel);
    }

    private void UpdateCooldowns()
    {
        float cooldown = GetCurrentCooldown();
        float timeRemaining = _nextFireTime - Time.time;
        OnCooldownChanged?.Invoke(timeRemaining, cooldown);
    }

    private float GetCurrentCooldown()
    {
        float finalAttacksPerSecond = currentWeaponData.attackSpeed + _playerData.currentAttackSpeed;
        if (_playerData.globalAttackCooldownMultiplier > 0)
        {
            finalAttacksPerSecond /= _playerData.globalAttackCooldownMultiplier;
        }
        return (finalAttacksPerSecond > 0) ? (1f / finalAttacksPerSecond) : float.MaxValue;
    }

    private void StartCharging()
    {
        if (Time.time < _nextFireTime) return;

        _isCharging = true;
        _currentChargeLevel = 0f;
        OnChargeChanged?.Invoke(0f);

        if (currentWeaponData.chargeEffectPrefab != null && _chargeEffectInstance == null)
        {
            _chargeEffectInstance = Instantiate(currentWeaponData.chargeEffectPrefab, firePoint.position, firePoint.rotation, firePoint);
        }
    }

    private void CancelCharging()
    {
        _isCharging = false;
        _currentChargeLevel = 0f;
        OnChargeChanged?.Invoke(0f);

        if (_chargeEffectInstance != null)
        {
            Destroy(_chargeEffectInstance);
            _chargeEffectInstance = null;
        }
    }

    private void TryFire()
    {
        if (_isCharging && _currentChargeLevel < currentWeaponData.minChargeToFire) return;
        if (Time.time < _nextFireTime) return;
        
        _currentStrategy.Fire(this, _playerData, firePoint, _aimDirection, _aimTarget);
        _nextFireTime = Time.time + GetCurrentCooldown();
    }

    /// <summary>
    /// 최종 발사 데미지를 계산 (발사 형식 클래스에서 사용)
    /// </summary>
    public float CalculateFinalDamage()
    {
        float baseTotalDamage = _playerData.currentAttackDamage + currentWeaponData.baseDamage;
        float bonusDamageRatio = _playerData.globalDamageMultiplier - 1f;
        float scaledBonusDamage = baseTotalDamage * bonusDamageRatio * currentWeaponData.damageMultiplierScale;
        return baseTotalDamage + scaledBonusDamage;
    }
}
