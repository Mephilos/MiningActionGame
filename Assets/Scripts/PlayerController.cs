using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
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
        if (_playerData == null || _playerData.isDead)
        {
            return;
        }
    
        HandleInput();
        // HandleRotation(); // 회전 로직은 매 프레임 호출
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
        if (IsAiming && _applyAimRotationInFixedUpdate)
        {
            // _aimingDirection은 HandleInput (Update)에서 계산됨
            if (_aimingDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(_aimingDirection, Vector3.up);
            }
        }
        else if (!IsAiming) // 조준 중이 아닐 때
        {
            // _worldTargetDirection은 HandleInput (Update)에서 계산됨
            if (_worldTargetDirection.sqrMagnitude > 0.01f)
            {
                Vector3 lookDir = _worldTargetDirection;
                lookDir.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                // Time.fixedDeltaTime을 사용하여 회전 속도 조절
                float rotationStep = _playerData.currentRotationSpeed * Time.fixedDeltaTime;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);
            }
        }

        _applyAimRotationInFixedUpdate = false; // 사용 후 플래그 리셋
    }

    void HandleMovement()
    {
        float actualMaxSpeed = _playerData.currentMaxSpeed;
        float actualAcceleration = _playerData.currentAcceleration;


        if (_shiftKeyHeld && (Time.time - _shiftKeyDownTime >= shortPressThreshold) && !_playerData.isDashing)
        {
            actualMaxSpeed *= _playerData.currentBoostFactor;
            actualAcceleration *= _playerData.currentBoostFactor;
        }


        if (_worldTargetDirection.sqrMagnitude > 0.01f || IsAiming)
        {
            _currentSpeed += actualAcceleration * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Clamp(_currentSpeed, 0, actualMaxSpeed);
        }
        else if (!IsAiming) 
        { 
            // 조준 중이 아니고 이동 입력도 없을 때만 감속
            _currentSpeed -= _playerData.currentDeceleration * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Max(0, _currentSpeed);
        }
        else 
        {
            _currentSpeed -= _playerData.currentDeceleration * Time.fixedDeltaTime; 
            _currentSpeed = Mathf.Max(0, _currentSpeed); 
        }
        _currentVelocity = (IsAiming && _worldTargetDirection.sqrMagnitude < 0.01f) 
            ? transform.forward * _currentSpeed : _worldTargetDirection * _currentSpeed;
        
        if (IsAiming && _worldTargetDirection.sqrMagnitude < 0.01f && _aimingDirection.sqrMagnitude < 0.01f) // 조준 중이고 키보드 이동 입력이 없을 때
        {
            _currentVelocity = transform.forward * _currentSpeed; 
        } 
        else 
        {
            _currentVelocity = _worldTargetDirection * _currentSpeed;
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
            
            if (currentPosition.x != clampedX || currentPosition.z != clampedZ)
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
            _animator.SetBool("IsJumping", true);
        }
        else if (_playerData.jumpCountAvailable > 0) 
        {
            _verticalVelocity.y = _playerData.currentJumpForce; 
            _playerData.jumpCountAvailable--;
            _animator.SetBool("IsJumping", true);
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
        _animator.SetBool("IsDashing", true);
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
        _animator.SetBool("IsDashing", false);
        _dashCoroutine = null; // 코루틴 참조 해제
    }

    void UpdateAnimations()
    {
        if (_animator == null || _playerData == null) return;

        if (_playerData.isDead)
        {
            _animator.SetBool("IsDead", true);
            return;
        }
        
        Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z);
        float actualSpeed = horizontalVelocity.magnitude;
        _animator.SetFloat("Speed", actualSpeed);
        
        _animator.SetBool("Grounded", _characterController.isGrounded);

        if (!_characterController.isGrounded && _verticalVelocity.y > 0.1f)
        {
            _animator.SetBool("IsJumping", true);
        }
        else if (_characterController.isGrounded)
        {
            _animator.SetBool("IsJumping", false);
        }
        
        _animator.SetBool("IsDashing", _playerData.isDashing);
        _animator.SetBool("IsAiming", IsAiming);
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
}








// void HandleRotation()
    // {
    //     if (IsAiming)
    //     {
    //         if (Camera.main != null)
    //         {
    //             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //             Debug.DrawRay(ray.origin, ray.direction * 100f, Color.cyan);
    //             
    //             Plane groundPlane = new Plane(Vector3.up, transform.position); 
    //             
    //             Debug.Log($"[PlaneDebug] Player Y for plane: {transform.position.y}. Plane defined with normal: {groundPlane.normal}, distance: {groundPlane.distance}");
    //
    //
    //             if (groundPlane.Raycast(ray, out float rayDistance))
    //             {
    //                 Vector3 targetPoint = ray.GetPoint(rayDistance);
    //                 Debug.DrawLine(transform.position, targetPoint, Color.magenta);
    //
    //                 
    //                 Debug.Log($"[AimingCheck] TargetPoint: {targetPoint}, PlayerPos: {transform.position}");
    //
    //                 Vector3 directionToTarget = targetPoint - transform.position;
    //             
    //                 Debug.Log($"[Aiming] MousePos: {Input.mousePosition}, RayDistance: {rayDistance}, RawDirectionToTarget: {directionToTarget}");
    //
    //
    //                 if (directionToTarget.sqrMagnitude > 0.01f)
    //                 {
    //                     directionToTarget.y = 0;
    //                     Debug.Log($"[Aiming] FlattenedDirectionToTarget: {directionToTarget}");
    //                     transform.rotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
    //                     Debug.Log($"[Aiming] New Rotation Euler: {transform.rotation.eulerAngles}");
    //                 }
    //                 else
    //                 {
    //                     Debug.LogWarning("[Aiming] directionToTarget is too small. No rotation applied.");
    //                 }
    //             }
    //             else
    //             {
    //                 Debug.LogWarning($"[Aiming] Raycast to ground plane failed. Mouse Position: {Input.mousePosition}. Character will hold current rotation.");
    //             }
    //         }
    //     }
    //     else // 조준 중이 아닐 때
    //     {
    //         if (_worldTargetDirection.sqrMagnitude > 0.01f)
    //         {
    //             Vector3 lookDir = _worldTargetDirection;
    //             lookDir.y = 0;
    //             Quaternion targetRotation = Quaternion.LookRotation(lookDir);
    //             float rotationStep = _playerData.currentRotationSpeed * Time.deltaTime;
    //             transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);
    //         }
    //     }
    // }