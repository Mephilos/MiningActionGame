using UnityEngine;

public class DynamicCamera : MonoBehaviour
{
    [Header("타겟 설정")]
    public Transform target; // 플레이어 Transform

    [Header("카메라 기본 위치 및 각도")]
    [SerializeField] private float distance = 15f;         // 타겟으로부터의 기본 거리
    // [SerializeField] private float height = 12f;           // 타겟보다 얼마나 높이 있을지
    [SerializeField] private float horizontalAngle = 45f;  // Y축 기준 기본 회전 각도
    [SerializeField] private float verticalAngle = 35f;    // X축 기준 기본 기울기 각도 (내려다보는 각도)

    [Header("추적 설정")]
    [SerializeField] private float followSmoothSpeed = 5f;   // 카메라 이동 시 부드러움 정도
    [SerializeField] private float rotationSmoothSpeed = 5f; // 카메라 회전 시 부드러움 정도

    [Header("동적 줌 (Perspective 모드 - FOV 조절)")]
    [SerializeField] private float baseFieldOfView = 50f;    // 기본 시야각
    [SerializeField] private float zoomedFieldOfView = 40f;  // 줌 인 되었을 때 시야각 (예: 플레이어 부스트 시)
    [SerializeField] private float fovSmoothSpeed = 3f;      // FOV 변경 시 부드러움 정도

    private Camera _cam;
    private Vector3 _currentVelocity = Vector3.zero; // SmoothDamp용 현재 속도

    // 플레이어 컨트롤러 참조 
    private PlayerController _playerController;
    private PlayerData _playerData;


    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null)
        {
            Debug.LogError($"[{gameObject.name}] 카메라 오브젝트 필요");
            enabled = false;
            return;
        }
        _cam.orthographic = false;
        _cam.fieldOfView = baseFieldOfView;

        if (target != null)
        {
            _playerController = target.GetComponent<PlayerController>();
            _playerData = target.GetComponent<PlayerData>();
        }
        else
        {
            Debug.LogWarning($"[{gameObject}] 카메라 확인");
        }
    }

    void FixedUpdate()
    {
        if (target == null) return;

        HandleCameraPositionAndRotation();
        HandleDynamicFOV();
    }

    void HandleCameraPositionAndRotation()
    {
        // 목표 카메라 회전값 계산
        Quaternion desiredRotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);

        // 타겟 위치 기준으로 오프셋 계산 (회전을 적용한 거리와 높이)
        Vector3 targetHeadPosition = target.position + Vector3.up * 1.0f; // 예시: 플레이어 머리 위 1유닛
        Vector3 desiredPosition = targetHeadPosition - (desiredRotation * Vector3.forward * distance);

        // 부드럽게 카메라 위치 이동 SmoothDamp 
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, 1f / followSmoothSpeed);
        // Lerp
        //transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSmoothSpeed);
        
        Vector3 lookAtPoint = target.position + Vector3.up * 1.5f; // 플레이어의 가슴 높이 정도
        Quaternion targetLookRotation = Quaternion.LookRotation(lookAtPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetLookRotation, Time.deltaTime * rotationSmoothSpeed);
    }
    /// <summary>
    /// 특정 기능에서 fov변화
    /// 대쉬 중 fov 변경
    /// </summary>
    void HandleDynamicFOV()
    {
        float targetFOV = baseFieldOfView;
        
        if (_playerData != null && _playerData.isDashing) // 또는 별도의 'isBoosting' 상태 변수 사용
        {
            targetFOV = zoomedFieldOfView; // 대쉬/부스트 시 FOV 변경 (값을 더 크게 하면 Zoom Out)
        }

        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, Time.deltaTime * fovSmoothSpeed);
    }
}