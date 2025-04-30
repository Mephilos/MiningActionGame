//using System.Numerics;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;                 // 따라갈 타겟 (플레이어)
    public Vector3 offset = new Vector3(0, 3, -5);  // 타겟 기준 위치
    public Vector3 forward { get; private set; } // 플레이어 이동 방향 계산용
    public Vector3 right { get; private set; }

    void LateUpdate()
    {
        // 목표 위치 = 플레이어 위치 + 오프셋
        transform.position = target.position + offset;
        // 플레이어를 항상 바라봄
        transform.LookAt(target.position + Vector3.up * 1.5f);

        forward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        right   = new Vector3(transform.right.x, 0f, transform.right.z).normalized;
    }
}
