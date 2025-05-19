//using System.Numerics;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // public float moveSpeed = 5f;
    // public float jumpForce = 8f;
    // public float gravity = -20f;

    [SerializeField] private CameraFollow cameraFollow; // 카메라 기준 방향
    private CharacterController controller;
    private PlayerData playerData;
    private Vector3 inputDir;
    private Vector3 velocity;

    private Coroutine dashCoroutine;
    private Coroutine invincibilityCoroutine;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerData = GetComponent<PlayerData>();

        if (cameraFollow == null)
        {
            Debug.LogError("카메라가 플레이어 컨트롤러에 설정되지 않음");
            enabled = false;
            return;
        }
        if (playerData == null)
        {
            Debug.LogError("플레이어 데이터 컴포넌트가 설정되지 않음");
            enabled = false;
            return;
        }
    }
    void Start()
    {
        if (playerData != null)
        {
            playerData.jumpCountAvailable = playerData.currentMaxJumpCount;
        }

    }

    void Update()
    {
        CheckGroundedAndResetStates();
        HandleJumpInput();
        HandleDashInput();
        RotateTowardsAimDirection();

        if (playerData.dashCooldownTimer > 0)
        {
            playerData.dashCooldownTimer -= Time.deltaTime;
        }
    }
    void FixedUpdate()
    {
        HandleMovement();
    }

    /// <summary>
    /// 플레이어 이동 처리 (좌우, 전후)
    /// </summary>
    void HandleMovement()
    {
        if (playerData == null) return;
        if (playerData.isDashing) return;

        // 입력만 받아놓고 방향은 카메라 기준으로 변환
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // 카메라 기준 방향
        Vector3 camForward = cameraFollow.forward;
        Vector3 camRight = cameraFollow.right;

        inputDir = (camForward * moveZ + camRight * moveX).normalized;

        Vector3 horizontalVelocity = inputDir * playerData.currentMoveSpeed;

        float currentGravity = -20f;
        //중력 구현
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 바닥에 눌러붙게
        }
        velocity.y += currentGravity * Time.fixedDeltaTime;

        //월드 경계 처리
        Vector3 currentPosition = transform.position;
        Vector3 intendedDeltaMovement = (horizontalVelocity + Vector3.up * velocity.y) * Time.fixedDeltaTime;
        Vector3 targetPosition = currentPosition + intendedDeltaMovement;

        if(ChunkManager.Instance != null && ChunkManager.Instance.useWorldBorder)
        {
            float playerRadius = controller.radius;

            float worldMinX = ChunkManager.Instance.minChunkX * ChunkManager.Instance.chunkSize + playerRadius;
            float worldMaxX = (ChunkManager.Instance.maxChunkX + 1) * ChunkManager.Instance.chunkSize - playerRadius;
            float worldMinZ = ChunkManager.Instance.minChunkZ * ChunkManager.Instance.chunkSize + playerRadius;
            float worldMaxZ = (ChunkManager.Instance.maxChunkZ + 1) * ChunkManager.Instance.chunkSize - playerRadius;

            // 목표 X 위치가 경계를 벗어나면 경계값으로 강제 설정
            if (targetPosition.x < worldMinX) targetPosition.x = worldMinX;
            if (targetPosition.x > worldMaxX) targetPosition.x = worldMaxX;

            // 목표 Z 위치가 경계를 벗어나면 경계값으로 강제 설정
            if (targetPosition.z < worldMinZ) targetPosition.z = worldMinZ;
            if (targetPosition.z > worldMaxZ) targetPosition.z = worldMaxZ;
        }
    
        Vector3 actualMovement = targetPosition - currentPosition;

        controller.Move(actualMovement);

        //조준 모드일경우 이동방향 회전 off
        if (inputDir.sqrMagnitude > 0.01f && !Input.GetMouseButton(1))
        {       
            Quaternion targetRotation = Quaternion.LookRotation(inputDir);
            transform.rotation = targetRotation;
        }
    }
    /// <summary>
    /// 착지여부 확인, 대쉬 상태 초기화
    /// </summary>
    void CheckGroundedAndResetStates()
    {
        if (playerData == null) return;

        if (controller.isGrounded)
        {
            if (playerData.jumpCountAvailable < playerData.currentMaxJumpCount)
            {
                playerData.jumpCountAvailable = playerData.currentMaxJumpCount;
            }
        }

        Debug.DrawRay(transform.position, Vector3.down * 0.2f, controller.isGrounded ? Color.green : Color.red);
    }

    /// <summary>
    /// 점프 (바닥에 있을 때만)
    /// </summary>
    void HandleJumpInput()
    {
        if (playerData == null) return;

        if (Input.GetKeyDown(KeyCode.Space) && playerData.jumpCountAvailable > 0)
        {
            velocity.y = playerData.currentJumpForce;
            playerData.jumpCountAvailable--;

            Debug.Log($"점프 상태: { playerData.jumpCountAvailable}/{playerData.currentMaxJumpCount}");
        }
    }

    /// <summary>
    /// 대쉬 처리
    /// </summary>
    void HandleDashInput()
    {
        if (playerData == null) return;

        // 왼쪽 Shift 키를 눌렀고, 현재 대쉬 중이 아니고, 대쉬 쿨타임이 다 되었을 때
        if (Input.GetKeyDown(KeyCode.LeftShift) && !playerData.isDashing && playerData.dashCooldownTimer <= 0)
        {
            if (dashCoroutine != null) // 만약 이전 대쉬 코루틴이 (오류로) 남아있다면 중지
            {
                StopCoroutine(dashCoroutine);
            }
            dashCoroutine = StartCoroutine(DashCoroutine());
        }
    }

    private IEnumerator DashCoroutine()
    {
        if (playerData == null) yield break;

        playerData.isDashing = true;
        playerData.dashCooldownTimer = playerData.currentDashCooldown;

        if (playerData.currentDashInvincibleDuration > 0)
        {
            if (invincibilityCoroutine != null) StopCoroutine(invincibilityCoroutine);
            invincibilityCoroutine = StartCoroutine(InvincibilityCoroutine(playerData.currentDashInvincibleDuration));
        }
        float startTime = Time.time;
        Vector3 dashDirection = inputDir.sqrMagnitude > 0.01f ? inputDir : transform.forward;

        while (Time.time < startTime + playerData.currentDashDuration)
        {
            //대쉬중에는 중력 영향 X (록맨X처럼 공중대쉬 가능)
            velocity.y = 0f;
            controller.Move(dashDirection * playerData.currentDashForce * Time.deltaTime);
            yield return null;
        }

        playerData.isDashing = false;
        dashCoroutine = null;
    }
    /// <summary>
    /// 무적상태 부여 코루틴
    /// </summary>
    /// <param name="duration"></param>
    /// <returns></returns>
    private IEnumerator InvincibilityCoroutine(float duration)
    {
        if (playerData == null) yield break;

        playerData.isInvincible = true;
        Debug.Log("대쉬 무적");
        yield return new WaitForSeconds(duration);
        playerData.isInvincible = false;
        Debug.Log("대쉬 무적 종료");
        invincibilityCoroutine = null;
    }

    /// <summary>
    /// 조준시 에임 방향으로 캐릭터 회전
    /// </summary>
    void RotateTowardsAimDirection()
    {
        //조준 모드일 경우 에임위치 따라 케릭터 회전
        if(Input.GetMouseButton(1))
        {
            // 카메라 중심에서 ray 생성
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            Vector3 targetPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.origin + ray.direction * 50f; // fallback
            }

            Vector3 lookDir = targetPoint - transform.position;
            lookDir.y = 0f; // y축 회전만

            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }
    }
}
