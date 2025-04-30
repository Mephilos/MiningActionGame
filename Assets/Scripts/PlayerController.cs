using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    public float groundCheckDistance = 1.1f;  // 바닥 체크 거리 (캡슐 높이 고려)
    public LayerMask groundLayer;             // 어떤 레이어를 '바닥'으로 볼지 설정
    [SerializeField] private CameraFollow cameraFollow; // 카메라 기준 방향
    private Rigidbody rb;
    private Vector3 inputDir;
    private bool isGrounded;


  
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX 
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        HandleJump();
        CheckGrounded();
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
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // 카메라 기준 방향
        Vector3 camForward = cameraFollow.forward;
        Vector3 camRight = cameraFollow.right;

        inputDir = (camForward * moveZ + camRight * moveX).normalized;

        Debug.DrawRay(transform.position, inputDir, Color.blue);
               
        if (inputDir.sqrMagnitude > 0.01f)
        {
            // 이동
            Vector3 moveVector = inputDir * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + moveVector);

            // 회전 (즉시 적용)
            Quaternion targetRotation = Quaternion.LookRotation(inputDir);
            transform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// 점프 (바닥에 있을 때만)
    /// </summary>
    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            Debug.Log("isGrounded: " + isGrounded);
        }
    }

    /// <summary>
    /// Raycast 바닥 판정
    /// </summary>
    void CheckGrounded()
    {
        // Ray로 Ground Check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);

        // 디버그(빨간 = 공중, 초록 = 바닥에 닿음)
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }
}
