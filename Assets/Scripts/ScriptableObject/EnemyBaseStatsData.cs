using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyBaseStats", menuName = "Enemy Data/Base Stats")]
public class EnemyBaseStatsData : ScriptableObject
{
    public string enemyName = "기본 적";

    [Header("채력 스탯")]
    public float maxHealth = 50f;

    [Header("공격 능력치")]
    public float attackDamage = 10f; // 일반 공격이 있다면 (자폭 미사일은 자폭 데미지)
    public float selfDestructDamage = 20f; // 자폭 시 데미지 (BasicEnemy의 경우)
    // public float attackRange = 0f;
    // public float attackCooldown = 0f;
    
    [Header("원거리 공격 전용 (RangedEnemy가 사용)")]
    public float rangedAttackRange = 15f;   // 원거리 공격 사거리
    public float attackCooldown = 2f;       // 공격 딜레이
    public GameObject projectilePrefab;
    
    [Header("이동 및 행동 관련")]
    public float moveSpeed = 3.5f; // BasicEnemy의 moveSpeed와 연동
    public float detectionRadius = 15f; // BasicEnemy의 detectionRadius와 연동
    

    [Header("보상")] 
    public int resourcesToGive = 10;
    // TODO: 자원 더 주는 오브젝트 드랍(확률 기반)

}