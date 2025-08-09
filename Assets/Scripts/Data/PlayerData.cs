using System;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public event Action<float, float> OnHealthChanged;
    public event Action<int> OnResourcesChanged;
    public event Action OnDeath;

    [Header("플레이어 스탯 데이터")] 
    public PlayerBaseStatsData baseStatsData;
    public StatUpgrader StatUpgrader { get; private set; }

    [Header("체력 스탯")] 
    public float currentHealth;
    public float maxHealth;
    public bool isDead;

    [Header("자원 관련 스탯")] 
    public int currentResources;

    [Header("전투 관련 스탯")] 
    public float currentAttackDamage;
    public float currentAttackSpeed;
    public float globalDamageMultiplier = 1;
    public float globalAttackCooldownMultiplier = 1;

    [Header("이동 관련 스탯")] 
    public float currentMaxSpeed;
    public float currentRotationSpeed;
    public float currentAcceleration;
    public float currentDeceleration;
    public float currentBoostFactor;
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

    [Header("스킬 관련 스탯")] 
    public float currentSkillCooldown;

    void Awake()
    {
        StatUpgrader = new StatUpgrader(this);
        InitializeStatsFromBaseData();
        currentSkillCooldown = 0;
    }

    void Update()
    {
        if (currentSkillCooldown > 0) currentSkillCooldown -= Time.deltaTime;
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
    }

    public void InitializeStatsFromBaseData()
    {
        if (baseStatsData == null)
        {
            Debug.LogError("[PlayerData] PlayerBaseStatsData가 할당되지 않음. 임시 값으로 초기화합니다.");
            maxHealth = 100f;
            currentResources = 0;
            currentAttackDamage = 10f;
            currentAttackSpeed = 0.5f;
            currentMaxSpeed = 7f;
            currentRotationSpeed = 360f;
            currentAcceleration = 10f;
            currentDeceleration = 10f;
            currentBoostFactor = 1.5f;
            currentJumpForce = 8f;
            currentMaxJumpCount = 2;
            currentDashForce = 15f;
            currentDashDuration = 0.2f;
            currentDashCooldown = 1f;
            currentDashInvincibleDuration = 0.1f;
        }
        else
        {
            maxHealth = baseStatsData.maxHealth;
            currentResources = baseStatsData.initialResources;
            currentAttackDamage = baseStatsData.initialAttackDamage;
            currentAttackSpeed = baseStatsData.initialAttackSpeed;
            currentMaxSpeed = baseStatsData.maxSpeed;
            currentRotationSpeed = baseStatsData.rotationSpeed;
            currentAcceleration = baseStatsData.acceleration;
            currentDeceleration = baseStatsData.deceleration;
            currentBoostFactor = baseStatsData.boostFactor;
            currentJumpForce = baseStatsData.jumpForce;
            currentMaxJumpCount = baseStatsData.initialMaxJumpCount;
            currentDashForce = baseStatsData.dashForce;
            currentDashDuration = baseStatsData.dashDuration;
            currentDashCooldown = baseStatsData.dashCooldown;
            currentDashInvincibleDuration = baseStatsData.dashInvincibleDuration;
        }

        StatUpgrader.InitializeCosts();
        
        currentHealth = maxHealth;
        jumpCountAvailable = currentMaxJumpCount;
        dashCooldownTimer = 0f;
        isInvincible = false;
        isDashing = false;
        isDead = false;

        Debug.Log($"[{gameObject.name}] PlayerData가 초기화되었습니다.");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnResourcesChanged?.Invoke(currentResources);
    }

    public void TakeDamage(float amount)
    {
        if (isDead || isInvincible) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDeath?.Invoke();

        GetComponent<PlayerController>().enabled = false;
        GetComponent<WeaponController>().enabled = false;

        if (StageManager.Instance != null)
        {
            StageManager.Instance.HandlePlayerDeath();
        }
        else
        {
            Debug.LogWarning("[PlayerData] StageManager를 찾을 수 없어 플레이어 사망 처리에 실패했습니다.");
            Time.timeScale = 0f;
        }
    }

    public void ReviveAndReset()
    {
        Debug.Log("[PlayerData] 플레이어 부활 및 스탯 초기화");
        InitializeStatsFromBaseData();
        isDead = false;

        GetComponent<PlayerController>().enabled = true;
        GetComponent<WeaponController>().enabled = true;
    }

    public void GainResources(int amount)
    {
        if (isDead) return;
        currentResources += amount;
        OnResourcesChanged?.Invoke(currentResources);
    }

    public bool SpendResources(int amountToSpend)
    {
        if (currentResources >= amountToSpend)
        {
            currentResources -= amountToSpend;
            OnResourcesChanged?.Invoke(currentResources);
            return true;
        }
        return false;
    }
}