using UnityEngine;

[CreateAssetMenu(fileName = "NewHelpDroneData", menuName = "Player Character/Player Help Drone")]
public class PlayerHelpDroneData : ScriptableObject
{
    [Header("기본 공격 능력치")] 
    public float attackDamage = 5f;
    public float attackCooldown = 1.5f;
    public float projectileSpeed;
    public GameObject projectilePrefab;
    
    [Header("타켓팅 능력치")]
    public float detectionRadius;
    public float attackRange;
}
