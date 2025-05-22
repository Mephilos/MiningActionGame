using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyBaseStats", menuName = "Enemy Data/Base Stats")]
public class EnemyBaseStatsData : ScriptableObject
{
    public string enemyName = "기본 적"; // 에디터에서 구분하기 위한 이름

    [Header("채력 스탯")]
    public float maxHealth = 50f;

    [Header("공격 능력치")]
    public float attackDamage = 10f; // 일반 공격이 있다면 (자폭 미사일은 자폭 데미지)
    public float selfDestructDamage = 20f; // 자폭 시 데미지 (BasicEnemy의 경우)
    // public float attackRange = 0f;
    // public float attackCooldown = 0f;

    [Header("이동 및 행동 관련")]
    public float moveSpeed = 3.5f; // BasicEnemy의 moveSpeed와 연동
    public float detectionRadius = 15f; // BasicEnemy의 detectionRadius와 연동
    // public float selfDestructDelay = 0f; // BasicEnemy의 selfDestructDelay와 연동 (이미 BasicEnemy에 있음)

    [Header("보상")] 
    public int resourcesToGive = 10;
    // TODO: 자원 더 주는 오브젝트 드랍(확률 기반)

}