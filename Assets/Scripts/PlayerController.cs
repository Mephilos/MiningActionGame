using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Grounded = Animator.StringToHash("Grounded");
    private static readonly int IsDead = Animator.StringToHash("IsDead");
    private static readonly int IsDashing = Animator.StringToHash("IsDashing");
    private static readonly int Aiming = Animator.StringToHash("IsAiming");
    private static readonly int JumpTrigger = Animator.StringToHash("JumpTrigger");
    private static readonly int ThrowTrigger = Animator.StringToHash("ThrowTrigger");
    private static readonly int IsAimingSkill = Animator.StringToHash("IsAimingSkill");
    private static readonly int IsActionLockedAnim = Animator.StringToHash("IsActionLocked");
    
    
    [SerializeField] private float isometricCameraAngleY = 45f;
    [SerializeField] private float shortPressThreshold = 0.2f;

    private CharacterController _characterController;
    private PlayerData _playerData;
    private Animator _animator;
    private WeaponController _weaponController;

    private Vector3 _worldTargetDirection = Vector3.forward;
    private Vector3 _currentVelocity = Vector3.zero;
    private float _currentSpeed;
    private Vector3 _verticalVelocity = Vector3.zero;

    private float _shiftKeyDownTime;
    private bool _shiftKeyHeld;

    private Coroutine _dashCoroutine;
    private Coroutine _invincibilityCoroutine;
    private Coroutine _knockbackCoroutine;

    private bool _isBeingKnockedBack;

    [Header("스킬 설정")]
    [SerializeField] private TrajectoryPredictor trajectoryPredictor;
    [Tooltip("수류탄을 던질 시작 위치")] public Transform grenadeThrowPoint;
    private bool _isAimingSkill = false;


    public bool IsAiming { get; private set; }
    private Vector3 _aimingDirection;
    public Vector3 AimingDirection => _aimingDirection;
    private Transform _lockedAimAssistTarget; // 현재 감지된 에임 어시스트 타겟
    
    [Header("Mobile Controls")]
    public Joystick movementJoystick;

    
    [Header("Aim Assist Settings")]
    public float aimAssistRadius = 10f; // 에임 어시스트 감지 반경
    public float horizontalAimConeAngle = 15f; // 플레이어 정면 기준 각도
    public float verticalAimConeAngle = 25f; // 플레이어 정면 기준 수직
    public LayerMask aimAssistLayerMask; // 에임 어시스트 적용 레이어 마스크 
    public LayerMask obstacleLayerMask; // 장애물 판별용 레이어 마스크 
    public float onReticleHorizontalAngle = 5.0f; // 조준선 일치 각도
    
    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerData = GetComponent<PlayerData>();
        _animator = GetComponent<Animator>();
        _weaponController = GetComponent<WeaponController>();

        if (_playerData == null)
        {
            Debug.LogError("플레이어 데이터 컴포넌트가 설정되지 않음");
            enabled = false;
            return;
        }
        if (_weaponController == null) Debug.LogError("WeaponController를 찾을 수 없음");
    }
    void Update()
    {
        // 죽거나 데이터 셋 없을 때
        if (_playerData == null || _playerData.isDead)
        {
            _lockedAimAssistTarget = null;
            // 죽음 애니메이션 처리
            _animator.SetBool(IsDead, _playerData.isDead);
            
            return;
        }
        HandleInput();
        HandleSkillInput();
        UpdateAiming();
        PassDataToWeaponController();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (_playerData == null || _playerData.isDead) return;
        if (_playerData.isDashing)
        {
            ApplyFinalMovement();
            return; // 대쉬 중에는 이동 로직 건너뜀 록맨X 점프 대쉬 같이 하기 위
        }
        HandleMovement();
        ApplyRotationFixedUpdate();
        ApplyGravity();
        ApplyFinalMovement();
    }

    public void LockAction()
    {
        if (_animator != null) _animator.SetBool(IsActionLockedAnim, true);
    }
    public void UnlockAction()
    {
        if (_animator != null) _animator.SetBool(IsActionLockedAnim, false);
    }
    public void ExecuteGrenadeThrow()
    {
        SkillData currentSkill = _weaponController.currentWeaponData.specialSkill;
        if (currentSkill == null) return;
        
        ThrowSkillGrenade(currentSkill);
        _playerData.currentSkillCooldown = currentSkill.cooldown;
    }
    void HandleSkillInput()
    {
        SkillData currentSkill = _weaponController.currentWeaponData.specialSkill;
        
        if (currentSkill == null || trajectoryPredictor == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 스킬 설정에 이슈 발생 ");
            return;
        }
        // Q키 누르기 시작 할 때
        if (Input.GetKeyDown(KeyCode.Q) && _playerData.currentSkillCooldown <= 0)
        {
            _isAimingSkill = true;
            if (_animator != null) _animator.SetBool(IsAimingSkill, true);
            trajectoryPredictor.Show();
        }

        // Q키를 누르고 있는 동안
        if (_isAimingSkill && Input.GetKey(KeyCode.Q))
        {
            // 마우스 위치에 따라 궤도 실시간 업데이트
            Vector3 throwDirection = GetThrowDirection(currentSkill.throwForce);
            trajectoryPredictor.PredictTrajectory(grenadeThrowPoint.position, throwDirection);
        }

        // Q키를 뗐을 때 
        if (_isAimingSkill && Input.GetKeyUp(KeyCode.Q))
        {
            if (_animator != null)
            {
                _animator.SetBool(IsAimingSkill, false);
                _animator.SetTrigger(ThrowTrigger);
            }
            _isAimingSkill = false;
            trajectoryPredictor.Hide();
        }
    }
    private Vector3 GetThrowDirection(float force)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~LayerMask.GetMask("Player")))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(50f);
        }
        
        Vector3 direction = (targetPoint - grenadeThrowPoint.position).normalized;
        return direction * force;
    }

    /// <summary>
    /// 스킬 수류탄을 실제로 던지는 메서드
    /// </summary>
    private void ThrowSkillGrenade(SkillData skill)
    {
        if (skill.grenadePrefab == null) return;
        
        Vector3 throwDirection = GetThrowDirection(skill.throwForce);
        GameObject grenadeGO = Instantiate(skill.grenadePrefab, grenadeThrowPoint.position, Quaternion.identity);

        if (grenadeGO.TryGetComponent<SkillGrenade>(out var skillGrenade))
        {
            skillGrenade.sourceSkillData = skill;
        }

        if (grenadeGO.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = throwDirection; // AddForce 대신 velocity를 직접 설정하여 궤도 예측과 일치시킴
        }
    }


    void HandleInput()
    {
        if (_isBeingKnockedBack) return;

        float moveX = 0f;
        float moveZ = 0f;

#if UNITY_IOS || UNITY_ANDROID// 모바일 플랫폼
        if (movementJoystick != null && movementJoystick.Direction.sqrMagnitude > 0.01f) // 조이스틱이 할당되어 있고, 입력이 있을 때
        { 
            moveX = movementJoystick.Horizontal; // 조이스틱의 Horizontal 값 사용
            moveZ = movementJoystick.Vertical;   // 조이스틱의 Vertical 값 사용
        }
#endif 

#if UNITY_EDITOR || UNITY_STANDALONE
        // pc
        moveX = Input.GetAxisRaw("Horizontal");
        moveZ = Input.GetAxisRaw("Vertical");
#endif
        Vector3 rawInputDir = new Vector3(moveX, 0, moveZ);
        
        // 이동 입력 방향 계산 (카메라 각도 고려)
        if (rawInputDir.sqrMagnitude > 0.01f)
        {
            _worldTargetDirection = Quaternion.Euler(0, isometricCameraAngleY, 0) * rawInputDir.normalized;
        }
        else 
        {
            _worldTargetDirection = Vector3.zero; // 이동 입력 없으면 0벡터
        }
#if !(UNITY_IOS || UNITY_ANDROID) // PC 환경
        // Shift 키 (대쉬/부스트)
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            _shiftKeyDownTime = Time.time;
            _shiftKeyHeld = true;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            _shiftKeyHeld = false;
            if (Time.time - _shiftKeyDownTime < shortPressThreshold) // 짧게 눌렀다 떼면 대쉬
            {
                TryDash();
            }
        }
        // Space 키 (점프)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryJump();
        }
#endif
        
#if UNITY_EDITOR || UNITY_STANDALONE
        // 마우스 우클릭 (조준 상태)
        IsAiming = Input.GetMouseButton(1);

        if (_weaponController.currentWeaponData != null && _weaponController.currentWeaponData.isChargeWeapon)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _weaponController.HandleChargeStart();
            }
            if (Input.GetMouseButtonUp(0))
            {
                _weaponController.HandleChargeRelease();
            }
        }
        else
        {
            _weaponController.HandleFireInput(Input.GetMouseButton(0));
        }
#endif
    }
    void UpdateAiming()
    {
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, transform.position);

            if (groundPlane.Raycast(ray, out float rayDistance))
            {
                Vector3 targetPoint = ray.GetPoint(rayDistance);
                _aimingDirection = (targetPoint - transform.position);
                _aimingDirection.y = 0;
                if(_aimingDirection.sqrMagnitude > 0.01f) _aimingDirection.Normalize();
            }
        }

        if (IsAiming)
        {
            UpdateLockOnTargetLogic();
        }
        else
        {
            _lockedAimAssistTarget = null;
        }
    }
    
    void PassDataToWeaponController()
    {
        if (_weaponController == null) return;
        
        _weaponController.AimDirection = (_aimingDirection.sqrMagnitude > 0.01f) ? _aimingDirection : transform.forward;
        _weaponController.AimTarget = _lockedAimAssistTarget;
    }


    void ApplyRotationFixedUpdate()
    {
        Vector3 finalAimDirToRotate = transform.forward; // 기본값은 현재 바라보는 방향 유지

        if (IsAiming && _aimingDirection.sqrMagnitude > 0.01f)
        {
            finalAimDirToRotate = _aimingDirection;
        }
        else if (!IsAiming && _worldTargetDirection.sqrMagnitude > 0.01f)
        {
            finalAimDirToRotate = _worldTargetDirection;
        }

        if (finalAimDirToRotate.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(finalAimDirToRotate, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                _playerData.currentRotationSpeed * Time.fixedDeltaTime);
        }
    }

    void HandleMovement()
    {
        float actualMaxSpeed = _playerData.currentMaxSpeed;
        float actualAcceleration = _playerData.currentAcceleration;
        float actualDeceleration = _playerData.currentDeceleration; 


        if (_shiftKeyHeld && (Time.time - _shiftKeyDownTime >= shortPressThreshold) && !_playerData.isDashing)
        {
            actualMaxSpeed *= _playerData.currentBoostFactor;
            actualAcceleration *= _playerData.currentBoostFactor;
        }
        bool isMovingInput = _worldTargetDirection.sqrMagnitude > 0.01f;

        if (isMovingInput)
        {
            // 조준 중일 때 이동 속도
            if (IsAiming) { actualMaxSpeed *= 0.5f; } // 조준 시 이동속도 절반

            _currentSpeed += actualAcceleration * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Clamp(_currentSpeed, 0, actualMaxSpeed);
        }
        else
        {
            // 이동 입력이 없을 때: 감속
            _currentSpeed -= actualDeceleration * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Max(0, _currentSpeed); // 속도가 0 이하로 내려가지 않도록
        }
        _currentVelocity = isMovingInput ? _worldTargetDirection.normalized * _currentSpeed : Vector3.zero;
    }

    void ApplyGravity()
    {
        if (_characterController.isGrounded)
        {
            if (_verticalVelocity.y < 0.0f)
            {
                _verticalVelocity.y = -0.5f; 
            }
           
            if (_playerData.jumpCountAvailable < _playerData.currentMaxJumpCount)
            {
                _playerData.jumpCountAvailable = _playerData.currentMaxJumpCount;
            }
        }
        else
        {
            // 공중에서는 중력 적용
            _verticalVelocity.y += Physics.gravity.y * Time.fixedDeltaTime;
        }
    }

    void ApplyFinalMovement()
    {
        Vector3 finalMove = (_currentVelocity + _verticalVelocity) * Time.fixedDeltaTime;
        _characterController.Move(finalMove);

        
        if (StageManager.Instance != null) 
        {
            Vector3 currentPosition = transform.position;
            
            Vector2Int currentStageCoord = StageManager.Instance.CurrentStageCoord;
            float sSize = StageManager.Instance.stageSize; 
            float playerRadius = _characterController.radius;
            
            float stageWorldMinX = currentStageCoord.x * sSize + playerRadius;
            float stageWorldMaxX = (currentStageCoord.x * sSize) + sSize - playerRadius;
            float stageWorldMinZ = currentStageCoord.y * sSize + playerRadius; 
            float stageWorldMaxZ = (currentStageCoord.y * sSize) + sSize - playerRadius;
            
            float clampedX = Mathf.Clamp(currentPosition.x, stageWorldMinX, stageWorldMaxX);
            float clampedZ = Mathf.Clamp(currentPosition.z, stageWorldMinZ, stageWorldMaxZ);
            
            if (!Mathf.Approximately(currentPosition.x, clampedX) || !Mathf.Approximately(currentPosition.z, clampedZ))
            {
                transform.position = new Vector3(clampedX, currentPosition.y, clampedZ);
            }
        }
    }
    
    void TryJump()
    {
        if (_playerData == null || _playerData.isDashing) return;
    
        if (_characterController.isGrounded) 
        {
            _verticalVelocity.y = _playerData.currentJumpForce;
            _playerData.jumpCountAvailable = _playerData.currentMaxJumpCount - 1;
            _animator.SetTrigger(JumpTrigger);
        }
        else if (_playerData.jumpCountAvailable > 0) 
        {
            _verticalVelocity.y = _playerData.currentJumpForce; 
            _playerData.jumpCountAvailable--;
            _animator.SetTrigger(JumpTrigger);
        }
    }
    
    void TryDash()
    {
        if (_playerData == null) return;

        // 대쉬 중이 아니고 쿨다운이 다 되었을 때
        if (!_playerData.isDashing && _playerData.dashCooldownTimer <= 0)
        {
            if (_dashCoroutine != null) StopCoroutine(_dashCoroutine);
            _dashCoroutine = StartCoroutine(DashCoroutine());
        }
    }

    private IEnumerator DashCoroutine()
    {
        if (_playerData == null) yield break;

        _playerData.isDashing = true;
        _animator.SetBool(IsDashing, true);
        _playerData.dashCooldownTimer = _playerData.currentDashCooldown; // 쿨다운 시작
        _currentSpeed = 0; // 대쉬 시작 시 현재 이동 속도 초기화

        // 대쉬 중 무적 처리
        if (_playerData.currentDashInvincibleDuration > 0)
        {
            if (_invincibilityCoroutine != null) StopCoroutine(_invincibilityCoroutine);
            _invincibilityCoroutine = StartCoroutine(InvincibilityCoroutine(_playerData.currentDashInvincibleDuration));
        }

        float startTime = Time.time;
        // 대쉬 방향: 현재 이동 입력 방향 우선, 없으면 현재 캐릭터가 바라보는 방향
        Vector3 dashDirection = _worldTargetDirection.sqrMagnitude > 0.01f ? _worldTargetDirection.normalized : transform.forward.normalized;
        if (dashDirection.sqrMagnitude < 0.01f) // 만약 transform.forward도 (0,0,0)이면 (거의 없을 일) 기본 전방
        {
            dashDirection = Vector3.forward;
        }


        while (Time.time < startTime + _playerData.currentDashDuration)
        {
            // 대쉬 힘과 방향으로 이동 (중력 무시)
            _characterController.Move(dashDirection * (_playerData.currentDashForce * Time.deltaTime));
            yield return null; // 다음 프레임까지 대기
        }

        _playerData.isDashing = false;
        if(_animator != null) _animator.SetBool(IsDashing, false);
        _dashCoroutine = null; // 코루틴 참조 해제
    }

    void UpdateAnimations()
    {
        if (_animator == null || _playerData == null) return;

        if (_playerData.isDead)
        {
            _animator.SetBool(IsDead, true);
            return;
        }
        _animator.SetFloat(Speed, _currentSpeed);
        _animator.SetBool(Grounded, _characterController.isGrounded);
        _animator.SetBool(IsDashing, _playerData.isDashing);
        _animator.SetBool(Aiming, IsAiming);
    }

    private IEnumerator InvincibilityCoroutine(float duration)
    {
        if (_playerData == null) yield break;

        _playerData.isInvincible = true;
        yield return new WaitForSeconds(duration);
        _playerData.isInvincible = false;
        _invincibilityCoroutine = null; // 코루틴 참조 해제
    }

    // 외부에서 플레이어 속도 초기화 시 호출 (예: 스테이지 변경 후 플레이어 위치 재설정 시)
    public void ResetVelocity()
    {
        _currentVelocity = Vector3.zero;
        _verticalVelocity = Vector3.zero;
        _currentSpeed = 0f;
    }
    
    /// <summary>
    /// 락온 타겟을 업데이트하는 주 로직 (Sticky 타겟 포함)
    /// </summary>
    void UpdateLockOnTargetLogic()
    {
        _lockedAimAssistTarget = null;
        if (!IsAiming || _aimingDirection.sqrMagnitude < 0.01f)
        {
            return;
        }
        
        Vector3 playerCharacterCenter = transform.position + _characterController.center;
        Collider[] hitColliders = Physics.OverlapSphere(playerCharacterCenter, aimAssistRadius, aimAssistLayerMask);
        
        Transform bestTargetOverall = null;
        float minHorizontalAngleOverall = horizontalAimConeAngle + 1.0f;

        List<Transform> candidatesOnReticle = new List<Transform>();

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == this.transform) continue; // 자기 자신 제외

            Vector3 targetCenter = hitCollider.bounds.center;
            Vector3 directionToTarget = targetCenter - playerCharacterCenter;
            
            if (directionToTarget.sqrMagnitude < 0.001f) continue;

            Vector3 vectorToTargetHorizontal = directionToTarget;
            vectorToTargetHorizontal.y = 0;
            float horizontalAngle;

            if (vectorToTargetHorizontal.sqrMagnitude < 0.001f)
            {
                horizontalAngle = Vector3.Angle(_aimingDirection.normalized, Vector3.ProjectOnPlane(directionToTarget, Vector3.up).normalized);
                if (Vector3.ProjectOnPlane(directionToTarget, Vector3.up).sqrMagnitude < 0.001f) horizontalAngle = 0;
            }
            else
            {
                horizontalAngle = Vector3.Angle(_aimingDirection.normalized, vectorToTargetHorizontal.normalized);
            }

            if (horizontalAngle <= horizontalAimConeAngle) // 설정된 임계 각도 내에 있고, 이전에 찾은 것보다 더 가까운 각도일 때
            {
                float verticalAngle = Vector3.Angle(vectorToTargetHorizontal.normalized, directionToTarget.normalized);
                // Vector3.Angle은 항상 0~180 사이의 양수 각도를 반환
                // 여기서는 수평면으로부터의 절대적인 각도만 중요
                if (verticalAngle <= verticalAimConeAngle) // 수직 각도 조건 만족 시
                {
                    if (HasLineOfSightToTarget(hitCollider.transform)) 
                    {
                        // 수평 각도가 onReticleHorizontalAngle 이내면 거리 우선 후보로 추가
                        if (horizontalAngle <= onReticleHorizontalAngle)
                        {
                            candidatesOnReticle.Add(hitCollider.transform);
                        }
                        // 전체 범위 내에서 가장 수평적으로 정렬된 타겟도 계속 추적
                        if (horizontalAngle < minHorizontalAngleOverall)
                        {
                            minHorizontalAngleOverall = horizontalAngle;
                            bestTargetOverall = hitCollider.transform;
                        }
                    }
                }
            }
        }
        // 최종 타겟 결정
        if (candidatesOnReticle.Count > 0)
        {
            // 조준선에 매우 가까운 타겟들이 있다면, 그 중에서 가장 가까운 타겟 선택
            float closestDistanceSqr = float.MaxValue;
            foreach (Transform candidate in candidatesOnReticle)
            {
                float distSqr = (candidate.position - playerCharacterCenter).sqrMagnitude; 
                if (distSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distSqr;
                    _lockedAimAssistTarget = candidate;
                }
            }
        }
        else if (bestTargetOverall != null)
        {
            _lockedAimAssistTarget = bestTargetOverall;
        }
    }
    

    /// <summary>
    /// 특정 타겟까지의 시야가 확보되었는지 (장애물에 가리지 않았는지) 확인
    /// </summary>
    bool HasLineOfSightToTarget(Transform target)
    {
        if (target == null) return false;

        // 캐릭터의 눈높이 설정
        Vector3 rayStartPoint;

        // WeaponController와 firePoint가 유효한지 확인
        if (_weaponController != null && _weaponController.firePoint != null)
        {
            rayStartPoint = _weaponController.firePoint.position;
        }
        else
        {
            if(_characterController != null) // 풀백 케릭터 컨트롤러 확인
            {
                Debug.LogWarning($"[{gameObject.name}] WeaponController 또는 firePoint를 찾을 수 없음 캐릭터 머리 기준으로 대체.");
                float headHeightRatio = 0.45f; 
                rayStartPoint = (transform.position + _characterController.center) + Vector3.up * (_characterController.height * headHeightRatio);
            }
            else 
            {
                Debug.LogError($"[{gameObject.name}] CharacterController를 찾을 수 없음");
                return false;
            }
        }
        // 타겟의 콜라이더 중심으로 조준
        Vector3 targetPoint = target.position;
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            targetPoint = targetCollider.bounds.center;
        }
        
        Vector3 directionToTarget = targetPoint - rayStartPoint;
        float distanceToTarget = directionToTarget.magnitude;
        
        // 매우 가까운 타겟은 항상 시야가 확보된 것으로 간주
        if (distanceToTarget < aimAssistRadius * 0.2f)
        {
            return true;
        }
        
        // Raycast 시 자기 자신(Player)이나 타겟 자신은 무시하거나, obstacleLayerMask에만 충돌하도록 설정
        if (Physics.Raycast(rayStartPoint, directionToTarget.normalized, distanceToTarget, obstacleLayerMask))
        {
            return false; // 레이캐스트가 obstacleLayerMask에 설정된 물체라면 시야가 막힌 것임
        }
        
        return true; // 아무 장애물에도 맞지 않으면 시야 확보
    }

    /// <summary>
    /// 넉백 적용 메서드 (보스 스킬 피드백) 
    /// </summary>
    /// <param name="direction">밀려난 방향</param>
    /// <param name="force">미는 힘</param>
    /// <param name="duration">적용 시간</param>
    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (_knockbackCoroutine != null)
        {
            StopCoroutine( _knockbackCoroutine );
        }
        _knockbackCoroutine = StartCoroutine(KnockbackCoroutine(direction, force, duration));
    }
    // 넉백 코루틴
    private IEnumerator KnockbackCoroutine(Vector3 direction, float force, float duration)
    {
        _isBeingKnockedBack = true;

        float timer = 0;
        while (timer < duration)
        {
            float currentForce = Mathf.Lerp(force, 0, timer / duration);
            _characterController.Move(direction * currentForce * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }
        _isBeingKnockedBack = false;
        _knockbackCoroutine = null;
    }
    public Transform GetLockedTarget()
    {
        return _lockedAimAssistTarget;
    }
    
    /// <summary>
    /// 모바일 조작 위한 ui버튼 설정 메서드들
    /// </summary>
    public void OnJumpButtonPressed()
    {
        if (_playerData == null || _playerData.isDead) return;
        Debug.Log("Jump Button Pressed"); // 버튼 클릭 로그 확인용
        TryJump();
    }
    public void OnDashButtonPressed()
    {
        if (_playerData == null || _playerData.isDead) return;
        Debug.Log("Dash Button Pressed"); // 버튼 클릭 로그 확인용
        TryDash();
    }
    /// <summary>
    /// 디버그용 (에임 어시스트 범위 확인)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (_characterController == null && Application.isPlaying) {
             _characterController = GetComponent<CharacterController>(); 
        }
        if (_characterController == null && !Application.isPlaying) { 
             _characterController = GetComponent<CharacterController>();
             if(_characterController == null && GetComponent<PlayerData>() == null) return;
        }
        
        Vector3 gizmoOrigin = transform.position;
        if (_characterController != null) 
        {
             gizmoOrigin = transform.position + _characterController.center;
        }

        Gizmos.color = new Color(0, 0.8f, 0, 0.10f); 
        Gizmos.DrawWireSphere(gizmoOrigin, aimAssistRadius);

        if (IsAiming && _aimingDirection.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.blue; 
            Gizmos.DrawRay(gizmoOrigin, _aimingDirection.normalized * aimAssistRadius);

            // 수평 에임 어시스트 각도(cone) 시각화
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.2f); 
            Vector3 leftRayOverallH = Quaternion.Euler(0, -horizontalAimConeAngle, 0) * _aimingDirection.normalized;
            Vector3 rightRayOverallH = Quaternion.Euler(0, horizontalAimConeAngle, 0) * _aimingDirection.normalized;
            Gizmos.DrawRay(gizmoOrigin, leftRayOverallH * (aimAssistRadius * 0.9f)); 
            Gizmos.DrawRay(gizmoOrigin, rightRayOverallH * (aimAssistRadius * 0.9f));
            Gizmos.DrawLine(gizmoOrigin + leftRayOverallH * (aimAssistRadius * 0.9f), gizmoOrigin + rightRayOverallH * (aimAssistRadius * 0.9f));

            // 수평 조준선 근처 우선 타겟팅 각도(cone) 시각화
            Gizmos.color = new Color(0, 1f, 1f, 0.3f); 
            Vector3 leftRayOnReticleH = Quaternion.Euler(0, -onReticleHorizontalAngle, 0) * _aimingDirection.normalized;
            Vector3 rightRayOnReticleH = Quaternion.Euler(0, onReticleHorizontalAngle, 0) * _aimingDirection.normalized;
            Gizmos.DrawRay(gizmoOrigin, leftRayOnReticleH * aimAssistRadius); 
            Gizmos.DrawRay(gizmoOrigin, rightRayOnReticleH * aimAssistRadius);
            Gizmos.DrawLine(gizmoOrigin + leftRayOnReticleH * aimAssistRadius, gizmoOrigin + rightRayOnReticleH * aimAssistRadius);

            // (참고: verticalAimConeAngle을 3D로 정확히 시각화하는 것은 더 복잡합니다.
            // 위 기즈모는 수평 범위만 보여주며, 실제로는 이 수평 범위 내에서 수직으로도 verticalAimConeAngle 만큼의 허용치가 더해집니다.)
        }
        
        if (_lockedAimAssistTarget != null)
        {
            Gizmos.color = Color.red;
            Vector3 targetDisplayPoint = _lockedAimAssistTarget.position;
            Collider targetCol = _lockedAimAssistTarget.GetComponent<Collider>();
            if(targetCol != null) targetDisplayPoint = targetCol.bounds.center;

            Vector3 startLinePoint = gizmoOrigin; 
            if (_weaponController != null && _weaponController.firePoint != null) {
                 startLinePoint = _weaponController.firePoint.position; 
            }
            Gizmos.DrawLine(startLinePoint, targetDisplayPoint);
        }
    }
}