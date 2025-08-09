using UnityEngine;

/// <summary>
/// 모든 무기 발사 인터페이스
/// </summary>
public interface IWeaponStrategy
{
    /// <summary>
    /// 무기 발사
    /// </summary>
    /// <param name="controller">발사를 요청 WeaponController</param>
    /// <param name="playerData">플레이어의 현재 데이터</param>
    /// <param name="firePoint">발사 시작 위치</param>
    /// <param name="aimDirection">조준 방향</param>
    /// <param name="aimTarget">락온된 타겟</param>
    void Fire(WeaponController controller, PlayerData playerData, Transform firePoint, Vector3 aimDirection, Transform aimTarget);
}
