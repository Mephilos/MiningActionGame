//using System.Numerics;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float gravity = -20f;

    [SerializeField] private CameraFollow cameraFollow; // 카메라 기준 방향
    private CharacterController controller;
    private Vector3 inputDir;
    private Vector3 velocity;


  
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleJump();
        CheckGrounded();
        RotateTowardsAimDirection();
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
        // 입력만 받아놓고 방향은 카메라 기준으로 변환
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // 카메라 기준 방향
        Vector3 camForward = cameraFollow.forward;
        Vector3 camRight = cameraFollow.right;

        inputDir = (camForward * moveZ + camRight * moveX).normalized;

        Vector3 horizontalVelocity = inputDir * moveSpeed;
        //중력 구현
        if (controller.isGrounded && velocity.y < 0)
        {   
            velocity.y = -2f; // 바닥에 눌러붙게
        }
        velocity.y += gravity * Time.fixedDeltaTime;

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
    /// 점프 (바닥에 있을 때만)
    /// </summary>
    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            velocity.y = jumpForce;
            Debug.Log("점프 상태: " + controller.isGrounded);
        }
    }

    /// <summary>
    /// Raycast 바닥 판정(디버그용)
    /// </summary>
    void CheckGrounded()
    {
        //레이로 상태 그라운드 상태 확인
        Debug.DrawRay(transform.position, Vector3.down * 0.2f, controller.isGrounded ? Color.green : Color.red);
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
