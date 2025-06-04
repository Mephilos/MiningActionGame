using UnityEngine;
using UnityEngine.UI;

public class LockOnReticleUI : MonoBehaviour
{
    public Image lockOnMarkerImage;
    public PlayerController playerController;
    public Camera mainCamera;
    public Vector3 offset = new Vector3(0, 0, 0); 


    void Start()
    {
        if (lockOnMarkerImage == null)
        {
            Debug.LogError("LockOnMarkerImage가 할당되지 않았습니다!");
            enabled = false;
            return;
        }
        if (playerController == null)
        {
            Debug.LogError("PlayerController가 할당되지 않았습니다!");
            enabled = false;
            return;
        }
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }
        lockOnMarkerImage.enabled = false;
    }

    void Update()
    {
        if (playerController == null || lockOnMarkerImage == null || mainCamera == null) return;

        Transform currentTarget = playerController.GetLockedTarget();

        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            // 타겟의 월드 좌표를 스크린 좌표로 변환
            Vector3 targetScreenPosition = mainCamera.WorldToScreenPoint(currentTarget.position + GetTargetHeightOffset(currentTarget));

            // 타겟이 카메라 화면 내에 있는지 확인
            bool isTargetVisible = targetScreenPosition.z > 0 && 
                                   targetScreenPosition.x > 0 && targetScreenPosition.x < Screen.width &&
                                   targetScreenPosition.y > 0 && targetScreenPosition.y < Screen.height;

            if (isTargetVisible)
            {
                lockOnMarkerImage.enabled = true;
                lockOnMarkerImage.rectTransform.position = targetScreenPosition + offset; // 오프셋 적용
            }
            else
            {
                lockOnMarkerImage.enabled = false; // 화면 밖에 있으면 숨김
            }
        }
        else
        {
            lockOnMarkerImage.enabled = false; // 락온된 타겟이 없으면 숨김
        }
    }

    // 타겟 오프셋 반환
    Vector3 GetTargetHeightOffset(Transform target)
    {
        Collider col = target.GetComponent<Collider>();
        if (col != null)
        {
            return new Vector3(0, col.bounds.extents.y, 0); // 콜라이더의 절반 높이만큼 위로
        }
        return Vector3.zero; // 콜라이더 없으면 오프셋 없음
    }
}