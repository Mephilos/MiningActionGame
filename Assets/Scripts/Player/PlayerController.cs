using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerData), typeof(Animator))]
[RequireComponent(typeof(PlayerInputHandler), typeof(AimController))]
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

    [SerializeField] private float isometricCameraAngleY = 45f;
    

    private CharacterController _characterController;
    private PlayerData _playerData;
    private Animator _animator;
    private WeaponController _weaponController;
    private PlayerInputHandler _inputManager;
    private AimController _aimController;

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

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerData = GetComponent<PlayerData>();
        _animator = GetComponent<Animator>();
        _weaponController = GetComponent<WeaponController>();
        _inputManager = GetComponent<PlayerInputHandler>();
        _aimController = GetComponent<AimController>();

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
        if (_playerData == null || _playerData.isDead)
        {
            _animator.SetBool(IsDead, _playerData.isDead);
            return;
        }

        HandleInput();
        HandleSkillInput();
        PassDataToWeaponController();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (_playerData == null || _playerData.isDead) return;
        if (_playerData.isDashing)
        {
            ApplyFinalMovement();
            return;
        }
        HandleMovement();
        ApplyRotationFixedUpdate();
        ApplyGravity();
        ApplyFinalMovement();
    }

    void HandleInput()
    {
        if (_isBeingKnockedBack) return;

        // Movement
        Vector2 moveInput = _inputManager.MoveInput;
        Vector3 rawInputDir = new Vector3(moveInput.x, 0, moveInput.y);
        if (rawInputDir.sqrMagnitude > 0.01f)
        {
            _worldTargetDirection = Quaternion.Euler(0, isometricCameraAngleY, 0) * rawInputDir.normalized;
        }
        else
        {
            _worldTargetDirection = Vector3.zero;
        }

        // Actions
        if (_inputManager.IsJumpPressed)
        {
            TryJump();
        }
        if (_inputManager.IsDashButtonPressed)
        {
            TryDash();
        }
    }

    void HandleSkillInput()
    {
        SkillData currentSkill = _weaponController.currentWeaponData.specialSkill;
        if (currentSkill == null || trajectoryPredictor == null) return;

        if (_inputManager.IsSkillButtonPressed && _playerData.currentSkillCooldown <= 0)
        {
            _isAimingSkill = true;
            _animator.SetBool(IsAimingSkill, true);
            trajectoryPredictor.Show();
        }

        if (_isAimingSkill && _inputManager.IsSkillButtonHeld)
        {
            Vector3 throwDirection = GetThrowDirection(currentSkill.throwForce);
            trajectoryPredictor.PredictTrajectory(grenadeThrowPoint.position, throwDirection);
        }

        if (_isAimingSkill && _inputManager.IsSkillButtonReleased)
        {
            _animator.SetBool(IsAimingSkill, false);
            _animator.SetTrigger(ThrowTrigger);
            _isAimingSkill = false;
            trajectoryPredictor.Hide();
        }
    }

    void PassDataToWeaponController()
    {
        if (_weaponController == null) return;

        // 조준 상태에 따라 조준 방향과 대상을 무기 컨트롤러에 전달
        if (_inputManager.IsAiming)
        {
            _weaponController.SetAimingData(_aimController.AimingDirection, _aimController.LockedAimAssistTarget);
        }
        else
        {
            // 조준하고 있지 않을 땐 캐릭터의 정면으로 발사 락온 타겟은 null로
            _weaponController.SetAimingData(transform.forward, null);
        }

        // 발사 입력 처리
        if (_weaponController.currentWeaponData != null && _weaponController.currentWeaponData.isChargeWeapon)
        {
            if (_inputManager.IsFirePressed) _weaponController.HandleChargeStart();
            if (_inputManager.IsFireReleased) _weaponController.HandleChargeRelease();
        }
        else
        {
            _weaponController.HandleFireInput(_inputManager.IsFireHeld);
        }
    }

    void ApplyRotationFixedUpdate()
    {
        Vector3 finalAimDirToRotate = transform.forward;

        if (_inputManager.IsAiming && _aimController.AimingDirection.sqrMagnitude > 0.01f)
        {
            finalAimDirToRotate = _aimController.AimingDirection;
        }
        else if (!_inputManager.IsAiming && _worldTargetDirection.sqrMagnitude > 0.01f)
        {
            finalAimDirToRotate = _worldTargetDirection;
        }

        if (finalAimDirToRotate.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(finalAimDirToRotate, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _playerData.currentRotationSpeed * Time.fixedDeltaTime);
        }
    }

    void HandleMovement()
    {
        float actualMaxSpeed = _playerData.currentMaxSpeed;
        float actualAcceleration = _playerData.currentAcceleration;
        float actualDeceleration = _playerData.currentDeceleration;
        bool isMovingInput = _worldTargetDirection.sqrMagnitude > 0.01f;

        if (isMovingInput)
        {
            if (_inputManager.IsAiming) { actualMaxSpeed *= 0.5f; }

            _currentSpeed += actualAcceleration * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Clamp(_currentSpeed, 0, actualMaxSpeed);
        }
        else
        {
            _currentSpeed -= actualDeceleration * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Max(0, _currentSpeed);
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
        _playerData.dashCooldownTimer = _playerData.currentDashCooldown;
        _currentSpeed = 0;

        if (_playerData.currentDashInvincibleDuration > 0)
        {
            if (_invincibilityCoroutine != null) StopCoroutine(_invincibilityCoroutine);
            _invincibilityCoroutine = StartCoroutine(InvincibilityCoroutine(_playerData.currentDashInvincibleDuration));
        }

        float startTime = Time.time;
        Vector3 dashDirection = _worldTargetDirection.sqrMagnitude > 0.01f ? _worldTargetDirection.normalized : transform.forward.normalized;
        if (dashDirection.sqrMagnitude < 0.01f)
        {
            dashDirection = Vector3.forward;
        }

        while (Time.time < startTime + _playerData.currentDashDuration)
        {
            _characterController.Move(dashDirection * (_playerData.currentDashForce * Time.deltaTime));
            yield return null;
        }

        _playerData.isDashing = false;
        if (_animator != null) _animator.SetBool(IsDashing, false);
        _dashCoroutine = null;
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
        _animator.SetBool(Aiming, _inputManager.IsAiming);
    }

    private IEnumerator InvincibilityCoroutine(float duration)
    {
        if (_playerData == null) yield break;
        _playerData.isInvincible = true;
        yield return new WaitForSeconds(duration);
        _playerData.isInvincible = false;
        _invincibilityCoroutine = null;
    }

    public void ResetVelocity()
    {
        _currentVelocity = Vector3.zero;
        _verticalVelocity = Vector3.zero;
        _currentSpeed = 0f;
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (_knockbackCoroutine != null)
        {
            StopCoroutine(_knockbackCoroutine);
        }
        _knockbackCoroutine = StartCoroutine(KnockbackCoroutine(direction, force, duration));
    }

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
    
    public void ExecuteGrenadeThrow()
    {
        SkillData currentSkill = _weaponController.currentWeaponData.specialSkill;
        if (currentSkill == null) return;
        
        ThrowSkillGrenade(currentSkill);
        _playerData.currentSkillCooldown = currentSkill.cooldown;
    }
    
    private Vector3 GetThrowDirection(float force)
    {
        Ray ray = Camera.main.ScreenPointToRay(_inputManager.MousePosition);
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
            rb.linearVelocity = throwDirection;
        }
    }
}
