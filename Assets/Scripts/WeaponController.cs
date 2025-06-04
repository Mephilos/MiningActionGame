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
    private PlayerData _playerData; // 플레이어 스탯 참조
    private Transform _ownerTransform; // 사용자 본체
    private Animator _animator;
    private float _nextFireTime;

    public LayerMask aimLayerMask;
    
    private PlayerController _playerController;

    private bool _isCharging;
    private float _currentChargeLevel = 0f;
    private GameObject _chargeEffectInstance;
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
    }

    void Update()
    {
        if (_playerData == null || _playerData.isDead)
        {
            if (_isCharging) CancelCharging(); // 죽으면 차징 캔슬
            return;
        }
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        if (currentWeaponData != null && currentWeaponData.isChargeWeapon)
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartCharging();
            }
            else if (Input.GetMouseButton(0) && _isCharging)
            {
                UpdateCharging();
            }
            else if (Input.GetMouseButtonUp(0) && _isCharging)
            {
                if (currentWeaponData.fireOnRelease)
                {
                    TryFireChargedLaser();
                }
                // fireOnRelease가 false라면 별도의 발사 버튼이나 로직 필요
            }

            // 마우스 버튼을 떼지 않았는데 차징 상태가 아니라면 (예: 다른 이유로 취소된 경우)
            if (!Input.GetMouseButton(0) && _isCharging)
            {
                CancelCharging();
            }
        }
        else
        {
            if (Input.GetMouseButton(0)) // 마우스 왼쪽 버튼 클릭 시 발사
            {
                if (_animator != null) _animator.SetBool(IsAttack, true);
                TryFireProjectile();
            }
            else
            {
                if (_animator != null) _animator.SetBool(IsAttack, false);
            }
        }
#endif
    }

    void StartCharging()
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
            // 필요하다면 _chargeEffectInstance의 ParticleSystem 등을 제어
        }
        Debug.Log("차징 시작");
    }
    
    void UpdateCharging()
    {
        if (!_isCharging) return;

        if (currentWeaponData.chargeTime > 0)
        {
            _currentChargeLevel += Time.deltaTime / currentWeaponData.chargeTime;
            _currentChargeLevel = Mathf.Clamp01(_currentChargeLevel); // 0과 1 사이로 제한
        }
        else // chargeTime이 0이거나 음수면 즉시 최대 차징
        {
            _currentChargeLevel = 1f;
        }
        // Debug.Log($"차징 중: {_currentChargeLevel * 100:F0}%");
        // 차징 이펙트 업데이트 (예: 크기나 색상 변경)
    }
    void CancelCharging()
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
        Debug.Log("차징 취소");
    }
    void TryFireChargedLaser()
    {
        if (!_isCharging) return;

        if (_currentChargeLevel >= currentWeaponData.minChargeToFire)
        {
            if (Time.time >= _nextFireTime)
            {
                FireLaser();
                _nextFireTime = Time.time + currentWeaponData.attackSpeed; // attackSpeed는 발사 후 쿨다운
            }
            else
            {
                Debug.Log("쿨다운 중입니다.");
            }
        }
        else
        {
            Debug.Log($"최소 차징 필요: {currentWeaponData.minChargeToFire * 100:F0}%, 현재: {_currentChargeLevel * 100:F0}%");
        }
        CancelCharging(); // 발사 시도 후에는 항상 차징 상태 해제
    }
    
    void FireLaser()
    {
        if (_playerData == null || currentWeaponData == null) return;

        // 발사 방향 결정 (기존 FireProjectile과 유사)
        Vector3 fireDirection = _ownerTransform.forward;
        Transform lockedTarget = _playerController != null ? _playerController.GetLockedTarget() : null;

        if (lockedTarget != null)
        {
            Vector3 targetCenter = lockedTarget.position;
            Collider targetCollider = lockedTarget.GetComponent<Collider>();
            if (targetCollider != null) targetCenter = targetCollider.bounds.center;
            fireDirection = (targetCenter - firePoint.position).normalized;
        }
        else
        {
            // 카메라 또는 캐릭터 정면 기준으로 발사 방향 설정 (PlayerController의 조준 로직 활용 가능)
            // 간단하게는 firePoint.forward 또는 캐릭터의 정면을 사용
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // PC 기준 마우스 포인터
            Plane groundPlane = new Plane(Vector3.up, firePoint.position); // 플레이어 발사점 높이의 평면
            if (groundPlane.Raycast(ray, out float rayDistance))
            {
                Vector3 worldPoint = ray.GetPoint(rayDistance);
                fireDirection = (worldPoint - firePoint.position).normalized;
            } else {
                fireDirection =_playerController.transform.forward; // 정면으로 조준점 없을시
            }
        }
        if (fireDirection == Vector3.zero) fireDirection = firePoint.forward; // 방향이 0벡터가 되는 것 방지

        // 데미지 계산
        float actualDamage = currentWeaponData.baseDamage + _playerData.currentAttackDamage;
        if (currentWeaponData.damageScalesWithCharge)
        {
            // 최소 차징부터 최대 차징까지 선형적으로 데미지 증가 (최소 데미지는 baseDamage, 최대는 baseDamage * multiplier)
            float chargeRatio = (_currentChargeLevel - currentWeaponData.minChargeToFire) / (1f - currentWeaponData.minChargeToFire);
            chargeRatio = Mathf.Clamp01(chargeRatio); // minChargeToFire일 때 0, 최대일 때 1
            actualDamage *= Mathf.Lerp(1f, currentWeaponData.maxChargeDamageMultiplier, chargeRatio);
        } else if (_currentChargeLevel >= currentWeaponData.minChargeToFire && currentWeaponData.maxChargeDamageMultiplier > 1f && !currentWeaponData.damageScalesWithCharge) {
             actualDamage *= currentWeaponData.maxChargeDamageMultiplier; // 차징만 완료되면 최대 데미지 배율 적용
        }


        Debug.Log($"레이저 발사! 충전율: {_currentChargeLevel * 100:F0}%, 데미지: {actualDamage}");

        // 레이저 빔 시각 효과 생성
        Vector3 laserEndPoint = firePoint.position + fireDirection * currentWeaponData.range;
        if (currentWeaponData.laserEffectPrefab != null)
        {
            GameObject laserEffect = Instantiate(currentWeaponData.laserEffectPrefab, firePoint.position, Quaternion.LookRotation(fireDirection));
            // LaserBeam 스크립트가 있다면 시작점과 끝점 설정 (아래 3단계에서 만들 스크립트)
            LaserBeam beamScript = laserEffect.GetComponent<LaserBeam>();
            if (beamScript != null)
            {
                beamScript.Show(firePoint.position, laserEndPoint); // 초기에는 최대 사거리까지 그림
            }
            else
            {
                // LaserBeam 스크립트가 없다면, LineRenderer를 직접 제어하거나 간단히 시간차 두고 파괴
                 Destroy(laserEffect, 0.2f); // 예시: 0.2초 뒤 파괴
            }
        }

        // Raycast로 실제 충돌 확인
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, fireDirection, out hit, currentWeaponData.range, aimLayerMask))
        {
            laserEndPoint = hit.point; // 실제 충돌 지점으로 끝점 수정
            Debug.Log($"레이저 충돌: {hit.collider.name}");

            // 적에게 데미지 주기
            EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(actualDamage);
            }
            // 파괴 가능한 오브젝트에 데미지 주기
            Destructible destructible = hit.collider.GetComponent<Destructible>();
            if (destructible != null)
            {
                destructible.TakeDamage(actualDamage);
            }

            // 충돌 지점에 이펙트 생성 (기존 impactEffectPrefab 활용 가능)
            if (muzzleFlashPrefab != null) // 여기서 muzzleFlashPrefab은 사실상 'impactEffectPrefab'으로 해석하는 것이 적절
            {
                 // WeaponData에 laserImpactEffectPrefab 필드를 추가하는 것이 더 명확함
                Instantiate(muzzleFlashPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        
        // 레이저 빔 시각 효과의 끝점을 실제 충돌 지점이나 최대 사거리로 다시 한번 업데이트 (선택적, LaserBeam 스크립트가 있다면 그쪽에서 처리)
        // 이미 생성된 laserEffect의 LineRenderer를 찾아서 SetPosition(1, laserEndPoint) 호출

        // 총구 화염 (Muzzle Flash) - 레이저 무기에도 적용할 수 있음
        if (muzzleFlashPrefab != null)
        {
            Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
        }

        // 발사 후 애니메이션 트리거 (선택 사항)
        if (_animator != null) _animator.SetTrigger(IsAttack); // 또는 "FireLaser" 같은 별도 트리거
    }
    void TryFireProjectile()
    {
        if (_playerData == null || currentWeaponData == null) return; 

        if (Time.time >= _nextFireTime)
        {
            FireProjectileInternal();
            // 공격 속도는 PlayerData에서 가져옴
            _nextFireTime = Time.time + _playerData.currentAttackSpeed;
            Debug.Log($"공격 실행. 다음 공격 가능 시간: {_nextFireTime}, 현재 공격 속도(쿨다운): {_playerData.currentAttackSpeed}");
        }
    }
    void FireProjectileInternal() // 기존 FireProjectile 메서드 내용을 여기로 옮기고 약간 수정
    {
        if (currentWeaponData.projectilePrefab == null) //
        {
            Debug.LogError("현재 무기 데이터 또는 발사체 프리팹이 설정되지 않았습니다"); //
            return;
        }
        
        Vector3 fireDirection = _ownerTransform.forward; //
        Quaternion bulletRotation = _ownerTransform.rotation; //
        Transform lockedTarget = _playerController != null ? _playerController.GetLockedTarget() : null; //

        if (lockedTarget != null) //
        {
            Vector3 targetPositionToAimAt = lockedTarget.position; //
            Collider targetCollider = lockedTarget.GetComponent<Collider>(); //
            if (targetCollider != null) targetPositionToAimAt = targetCollider.bounds.center; //
            fireDirection = (targetPositionToAimAt - firePoint.position).normalized; //
            if (fireDirection == Vector3.zero) fireDirection = firePoint.forward; //
            bulletRotation = Quaternion.LookRotation(fireDirection); //
        }
        else 
        {
            Vector3 worldForwardOfFirePoint = firePoint.forward; //
            fireDirection = new Vector3(worldForwardOfFirePoint.x, 0f, worldForwardOfFirePoint.z); //
            if (fireDirection.sqrMagnitude < 0.001f) //
            {
                Vector3 ownerHorizontalForward = _ownerTransform.forward; //
                ownerHorizontalForward.y = 0; //
                fireDirection = ownerHorizontalForward.normalized; //
                if (fireDirection.sqrMagnitude < 0.001f) fireDirection = Vector3.forward; //
            }
            else fireDirection = fireDirection.normalized; //
            bulletRotation = Quaternion.LookRotation(fireDirection); //
        }

        GameObject bullet = Instantiate(currentWeaponData.projectilePrefab, firePoint.position, bulletRotation); //
        Rigidbody rb = bullet.GetComponent<Rigidbody>(); //
        if (rb != null) rb.linearVelocity = fireDirection * bulletSpeed; //

        Projectile projectileScript = bullet.GetComponent<Projectile>(); //
        if (projectileScript != null) //
        {
            float totalDamage = _playerData.currentAttackDamage + currentWeaponData.baseDamage; //
            projectileScript.SetDamage(totalDamage); //
            projectileScript.isEnemyProjectile = false; //

            if (currentWeaponData.explosionRadius > 0f) //
            {
                projectileScript.InitializeExplosion( //
                    currentWeaponData.explosionRadius, //
                    currentWeaponData.explosionEffectPrefab, //
                    currentWeaponData.explosionDamageLayerMask //
                );
            }
        }
        
        if (muzzleFlashPrefab != null && firePoint != null) //
        {
            Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint); //
        }
    }
    
    /// <summary>
    /// 모바일용 ui
    /// </summary>
    public void OnAttackButtonPressed() // 모바일 버튼 (탭 방식) //
    {
        if (currentWeaponData != null && currentWeaponData.isChargeWeapon)
        {
            // 모바일 차징 로직은 더 복잡함. (예: 버튼 누르고 있으면 차징, 떼면 발사 또는 별도 발사 버튼)
            // 현재는 탭하면 최소 차징으로 즉시 발사 시도 또는 차징 시작/발사 토글 형태로 구현 가능
            if (!_isCharging)
            {
                StartCharging(); 
                // 짧은 시간 뒤 최소 차징으로 자동 발사하거나, 특정 조건 만족 시 발사하도록 설계 필요
                // 예: StartCoroutine(AutoFireAfterMinCharge());
            }
            else // 이미 차징 중일 때 또 누르면 발사 (간단한 토글 방식)
            {
                TryFireChargedLaser();
            }
        }
        else // 일반 무기
        {
            if (_animator != null) _animator.SetBool(IsAttack, true); //
            TryFireProjectile(); //
            // 일반 무기의 경우, 버튼을 떼는 애니메이션 처리가 필요하면 추가 구현
            // Invoke(nameof(ResetAttackAnim), 0.1f); // 예시
        }
    }
}