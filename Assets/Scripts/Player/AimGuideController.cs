using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimGuideController : MonoBehaviour
{
    public PlayerController playerController;
    public WeaponController weaponController;

    public float guideLength = 15f;
    public LayerMask obstacleLayerMask;
    
    private LineRenderer _lineRenderer;
    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (playerController == null || weaponController == null)
        {
            Debug.LogError($"[{gameObject.name}] 필요한 참조 누락");
            enabled = false;
            return;
        }
        _lineRenderer.enabled = false;
    }

    void Update()
    {
        if (playerController == null || weaponController == null || weaponController.firePoint == null) return;

        if (playerController.IsAiming)
        {
            _lineRenderer.enabled = true;
            UpdateGuideLine();
        }
        else
        {
            _lineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 가이드 라인 계산
    /// </summary>
    private void UpdateGuideLine()
    {
        Vector3 startPoint = weaponController.firePoint.position;
        Vector3 aimDirection = playerController.AimingDirection;
        Vector3 endPoint;
        
        RaycastHit hit;

        if (Physics.Raycast(startPoint, aimDirection, out hit, guideLength, obstacleLayerMask))
        {
            endPoint = hit.point;
        }
        else
        {
            endPoint = startPoint + aimDirection * guideLength;
        }
        
        _lineRenderer.SetPosition(0, startPoint);
        _lineRenderer.SetPosition(1, endPoint);
    }
}
