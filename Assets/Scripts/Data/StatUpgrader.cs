using UnityEngine;

/// <summary>
/// 플레이어의 스탯 업그레이드 로직을 전담하는 클래스
/// </summary>
public class StatUpgrader
{
    private PlayerData _playerData;

    // 업그레이드 비용
    public int MaxHealthUpgradeCost { get; private set; }
    public int AttackDamageUpgradeCost { get; private set; }
    public int AttackSpeedUpgradeCost { get; private set; }

    // 업그레이드 수치
    private const float MaxHealthUpgradeAmount = 20f;
    private const float AttackDamageUpgradeAmount = 2f;
    private const float AttackSpeedUpgradeAmount = 0.2f;
    private const float AttackSpeedCap = 10f;

    public StatUpgrader(PlayerData playerData)
    {
        _playerData = playerData;
        InitializeCosts();
    }

    /// <summary>
    /// 업그레이드 비용을 초기값으로 설정
    /// </summary>
    public void InitializeCosts()
    {
        MaxHealthUpgradeCost = 10;
        AttackDamageUpgradeCost = 15;
        AttackSpeedUpgradeCost = 20;
    }

    /// <summary>
    /// 최대 체력을 업그레이드
    /// </summary>
    public void UpgradeMaxHealth()
    {
        if (!_playerData.SpendResources(MaxHealthUpgradeCost)) return;

        _playerData.maxHealth += MaxHealthUpgradeAmount;
        _playerData.Heal(MaxHealthUpgradeAmount); // 업그레이드 시 현재 체력도 채워줌
        MaxHealthUpgradeCost += 5;
        Debug.Log($"[StatUpgrader] 최대 체력 증가. 현재: {_playerData.maxHealth}");
    }

    /// <summary>
    /// 공격력을 업그레이드
    /// </summary>
    public void UpgradeAttackDamage()
    {
        if (!_playerData.SpendResources(AttackDamageUpgradeCost)) return;

        _playerData.currentAttackDamage += AttackDamageUpgradeAmount;
        AttackDamageUpgradeCost += 8;
        Debug.Log($"[StatUpgrader] 공격력 증가. 현재 공격력: {_playerData.currentAttackDamage}");
    }

    /// <summary>
    /// 공격 속도를 업그레이드
    /// </summary>
    public void UpgradeAttackSpeed()
    {
        if (_playerData.currentAttackSpeed >= AttackSpeedCap) return;
        if (!_playerData.SpendResources(AttackSpeedUpgradeCost)) return;

        _playerData.currentAttackSpeed += AttackSpeedUpgradeAmount;
        if (_playerData.currentAttackSpeed > AttackSpeedCap)
        {
            _playerData.currentAttackSpeed = AttackSpeedCap;
        }
        AttackSpeedUpgradeCost += 10;
        Debug.Log($"[StatUpgrader] 공격 속도 증가. 현재: {_playerData.currentAttackSpeed}");
    }
}
