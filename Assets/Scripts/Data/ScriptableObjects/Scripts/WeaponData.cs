using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game Data/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "기본 무기";
    public float baseDamage = 15f; // 무기 자체의 기본 데미지
    public float attackSpeed = 1.0f;
    public float range = 100f;
    public GameObject projectilePrefab; // 발사체 프리팹
}