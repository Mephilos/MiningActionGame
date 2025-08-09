using UnityEngine;

/// <summary>
/// 레이저를 발사하는 무기 전략
/// </summary>
public class LaserStrategy : IWeaponStrategy
{
    public void Fire(WeaponController controller, PlayerData playerData, Transform firePoint, Vector3 aimDirection, Transform aimTarget)
    {
        WeaponData weaponData = controller.currentWeaponData;
        Vector3 fireDirection = aimDirection;

        // 락온 타겟이 있다면 그쪽으로 방향을 재계산
        if (aimTarget != null)
        {
            Vector3 targetCenter = aimTarget.GetComponent<Collider>()?.bounds.center ?? aimTarget.position;
            fireDirection = (targetCenter - firePoint.position).normalized;
        }

        if (fireDirection == Vector3.zero) fireDirection = firePoint.forward;

        float finalDamage = controller.CalculateFinalDamage();

        // 차지 레벨에 따른 데미지 보정
        if (weaponData.damageScalesWithCharge)
        {
            float chargeRatio = Mathf.Clamp01((controller.CurrentChargeLevel - weaponData.minChargeToFire) / (1f - weaponData.minChargeToFire));
            finalDamage *= Mathf.Lerp(1f, weaponData.maxChargeDamageMultiplier, chargeRatio);
        }
        else if (controller.CurrentChargeLevel >= weaponData.minChargeToFire && weaponData.maxChargeDamageMultiplier > 1f)
        {
            finalDamage *= weaponData.maxChargeDamageMultiplier;
        }

        // 레이저 이펙트 생성
        Vector3 laserEndPoint = firePoint.position + fireDirection * weaponData.range;
        if (weaponData.laserEffectPrefab != null)
        {
            GameObject laserEffect = Object.Instantiate(weaponData.laserEffectPrefab, firePoint.position, Quaternion.LookRotation(fireDirection));
            if (laserEffect.TryGetComponent<LaserBeam>(out var beamScript))
            {
                beamScript.Show(firePoint.position, laserEndPoint);
            }
            else
            {
                Object.Destroy(laserEffect, 0.2f);
            }
        }

        // 레이캐스트로 충돌 처리
        if (Physics.Raycast(firePoint.position, fireDirection, out RaycastHit hit, weaponData.range, controller.aimLayerMask))
        {
            laserEndPoint = hit.point;
            if (hit.collider.TryGetComponent<EnemyBase>(out var enemy)) enemy.TakeDamage(finalDamage);
            if (hit.collider.TryGetComponent<Destructible>(out var destructible)) destructible.TakeDamage(finalDamage);

            // 피격 지점 이펙트
            if (controller.muzzleFlashPrefab != null)
            {
                ObjectPoolManager.Instance.GetFromPool(controller.muzzleFlashPrefab.name, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }

        // 총구 화염 효과
        if (controller.muzzleFlashPrefab != null)
        {
            ObjectPoolManager.Instance.GetFromPool(controller.muzzleFlashPrefab.name, firePoint.position, firePoint.rotation);
        }
    }
}
