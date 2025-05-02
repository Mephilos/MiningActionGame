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

        Debug.DrawRay(transform.position, inputDir, Color.blue);
        Vector3 horizontalVelocity = inputDir * moveSpeed;
        //중력 구현
        if (controller.isGrounded && velocity.y < 0)
        {   
            velocity.y = -2f; // 바닥에 눌러붙게
        }
        velocity.y += gravity * Time.deltaTime;
        Vector3 finalVelocity = horizontalVelocity + Vector3.up * velocity.y;
        controller.Move(finalVelocity * Time.deltaTime);

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
    /// Raycast 바닥 판정
    /// </summary>
    void CheckGrounded()
    {
        //레이로 상태 그라운드 상태 확인
        Debug.DrawRay(transform.position, Vector3.down * 0.2f, controller.isGrounded ? Color.green : Color.red);
    }

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
