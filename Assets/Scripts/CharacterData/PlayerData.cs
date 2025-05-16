using UnityEngine;
using System.Collections.Generic;
public class PlayerData : MonoBehaviour
{
    // 기본 스텟 데이터(ScriptableObject)
    public PlayerBaseStatsData baseStatsData;

    // 체력 스탯
    public float currentHealth;
    private float _maxHealth;
    // 레벨 경험치
    public int level;
    public float currentXP;
    public float xpToNextLevel;
    // 전투력
    public float currentAttackDamage;
    // 이동기
    public float currentMoveSpeed;
    public float currentJumpForce;
    public int currentMaxJumpCount;
    public int jumpCountAvailable;
    public float currentDashForce;
    public float currentDashDuration;
    public float currentDashCooldown;
    public float dashCooldownTimer;
    public float currentDashInvincibleDuration;
    public bool isInvincible;
    public bool isDashing;
    // 드릴 능력치
    public float currentDrillPower;
    public float currentDrillRange;

    //TODO:아이템은 나중에 추가 (리스트 사용 예정)

    void Awake()
    {
        InitializeStatsFromBaseData();
    }
    void Update()
    {
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    public void InitializeStatsFromBaseData()
    {
        if (baseStatsData == null)
        {
            Debug.LogError("PlayerBaseStatsData가 이 PlayerStats 컴포넌트에 할당되지 않았습니다!");
            //기본값으로 초기화
            _maxHealth = 100f;
            currentHealth = _maxHealth;
            level = 1;
            currentXP = 0f;
            xpToNextLevel = 100f;
            currentAttackDamage = 10f;
            currentMoveSpeed = 5f;
            currentJumpForce = 8f;
            currentMaxJumpCount = 1;
            currentDashForce = 15f;
            currentDashDuration = 0.2f;
            currentDashCooldown = 1f;
            currentDashInvincibleDuration = 0.1f;
            currentDrillPower = 1f;
            currentDrillRange = 3f;
            Debug.LogWarning("임시 기본값으로 플레이어 스탯 초기화됨.");
            return;
        }

        // ScriptableObject로부터 값 불러와서 현재 스탯 초기화
        _maxHealth = baseStatsData.maxHealth;
        currentHealth = _maxHealth; // 시작 시 체력은 최대로

        level = 1; // 시작 레벨
        currentXP = 0f;
        xpToNextLevel = baseStatsData.initialXpToNextLevel;

        currentAttackDamage = baseStatsData.baseAttackDamage;

        currentMoveSpeed = baseStatsData.moveSpeed;
        currentJumpForce = baseStatsData.jumpForce;
        currentMaxJumpCount = baseStatsData.initialMaxJumpCount;

        currentDashForce = baseStatsData.dashForce;
        currentDashDuration = baseStatsData.dashDuration;
        currentDashCooldown = baseStatsData.dashCooldown;
        currentDashInvincibleDuration = baseStatsData.dashInvincibleDuration;

        currentDrillPower = baseStatsData.drillPower;
        currentDrillRange = baseStatsData.drillRange;

        // 기타 초기화
        jumpCountAvailable = currentMaxJumpCount;
        dashCooldownTimer = 0f;
        isInvincible = false;
        isDashing = false;

        Debug.Log("PlayerStats가 PlayerBaseStatsData로부터 초기화되었습니다.");
    }
    // 체력 관련 함수
    public void TakeDamage(float amount)
    {
        if (isInvincible) return;

        // TODO: 방어력 계산
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);
        Debug.Log($"받은 데미지: {amount} / 현재 체력: {currentHealth}/{_maxHealth}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);
        Debug.Log($"회복: {amount} / 현재 체력: {currentHealth}/{_maxHealth}");
    }
    void Die()
    {
        Debug.Log("플레이어 죽음");
        // TODO: 사망 처리
    }
    public void GainXP(float amount)
    {
        currentXP += amount;
        Debug.Log($"경험치 획득: {amount} / 현재 XP: {currentXP}/{xpToNextLevel}");
        CheckLevelUp();
    }
    void CheckLevelUp()
    {
        if (currentXP >= xpToNextLevel)
        {
            level++;
            currentXP -= xpToNextLevel; // 남은 경험치는 다음 레벨로 이월
            xpToNextLevel *= (baseStatsData != null ? baseStatsData.xpLevelUpMultiplier : 1.5f); // 다음 필요 경험치 증가

            // 레벨업 시 스탯 증가
            _maxHealth += 10; 
            currentHealth = _maxHealth; 
            currentAttackDamage += 2; 

            Debug.Log($"레벨 업! 레벨: {level}, 공격력: {currentAttackDamage}, 최대체력: {_maxHealth}. 다음 레벨 XP: {xpToNextLevel}");

            // 레벨업 후에도 남은 경험치가 여전히 다음 필요 경험치보다 많으면 다시 체크
            if (currentXP >= xpToNextLevel) CheckLevelUp();
        }

    }
    public void IncreaseMaxJumpCount(int amount)
    {
        currentMaxJumpCount += amount;
        // jumpCountAvailable도 상황에 맞게 조절 필요
        Debug.Log($"최대 점프 횟수 증가: {currentMaxJumpCount}");
    }
}
