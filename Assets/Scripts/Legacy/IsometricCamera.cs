using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    public Transform target; // 플레이어 Transform
    public float distance = 15f; // 플레이어로부터의 기본 거리 (Orthographic에서는 Size로 조절)
    public float heightOffset = 10f; // 플레이어 머리 위로 얼마나 높이 있을지
    public float horizontalAngle = 45f; // Y축 기준 회전 각도
    public float verticalAngle = 30f; // X축 기준 기울기 각도 (내려다보는 각도)
    public float orthographicSize = 10f;// 카메라의 Orthographic Size (줌 역할)

    private Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null)
        {
            Debug.LogError($"[{gameObject.name}]카메라가 없습니다");
            enabled = false;
            return;
        }
        _cam.orthographic = true; // 직교 투영으로 설정
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning($"[{gameObject.name}]카메라 타겟을 설정해야 합니다");
            return;
        }

        _cam.orthographicSize = orthographicSize; 
        
        Vector3 targetPosition = target.position + Vector3.up * heightOffset;
        
        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);
        
        Vector3 offsetDirection = rotation * Vector3.forward; 
        transform.position = targetPosition - (offsetDirection * distance); 
        
        transform.LookAt(target.position);
    }
}