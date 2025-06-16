using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Serialization;

public enum PerkRarity
{
    Common,
    Uncommon,
    Rare,
    Epic
}

public enum PerkTarget
{
    Player,
    Drone
}

public enum PerkEffectType
{
    // 플레이어 스텟 증가
    PlayerMaxHealthAdd,
    PlayerAttackDamageAdd,
    PlayerAttackSpeedIncrease,
    
    PlayerGlobalDamageMultiply,
    PlayerGlobalAttackSpeedMultiply,
    
    // 드론 효과
    DroneAttackDamageAdd,
    DroneAttackCooldownDecrease,
    DroneAttackRangeAdd,
    DroneProjectileAdd,
    DronePlayerShield,
    DronePlayerHealOverTime
}
[CreateAssetMenu(fileName = "NewPerkData", menuName = "Game Data/Perk Data")]
public class PerkData : ScriptableObject, IWeightedItem 
{
    public string perkName;
    [TextArea] public string description;
    public Sprite icon;
    public int cost;

    [Header("분류")]
    public PerkTarget target;
    public PerkRarity rarity;

    [Header("효과")]
    public PerkEffectType effectType;
    public float effectValue;

    // 희귀도에 따라 스폰 가중치를 반환
    public float SpawnWeight
    {
        get
        {
            switch (rarity)
            {
                case PerkRarity.Common: return 10f;
                case PerkRarity.Uncommon: return 5f;
                case PerkRarity.Rare: return 2f;
                case PerkRarity.Epic: return 1f;
                default: return 1f;
            }
        }
    }
}

