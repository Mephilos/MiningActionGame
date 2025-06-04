using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game Data/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "기본 무기";
    public float baseDamage = 15f; // 무기 자체의 기본 데미지
    public float attackSpeed = 1.0f;
    public float range = 100f;
    public GameObject projectilePrefab; // 발사체 프리팹
    
    [Header("Explosion Settings (범위 공격용)")]
    [Tooltip("폭발 반경. 0 이하면 단일 타겟으로 간주됩니다.")]
    public float explosionRadius = 0f;
    [Tooltip("폭발 시 생성될 이펙트 프리팹입니다.")]
    public GameObject explosionEffectPrefab;
    [Tooltip("폭발 데미지가 적용될 레이어를 선택합니다.")]
    public LayerMask explosionDamageLayerMask;
    
    [Header("Charge Laser Settings (차징 레이저 무기용)")]
    public bool isChargeWeapon = false; // 이 무기가 차징 방식인지 여부
    [Tooltip("최대 차징까지 걸리는 시간 (초)")]
    public float chargeTime = 2.0f;
    [Tooltip("발사 가능한 최소 차징 비율 (0.0 ~ 1.0)")]
    public float minChargeToFire = 0.1f; // 최소 10% 차징해야 발사 가능
    [Tooltip("차징 레벨에 따라 데미지가 증가하는지 여부")]
    public bool damageScalesWithCharge = true;
    [Tooltip("최대 차징 시 기본 데미지에 곱해지는 최대 배율")]
    public float maxChargeDamageMultiplier = 2.0f; // 최대 차징 시 데미지 2배
    [Tooltip("레이저 빔 시각 효과 프리팹 (LineRenderer 포함)")]
    public GameObject laserEffectPrefab;
    [Tooltip("차징 중일 때 무기나 플레이어에게 표시될 이펙트 프리팹")]
    public GameObject chargeEffectPrefab;
    [Tooltip("마우스 버튼을 뗄 때 발사할지 여부")]
    public bool fireOnRelease = true;
}