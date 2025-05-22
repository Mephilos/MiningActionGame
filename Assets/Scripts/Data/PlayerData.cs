using UnityEngine;

public class PlayerData : MonoBehaviour
{
    // 기본 스텟 데이터(ScriptableObject)
    public PlayerBaseStatsData baseStatsData;

    // 체력 스탯
    public float currentHealth;
    private float _maxHealth;
    public bool isDead;
    
    // 자원
    public int currentResources;

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
            Debug.LogError("[PlayerData] PlayerBaseStatsData가 PlayerStats 컴포넌트에 할당되지 않았습니다. 임시 값으로 초기화 합니다");
            //기본값으로 초기화
            _maxHealth = 100f;
            currentHealth = _maxHealth;
            currentResources = 0;
            currentAttackDamage = 10f;
            currentMoveSpeed = 5f;
            currentJumpForce = 8f;
            currentMaxJumpCount = 2;
            currentDashForce = 15f;
            currentDashDuration = 0.2f;
            currentDashCooldown = 1f;
            currentDashInvincibleDuration = 0.1f;
            currentDrillPower = 1f;
            currentDrillRange = 3f;
            return;
        }
        else
        {
            // ScriptableObject로부터 값 불러와서 현재 스탯 초기화
            _maxHealth = baseStatsData.maxHealth;
            currentResources = baseStatsData.initialResources;
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
        }

        // ScriptableObject로부터 값 불러와서 현재 스탯 초기화
        currentHealth = _maxHealth; // 시작 시 체력은 최대로

        // 기타 초기화
        jumpCountAvailable = currentMaxJumpCount;
        dashCooldownTimer = 0f;
        isInvincible = false;
        isDashing = false;
        Debug.Log("[PlayerData] PlayerStats가 PlayerBaseStatsData로부터 초기화되었습니다.");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerHpUI(currentHealth, _maxHealth);
            UIManager.Instance.UpdateResourceDisplayUI(currentResources);
        }

        isDead = false;
    }
    /// <summary>
    /// 대미지 받는 함수
    /// </summary>
    /// <param name="amount">받을 데미지</param>
    public void TakeDamage(float amount)
    {
        if (isDead || isInvincible)
        {
            if(isDead) Debug.Log("[PlayerData] 플레이어가 이미 사망한 상태");
            if(isInvincible) Debug.Log("[PlayerData] 플레이어 무적 상태");
            return;
        }
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);
        Debug.Log($"[PlayerData] 받은 데미지: {amount} / 현재 체력: {currentHealth}/{_maxHealth}");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerHpUI(currentHealth, _maxHealth);
        }
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    /// <summary>
    /// 채력 회복
    /// </summary>
    /// <param name="amount">회복량</param>
    public void Heal(float amount)
    {
        if (isDead) return; // 죽었을 때 회복 방지
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);
        Debug.Log($"[PlayerData] 회복: {amount} / 현재 체력: {currentHealth}/{_maxHealth}");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerHpUI(currentHealth, _maxHealth);
        }
    }

    /// <summary>
    /// 플레이어 사망처리(StageManager참조)
    /// </summary>
    void Die()
    {
        if (isDead) return; // 중복 사망 처리 방지
        
        isDead = true;
        Debug.Log("[PlayerData] 플레이어 죽음");
        
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        WeaponController weaponController = GetComponent<WeaponController>();
        if (weaponController != null)
        {
            weaponController.enabled = false;
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.HandlePlayerDeath();
        }
        else
        {
            Debug.LogWarning("[PlayerData] 플레이어 사망 처리 오류 발생");
            Time.timeScale = 0f;
        }
    }
    /// <summary>
    /// 부활 , 스탯 초기화 (재시작시 사용)
    /// </summary>
    public void ReviveAndReset()
    {
        Debug.Log("[PlayerData] 플레이어 부활 및 스탯 초기화");
        InitializeStatsFromBaseData();
        isDead =false;
        
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = true;
            controller.ResetVelocity(); // 혹시 모를 속도 잔여분 초기화
        }
        else
        {
            Debug.LogWarning("[PlayerData] PlayerController를 찾을 수 없음");
        }

        WeaponController weaponController = GetComponent<WeaponController>();
        if (weaponController != null)
        {
            weaponController.enabled = true;
        }
        else
        {
            Debug.LogWarning("[PlayerData] PlayerController를 찾을 수 없음");
        }
    }
    /// <summary>
    /// 자원 획득 로직
    /// </summary>
    /// <param name="amount">획득한 자원</param>
    public void GainResources(int amount)
    {
        if(isDead) return;
        
        currentResources += amount;
        Debug.Log($"[PlayerData] 자원획득 {amount}, 현재 자원 {currentResources}");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateResourceDisplayUI(currentResources);
        }
    }
    /// <summary>
    /// 자원 소비 시도. 성공하면 true, 실패(자원 부족)하면 false 반환.
    /// </summary>
    public bool SpendResources(int amountToSpend)
    {
        if (currentResources >= amountToSpend)
        {
            currentResources -= amountToSpend;
            Debug.Log($"[PlayerData] 자원 소비: {amountToSpend} / 남은 자원: {currentResources}");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateResourceDisplayUI(currentResources); // UI 업데이트
            }
            return true;
        }
        else
        {
            Debug.LogWarning($"[PlayerData] 자원 부족! 필요 자원: {amountToSpend}, 현재 자원: {currentResources}");
            return false;
        }
    }
    /// <summary>
    /// 이동 속도 증가
    /// </summary>
    public void IncreaseMoveSpeed(float additionalSpeed)
    {
        currentMoveSpeed += additionalSpeed;
        Debug.Log($"[PlayerData] 이동 속도 증가! 현재 이동 속도: {currentMoveSpeed}");
        // 필요하다면 UIManager를 통해 변경된 스탯을 UI에 표시할 수 있습니다.
        if (UIManager.Instance != null) UIManager.Instance.UpdateShopStatsUI();
    }
    /// <summary>
    /// 공격력 증가
    /// </summary>
    public void IncreaseAttackDamage(float additionalDamage)
    {
        currentAttackDamage += additionalDamage;
        Debug.Log($"[PlayerData] 공격력 증가! 현재 공격력: {currentAttackDamage}");
        if (UIManager.Instance != null) UIManager.Instance.UpdateShopStatsUI();
    }
    public void IncreaseMaxJumpCount(int amount)
    {
        currentMaxJumpCount += amount;
        // jumpCountAvailable도 상황에 맞게 조절 필요
        Debug.Log($"[PlayerData] 최대 점프 횟수 증가: {currentMaxJumpCount}");
    }
}
