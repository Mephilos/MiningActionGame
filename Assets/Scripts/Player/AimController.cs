using System.Collections.Generic;
using UnityEngine;

public class AimController : MonoBehaviour
{
    [Header("Aim Assist Settings")]
    public float aimAssistRadius = 10f; // 에임 어시스트 감지 반경
    public float horizontalAimConeAngle = 15f; // 플레이어 정면 기준 각도
    public float verticalAimConeAngle = 25f; // 플레이어 정면 기준 수직
    public LayerMask aimAssistLayerMask; // 에임 어시스트 적용 레이어 마스크 
    public LayerMask obstacleLayerMask; // 장애물 판별용 레이어 마스크 
    public float onReticleHorizontalAngle = 5.0f; // 조준선 일치 각도
    public Vector3 AimingDirection { get; private set; }
    public Transform LockedAimAssistTarget { get; private set; }

    private CharacterController _characterController;
    private PlayerInputHandler _inputManager;
    private WeaponController _weaponController;

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _inputManager = GetComponent<PlayerInputHandler>();
        _weaponController = GetComponent<WeaponController>();
    }

    void Update()
    {
        UpdateAimingDirection();
        if (_inputManager.IsAiming)
        {
            UpdateLockOnTargetLogic();
        }
        else
        {
            LockedAimAssistTarget = null;
        }
    }

    private void UpdateAimingDirection()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(_inputManager.MousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 targetPoint = ray.GetPoint(rayDistance);
            Vector3 direction = (targetPoint - transform.position);
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
            {
                AimingDirection = direction.normalized;
            }
        }
    }

    private void UpdateLockOnTargetLogic()
    {
        LockedAimAssistTarget = null;
        if (AimingDirection.sqrMagnitude < 0.01f) return;

        Vector3 playerCharacterCenter = transform.position + _characterController.center;
        Collider[] hitColliders = Physics.OverlapSphere(playerCharacterCenter, aimAssistRadius, aimAssistLayerMask);

        Transform bestTargetOverall = null;
        float minHorizontalAngleOverall = horizontalAimConeAngle + 1.0f;
        List<Transform> candidatesOnReticle = new List<Transform>();

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == this.transform) continue;

            Vector3 targetCenter = hitCollider.bounds.center;
            Vector3 directionToTarget = targetCenter - playerCharacterCenter;
            if (directionToTarget.sqrMagnitude < 0.001f) continue;

            Vector3 vectorToTargetHorizontal = directionToTarget;
            vectorToTargetHorizontal.y = 0;
            float horizontalAngle;

            if (vectorToTargetHorizontal.sqrMagnitude < 0.001f)
            {
                horizontalAngle = Vector3.Angle(AimingDirection.normalized, Vector3.ProjectOnPlane(directionToTarget, Vector3.up).normalized);
                if (Vector3.ProjectOnPlane(directionToTarget, Vector3.up).sqrMagnitude < 0.001f) horizontalAngle = 0;
            }
            else
            {
                horizontalAngle = Vector3.Angle(AimingDirection.normalized, vectorToTargetHorizontal.normalized);
            }

            if (horizontalAngle <= horizontalAimConeAngle)
            {
                float verticalAngle = Vector3.Angle(vectorToTargetHorizontal.normalized, directionToTarget.normalized);
                if (verticalAngle <= verticalAimConeAngle)
                {
                    if (HasLineOfSightToTarget(hitCollider.transform))
                    {
                        if (horizontalAngle <= onReticleHorizontalAngle)
                        {
                            candidatesOnReticle.Add(hitCollider.transform);
                        }
                        if (horizontalAngle < minHorizontalAngleOverall)
                        {
                            minHorizontalAngleOverall = horizontalAngle;
                            bestTargetOverall = hitCollider.transform;
                        }
                    }
                }
            }
        }

        if (candidatesOnReticle.Count > 0)
        {
            float closestDistanceSqr = float.MaxValue;
            foreach (Transform candidate in candidatesOnReticle)
            {
                float distSqr = (candidate.position - playerCharacterCenter).sqrMagnitude;
                if (distSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distSqr;
                    LockedAimAssistTarget = candidate;
                }
            }
        }
        else if (bestTargetOverall != null)
        {
            LockedAimAssistTarget = bestTargetOverall;
        }
    }

    private bool HasLineOfSightToTarget(Transform target)
    {
        if (target == null) return false;

        Vector3 rayStartPoint;
        if (_weaponController != null && _weaponController.firePoint != null)
        {
            rayStartPoint = _weaponController.firePoint.position;
        }
        else
        {
            if (_characterController != null)
            {
                float headHeightRatio = 0.45f;
                rayStartPoint = (transform.position + _characterController.center) + Vector3.up * (_characterController.height * headHeightRatio);
            }
            else
            {
                return false;
            }
        }

        Vector3 targetPoint = target.position;
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            targetPoint = targetCollider.bounds.center;
        }

        Vector3 directionToTarget = targetPoint - rayStartPoint;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget < aimAssistRadius * 0.2f)
        {
            return true;
        }

        if (Physics.Raycast(rayStartPoint, directionToTarget.normalized, distanceToTarget, obstacleLayerMask))
        {
            return false;
        }

        return true;
    }
    
    void OnDrawGizmosSelected()
    {
        if (_characterController == null) _characterController = GetComponent<CharacterController>();
        if (_characterController == null) return;

        Vector3 gizmoOrigin = transform.position + _characterController.center;

        Gizmos.color = new Color(0, 0.8f, 0, 0.10f);
        Gizmos.DrawWireSphere(gizmoOrigin, aimAssistRadius);

        if (_inputManager != null && _inputManager.IsAiming && AimingDirection.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(gizmoOrigin, AimingDirection.normalized * aimAssistRadius);

            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.2f);
            Vector3 leftRayOverallH = Quaternion.Euler(0, -horizontalAimConeAngle, 0) * AimingDirection.normalized;
            Vector3 rightRayOverallH = Quaternion.Euler(0, horizontalAimConeAngle, 0) * AimingDirection.normalized;
            Gizmos.DrawRay(gizmoOrigin, leftRayOverallH * (aimAssistRadius * 0.9f));
            Gizmos.DrawRay(gizmoOrigin, rightRayOverallH * (aimAssistRadius * 0.9f));
            Gizmos.DrawLine(gizmoOrigin + leftRayOverallH * (aimAssistRadius * 0.9f), gizmoOrigin + rightRayOverallH * (aimAssistRadius * 0.9f));

            Gizmos.color = new Color(0, 1f, 1f, 0.3f);
            Vector3 leftRayOnReticleH = Quaternion.Euler(0, -onReticleHorizontalAngle, 0) * AimingDirection.normalized;
            Vector3 rightRayOnReticleH = Quaternion.Euler(0, onReticleHorizontalAngle, 0) * AimingDirection.normalized;
            Gizmos.DrawRay(gizmoOrigin, leftRayOnReticleH * aimAssistRadius);
            Gizmos.DrawRay(gizmoOrigin, rightRayOnReticleH * aimAssistRadius);
            Gizmos.DrawLine(gizmoOrigin + leftRayOnReticleH * aimAssistRadius, gizmoOrigin + rightRayOnReticleH * aimAssistRadius);
        }

        if (LockedAimAssistTarget != null)
        {
            Gizmos.color = Color.red;
            Vector3 targetDisplayPoint = LockedAimAssistTarget.position;
            Collider targetCol = LockedAimAssistTarget.GetComponent<Collider>();
            if (targetCol != null) targetDisplayPoint = targetCol.bounds.center;

            Vector3 startLinePoint = gizmoOrigin;
            if (_weaponController != null && _weaponController.firePoint != null)
            {
                startLinePoint = _weaponController.firePoint.position;
            }
            Gizmos.DrawLine(startLinePoint, targetDisplayPoint);
        }
    }
}
