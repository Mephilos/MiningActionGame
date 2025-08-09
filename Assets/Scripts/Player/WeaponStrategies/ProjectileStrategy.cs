using UnityEngine;

/// <summary>
/// 투사체 발사
/// </summary>
public class ProjectileStrategy : IWeaponStrategy
{
    public void Fire(WeaponController controller, PlayerData playerData, Transform firePoint, Vector3 aimDirection, Transform aimTarget)
    {
        WeaponData weaponData = controller.currentWeaponData;
        if (weaponData.projectilePrefab == null) return;

        string projectileTag = weaponData.projectilePrefab.name;
        Vector3 fireDirection = aimDirection;
        Quaternion bulletRotation = Quaternion.LookRotation(fireDirection);

        // 락온 타겟이 있다면 그쪽으로 방향을 재계산
        if (aimTarget != null)
        {
            Vector3 targetCenter = aimTarget.GetComponent<Collider>()?.bounds.center ?? aimTarget.position;
            fireDirection = (targetCenter - firePoint.position).normalized;
            bulletRotation = Quaternion.LookRotation(fireDirection);
        }

        GameObject bullet = ObjectPoolManager.Instance.GetFromPool(projectileTag, firePoint.position, bulletRotation);

        if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = fireDirection * controller.bulletSpeed;
        }

        if (bullet.TryGetComponent<Projectile>(out Projectile projectileScript))
        {
            float finalDamage = controller.CalculateFinalDamage();
            projectileScript.SetDamage(finalDamage);

            if (weaponData.explosionRadius > 0f)
            {
                projectileScript.InitializeExplosion(
                    weaponData.explosionRadius,
                    weaponData.explosionEffectPrefab,
                    weaponData.explosionDamageLayerMask
                );
            }
        }

        // 총구 화염 효과
        if (controller.muzzleFlashPrefab != null)
        {
            GameObject muzzleFlash = ObjectPoolManager.Instance.GetFromPool(controller.muzzleFlashPrefab.name, firePoint.position, firePoint.rotation);
            if (muzzleFlash != null)
            {
                muzzleFlash.transform.parent = firePoint;
            }
        }
    }
}
