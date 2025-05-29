using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Grounded = Animator.StringToHash("Grounded");
    private static readonly int IsDead = Animator.StringToHash("IsDead");
    private static readonly int IsDashing = Animator.StringToHash("IsDashing");
    private static readonly int Aiming = Animator.StringToHash("IsAiming");
    private static readonly int JumpTrigger = Animator.StringToHash("JumpTrigger");
    
    [SerializeField] private float isometricCameraAngleY = 45f;
    [SerializeField] private float shortPressThreshold = 0.2f;

    private CharacterController _characterController;
    private PlayerData _playerData;
    private Animator _animator;

    private Vector3 _worldTargetDirection = Vector3.forward;
    private Vector3 _currentVelocity = Vector3.zero;
    private float _currentSpeed = 0;
    private Vector3 _verticalVelocity = Vector3.zero;

    private float _shiftKeyDownTime = 0f;
    private bool _shiftKeyHeld = false;

    private Coroutine _dashCoroutine;
    private Coroutine _invincibilityCoroutine;

    public bool IsAiming { get; private set; }

    private Vector3 _aimingDirection;
    private bool _applyAimRotationInFixedUpdate = false;
    
    [Header("Mobile Controls")]
    public Joystick movementJoystick;

    
    [Header("Aim Assist Settings")]
    public float aimAssistRadius = 10f; // 에임 어시스트 감지 반경
    public float aimAssistAngleThreshold = 15f; // 플레이어 정면 기준 각도
    public LayerMask aimAssistLayerMask; // 에임 어시스트 적용 레이어 마스크 
    public float aimAssistRotationSpeed = 10f; // 타겟 대상 회전 속도
    public float loseTargetAngleThreshold = 30f; // 락온 해제 각도 (aimAssistAngleThreshold보다 커야 함)
    public float loseTargetRadiusFactor = 1.2f;  // 락온 해제 거리 계수 (aimAssistRadius * 이 값보다 멀어지면 해제)
    public LayerMask obstacleLayerMask; // 장애물 판별용 레이어 마스크 
    private Transform _lockedAimAssistTarget = null; // 현재 감지된 에임 어시스트 타겟
    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerData = GetComponent<PlayerData>();
        _animator = GetComponent<Animator>();

        if (_playerData == null)
        {
            Debug.LogError("플레이어 데이터 컴포넌트가 설정되지 않음");
            enabled = false;
            return;
        }

        if (_animator == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 플레이어에 Animator가 없음");
        }

        // 초기 바라보는 방향 설정
        _worldTargetDirection = Quaternion.Euler(0, isometricCameraAngleY, 0) * Vector3.forward;
        transform.rotation = Quaternion.LookRotation(_worldTargetDirection);
    }

    void Start()
    {
        if (_playerData != null)
        {
            _playerData.jumpCountAvailable = _playerData.currentMaxJumpCount;
        }
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

        if (!_playerData.isDead)
        {
            _animator.SetBool(IsDead, false);
        }
        HandleInput();

        if (IsAiming)
        {
            UpdateLockedTargetLogic();
        }
        else
        {
            _lockedAimAssistTarget = null;
        }
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (_playerData == null || _playerData.isDead) return;
        if (_playerData.isDashing)
        {
            ApplyFinalMovement();
            return; // 대쉬 중에는 이동 로직 건너뜀
        }
        HandleMovement();
        ApplyRotationFixedUpdate();
        ApplyGravity();
        ApplyFinalMovement();
    }

    void HandleInput()
    {
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
        _applyAimRotationInFixedUpdate = false;

        if (IsAiming)
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // 마우스 위치는 Update에서 샘플링하는 것이 좋음
                Plane groundPlane = new Plane(Vector3.up, transform.position);

                if (groundPlane.Raycast(ray, out float rayDistance))
                {
                    Vector3 targetPoint = ray.GetPoint(rayDistance);
                    Vector3 directionToTarget = targetPoint - transform.position;

                    if (directionToTarget.sqrMagnitude > 0.01f)
                    {
                        directionToTarget.y = 0;
                        _aimingDirection = directionToTarget.normalized; // 정규화된 조준 방향 저장
                        _applyAimRotationInFixedUpdate = true; // FixedUpdate에서 이 방향을 사용하도록 플래그 설정
                    }
                }
            }
        }
#endif
    }

    void ApplyRotationFixedUpdate()
    {
        Vector3 finalAimDirToRotate = transform.forward; // 기본값은 현재 바라보는 방향 유지

    if (IsAiming)
    {
        if (_lockedAimAssistTarget != null)
        {
            // 에임 어시스트 타겟이 있으면 그쪽을 향하도록 설정
            finalAimDirToRotate = (_lockedAimAssistTarget.position - transform.position);
            finalAimDirToRotate.y = 0; // 수평 회전만
            if (finalAimDirToRotate.sqrMagnitude < 0.001f) // 매우 가까우면 방향 벡터가 0이 될 수 있으므로 방지
            {
                finalAimDirToRotate = transform.forward;
            }
            else
            {
                finalAimDirToRotate.Normalize();
            }
        }
        else if (_applyAimRotationInFixedUpdate && _aimingDirection.sqrMagnitude > 0.01f)
        {
            finalAimDirToRotate = _aimingDirection;
        }
        // (선택적) 만약 위 두 경우가 아니고 그냥 조준 버튼만 누르고 있다면 (마우스 입력X, 어시스트 타겟X)
        // finalAimDirToRotate는 초기값인 transform.forward를 유지하거나,
        // 혹은 _worldTargetDirection (이동 방향)을 사용하도록 할 수도 있습니다.
        // 현재 로직은 transform.forward를 유지하는 방향으로 되어 있습니다.

        // 최종 계산된 방향으로 회전
        if (finalAimDirToRotate.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(finalAimDirToRotate, Vector3.up);
            // 에임 어시스트 중일 때는 더 빠른 회전 속도를, 아니면 플레이어 기본 회전 속도를 사용
            float currentEffectiveRotationSpeed = (_lockedAimAssistTarget != null) ? aimAssistRotationSpeed : _playerData.currentRotationSpeed;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, currentEffectiveRotationSpeed * Time.fixedDeltaTime);
        }
    }
    else // 조준 중이 아닐 때 (기존 이동 회전 로직)
    {
        if (_worldTargetDirection.sqrMagnitude > 0.01f)
        {
            Vector3 lookDir = _worldTargetDirection;
            lookDir.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _playerData.currentRotationSpeed * Time.fixedDeltaTime);
        }
    }
    _applyAimRotationInFixedUpdate = false; // 플래그 리셋
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

        if (IsAiming && !isMovingInput)
        {
            _currentVelocity = transform.forward * _currentSpeed;
        }
        else
        { 
            _currentVelocity = isMovingInput ? _worldTargetDirection.normalized * _currentSpeed : Vector3.zero;
        }
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
        _animator.SetBool(IsDashing, false);
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
    void UpdateLockedTargetLogic()
    {
        // 1. 현재 락온된 타겟이 유효한지 확인 (Sticky 로직)
        if (_lockedAimAssistTarget != null)
        {
            if (!IsTargetStillValid(_lockedAimAssistTarget))
            {
                _lockedAimAssistTarget = null; // 유효하지 않으면 락온 해제
            }
            else
            {
                return; // 현재 락온 타겟이 유효하면, 새로운 타겟을 찾지 않고 유지
            }
        }

        // 2. 락온된 타겟이 없거나 해제되었다면, 새로운 타겟 검색
        // (장애물에 가로막히지 않은 적 중 플레이어에게 가장 가까운 적 우선)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, aimAssistRadius, aimAssistLayerMask);
        Transform potentialNewTarget = null;
        float closestDistanceSqr = Mathf.Infinity;

        Vector3 playerAimingDirection = transform.forward; // 캐릭터의 현재 정면을 기준으로 각도 계산

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == this.transform) continue; // 자기 자신 제외

            Vector3 directionToPotentialTarget = (hitCollider.transform.position - transform.position);
            float distanceSqr = directionToPotentialTarget.sqrMagnitude;
            
            // 거리에 따라 각도 임계값 조정 (가까울수록 더 큰 각도 허용)
            float distance = Mathf.Sqrt(distanceSqr);
            float adjustedAngleThreshold = aimAssistAngleThreshold;
            
            // 가까운 타겟에 대해 각도 제한 완화
            if (distance < aimAssistRadius * 0.5f)
            {
                // 거리가 가까울수록 허용 각도 증가 (최대 2배까지)
                float distanceFactor = distance / (aimAssistRadius * 0.5f); // 0~1 사이 값
                adjustedAngleThreshold = Mathf.Lerp(aimAssistAngleThreshold * 2, aimAssistAngleThreshold, distanceFactor);
            }
            
            // Y축 차이를 무시하여 수평 방향만 고려 (가까운 적 락온에 유리)
            Vector3 horizontalDirection = directionToPotentialTarget;
            horizontalDirection.y = 0;
            
            float angleToTarget = Vector3.Angle(playerAimingDirection, horizontalDirection.normalized);

            if (angleToTarget < adjustedAngleThreshold) // 조정된 각도 임계값 사용
            {
                if (HasLineOfSightToTarget(hitCollider.transform)) // 시야 확보(장애물 없는지) 확인
                {
                    if (distanceSqr < closestDistanceSqr) // 가장 가까운 타겟인지 확인
                    {
                        closestDistanceSqr = distanceSqr;
                        potentialNewTarget = hitCollider.transform;
                    }
                }
            }
        }
        
        _lockedAimAssistTarget = potentialNewTarget; // 가장 적합한 타겟으로 락온 (또는 null)
    }

    /// <summary>
    /// 현재 락온된 타겟이 여전히 유효한지(Sticky 조건을 만족하는지) 검사합니다.
    /// </summary>
    bool IsTargetStillValid(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        // 거리 검사 (더 넓은 해제 반경 사용)
        if (Vector3.Distance(transform.position, target.position) > aimAssistRadius * loseTargetRadiusFactor) //
        {
            return false;
        }

        // 각도 검사 (더 넓은 해제 각도 사용)
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        // directionToTarget.y = 0; // 수평 각도만 고려하려면 주석 해제
        if (Vector3.Angle(transform.forward, directionToTarget) > loseTargetAngleThreshold) //
        {
            return false;
        }

        // 장애물(시야) 검사
        if (!HasLineOfSightToTarget(target))
        {
            return false;
        }

        return true; // 모든 조건을 만족하면 유효
    }

    /// <summary>
    /// 특정 타겟까지의 시야가 확보되었는지 (장애물에 가리지 않았는지) 확인합니다.
    /// </summary>
    bool HasLineOfSightToTarget(Transform target)
    {
        if (target == null) return false;

        // 캐릭터의 눈높이나 발사점 등 적절한 시작점 설정
        Vector3 rayStartPoint = transform.position + _characterController.center;
        
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
        if (distanceToTarget < aimAssistRadius * 0.3f)
        {
            return true;
        }
        
        // Raycast 시 자기 자신(Player)이나 타겟 자신은 무시하거나, obstacleLayerMask에만 충돌하도록 설정
        if (Physics.Raycast(rayStartPoint, directionToTarget.normalized, distanceToTarget, obstacleLayerMask))
        {
            return false; // 레이캐스트가 obstacleLayerMask에 설정된 어떤 물체에라도 맞았다면, 시야가 막힌 것임
        }
        
        return true; // 아무 장애물에도 맞지 않으면 시야 확보
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
        if (_characterController == null && Application.isPlaying) 
        { 
            return; 
        }


        // 기본 감지 반경 (에임 어시스트 시도 범위)
        Gizmos.color = new Color(0, 1, 1, 0.15f);
        if (Application.isPlaying) // 플레이 중에만 캐릭터 컨트롤러 중심 사용
             Gizmos.DrawSphere(transform.position + _characterController.center, aimAssistRadius);
        else
             Gizmos.DrawSphere(transform.position, aimAssistRadius);


        // 락온 해제 반경 (Sticky 반경)
        Gizmos.color = new Color(1, 0.92f, 0.016f, 0.10f); // 
        if (Application.isPlaying)
            Gizmos.DrawSphere(transform.position + _characterController.center, aimAssistRadius * loseTargetRadiusFactor);
        else
            Gizmos.DrawSphere(transform.position, aimAssistRadius * loseTargetRadiusFactor);


        // 감지 각도 (기본)
        Vector3 gizmoOrigin = Application.isPlaying ? transform.position + _characterController.center : transform.position;
        Vector3 forwardDir = transform.forward;

        Gizmos.color = Color.cyan;
        Vector3 leftRay = Quaternion.Euler(0, -aimAssistAngleThreshold, 0) * forwardDir;
        Vector3 rightRay = Quaternion.Euler(0, aimAssistAngleThreshold, 0) * forwardDir;
        Gizmos.DrawRay(gizmoOrigin, leftRay * aimAssistRadius);
        Gizmos.DrawRay(gizmoOrigin, rightRay * aimAssistRadius);

        // 락온 해제 각도
        Gizmos.color = Color.yellow;
        Vector3 loseLeftRay = Quaternion.Euler(0, -loseTargetAngleThreshold, 0) * forwardDir;
        Vector3 loseRightRay = Quaternion.Euler(0, loseTargetAngleThreshold, 0) * forwardDir;
        Gizmos.DrawRay(gizmoOrigin, loseLeftRay * aimAssistRadius * loseTargetRadiusFactor);
        Gizmos.DrawRay(gizmoOrigin, loseRightRay * aimAssistRadius * loseTargetRadiusFactor);

        // 현재 락온된 타겟이 있다면 선으로 연결
        if (_lockedAimAssistTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(gizmoOrigin, _lockedAimAssistTarget.position);
        }
    }
}