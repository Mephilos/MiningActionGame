using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimGuideController : MonoBehaviour
{
    public PlayerInputHandler playerInputHandler;
    public AimController aimController;
    public WeaponController weaponController;

    public float guideLength = 15f;
    public LayerMask obstacleLayerMask;
    
    private LineRenderer _lineRenderer;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.enabled = false;
    }

    void Update()
    {
        // 참조가 하나라도 준비되지 않았다면 아무것도 하지 않고 대기
        if (playerInputHandler == null || aimController == null || weaponController == null || weaponController.firePoint == null)
        {
            // 아직 참조가 연결되지 않았을 수 있으므로, 비활성화만 하고 오류는 출력하지 않음
            if(_lineRenderer.enabled) _lineRenderer.enabled = false;
            return;
        }

        if (playerInputHandler.IsAiming)
        {
            if(!_lineRenderer.enabled) _lineRenderer.enabled = true;
            UpdateGuideLine();
        }
        else
        {
            if(_lineRenderer.enabled) _lineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 가이드 라인 계산
    /// </summary>
    private void UpdateGuideLine()
    {
        Vector3 startPoint = weaponController.firePoint.position;
        Vector3 aimDirection = aimController.AimingDirection;
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
