using UnityEngine;
using UnityEngine.UI;

public class LockOnReticleUI : MonoBehaviour
{
    public Image lockOnMarkerImage;
    public PlayerController playerController;
    public Camera mainCamera;

    void Start()
    {
        if (lockOnMarkerImage == null)
        {
            Debug.LogError("LockOnMarkerImage가 할당 필요");
            enabled = false;
            return;
        }
        if (playerController == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerController = playerObj.GetComponent<PlayerController>();
            }
            if (playerController == null)
            {
                Debug.LogError($"[{gameObject.name}]씬에 PlayerController가 존재하지 않습니다");
                enabled = false;
                return;
            }
        }
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera 할당 필요");
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
            
            Collider targetCollider = currentTarget.GetComponent<Collider>();
            Vector3 targetWorldPosition = (targetCollider != null) ? targetCollider.bounds.center : currentTarget.position;
            
            // 타겟의 월드 좌표를 스크린 좌표로
            Vector3 targetScreenPosition = mainCamera.WorldToScreenPoint(targetWorldPosition);

            // 타겟이 카메라 화면 내에 있는지 확인
            bool isTargetVisible = targetScreenPosition.z > 0 && 
                                   targetScreenPosition.x > 0 && targetScreenPosition.x < Screen.width &&
                                   targetScreenPosition.y > 0 && targetScreenPosition.y < Screen.height;

            if (isTargetVisible)
            {
                lockOnMarkerImage.enabled = true;
                lockOnMarkerImage.rectTransform.position = targetScreenPosition;
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
}