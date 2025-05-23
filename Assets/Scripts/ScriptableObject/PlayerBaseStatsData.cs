using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerBaseStats", menuName = "Player Character/Player Base Stats", order = 0)]
public class PlayerBaseStatsData : ScriptableObject // MonoBehaviour 대신 ScriptableObject를 상속
{
    [Header("기본 생존 스탯")]
    public float maxHealth = 100f;
    
    [Header("기본 전투 능력치")]
    public float initialAttackDamage = 10f;
    public float initialAttackSpeed = 0.5f;
    [Header("자원 관련")]
    public int initialResources = 10;
    [Header("기본 이동 및 액션 관련")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public int initialMaxJumpCount = 1; // 기본 점프 횟수

    public float dashForce = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float dashInvincibleDuration = 0.1f;

    [Header("기본 자원 채집 (드릴)")]
    public float drillPower = 1f;
    public float drillRange = 3f;
}