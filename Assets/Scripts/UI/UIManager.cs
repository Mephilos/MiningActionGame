using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("게임 플레이 UI")]
    public TMP_Text playerHpText;
    public TMP_Text stageTimerText;
    public TMP_Text stageNumberText;
    public TMP_Text stageClearText;
    public TMP_Text playerResourceDisplayText;
    public CooldownDisplay dashCooldownDisplay;
    public CooldownDisplay skillCooldownDisplay;
    public Image skillIcon;
    public GameObject playerHealthBarPrefab; 
    
    [Header("진행 화면 전환 UI")] 
    public GameObject gameOverPanel;
    public GameObject stageClearPanel;
    
    [Header("상점 UI")]
    public GameObject shopPanel; 
    public TMP_Text shopResourceText; 
    public Button upgradeMaxHealthButton;
    public Button upgradeAttackDamageButton;
    public Button upgradeAttackSpeedButton;
    public TMP_Text maxHealthStatText;
    public TMP_Text attackDamageStatText;
    public TMP_Text attackSpeedStatText;
    public Button proceedToNextStageButtonFromShop;
    public Button healButton;
    public TMP_Text healCostText;
    public Button rerollPerksButton;
    public TMP_Text rerollCostText;
    
    [Header("상점 설정")]
    public List<PerkData> allPerks = new List<PerkData>();
    public GameObject perkUIPrefab;
    public Transform playerPerksPanel;
    public Transform dronePerksPanel;
    private int _healCost = 15;
    private float _healAmount = 50f;
    private int _initialRerollCost = 20;
    private int _currentRerollCost;

    // 내부 참조
    private List<ShopPerkUI> _activePerkUIItems = new List<ShopPerkUI>(); 
    private PlayerData _playerData;
    private StatUpgrader _statUpgrader;
    private WeaponController _weaponController;
    private SkillData _currentSkillData;
    private WorldSpaceHealthBar _playerHealthBarInstance;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerData = playerObj.GetComponent<PlayerData>();
            _weaponController = playerObj.GetComponent<WeaponController>();
            Initialize(_playerData, _weaponController);
        }
    }

    public void Initialize(PlayerData playerData, WeaponController weaponController)
    {
        _playerData = playerData;
        _weaponController = weaponController;
        _statUpgrader = _playerData.StatUpgrader; // PlayerData로부터 StatUpgrader 참조

        if (_playerData == null) Debug.LogError($"[{gameObject.name}] Initialize: PlayerData가 null입니다!");
        if (_weaponController == null) Debug.LogError($"[{gameObject.name}] Initialize: WeaponController가 null입니다!");

        // 월드 스페이스 체력바 생성 및 설정
        if (playerHealthBarPrefab != null)
        {
            GameObject hpBarObj = Instantiate(playerHealthBarPrefab, _playerData.transform.position, Quaternion.identity);
            _playerHealthBarInstance = hpBarObj.GetComponent<WorldSpaceHealthBar>();
            if (_playerHealthBarInstance != null)
            {
                _playerHealthBarInstance.targetToFollow = _playerData.transform;
                _playerData.OnHealthChanged += _playerHealthBarInstance.UpdateHealth;
                _playerData.OnDeath += HandlePlayerDeathForHealthBar;
                if (_weaponController != null)
                {
                    _weaponController.OnChargeChanged += HandleWeaponChargeChanged;
                    _weaponController.OnCooldownChanged += HandleWeaponCooldownChanged;
                }
            }
        }

        // 이벤트 구독
        _playerData.OnHealthChanged += UpdatePlayerHpUI;
        _playerData.OnResourcesChanged += UpdateResourceDisplayUI;
        _playerData.OnDeath += HandleGameOver;

        // 초기 UI 업데이트
        UpdatePlayerHpUI(_playerData.currentHealth, _playerData.maxHealth);
        UpdateResourceDisplayUI(_playerData.currentResources);
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (stageClearPanel != null) stageClearPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);

        // 버튼 리스너 연결
        upgradeMaxHealthButton?.onClick.AddListener(OnUpgradeMaxHealthPressed);
        upgradeAttackDamageButton?.onClick.AddListener(OnUpgradeAttackDamageButtonPressed);
        upgradeAttackSpeedButton?.onClick.AddListener(OnUpgradeAttackSpeedButtonPressed);
        proceedToNextStageButtonFromShop?.onClick.AddListener(OnNextStageButtonPressed);
        healButton?.onClick.AddListener(OnHealButtonPressed);
        rerollPerksButton?.onClick.AddListener(OnRerollPerksButtonPressed);

        // StageManager 이벤트 구독
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted += HandleStageStarted;
            StageManager.Instance.OnStageCleared += HandleStageCleared;
            StageManager.Instance.OnGameRestart += HandleGameRestart;
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (_playerData != null)
        {
            _playerData.OnHealthChanged -= UpdatePlayerHpUI;
            _playerData.OnResourcesChanged -= UpdateResourceDisplayUI;
            _playerData.OnDeath -= HandleGameOver;
            if (_playerHealthBarInstance != null)
            {
                _playerData.OnHealthChanged -= _playerHealthBarInstance.UpdateHealth;
                _playerData.OnDeath -= HandlePlayerDeathForHealthBar;
            }
        }
        if (_weaponController != null && _playerHealthBarInstance != null)
        {
            _weaponController.OnChargeChanged -= HandleWeaponChargeChanged;
            _weaponController.OnCooldownChanged -= HandleWeaponCooldownChanged;
        }
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted -= HandleStageStarted;
            StageManager.Instance.OnStageCleared -= HandleStageCleared;
            StageManager.Instance.OnGameRestart -= HandleGameRestart;
        }
    }

    // --- 이벤트 핸들러 --- 
    private void HandleWeaponChargeChanged(float chargeLevel)
    {
        if (_playerHealthBarInstance == null) return;
        _playerHealthBarInstance.SetAttackGaugeVisibility(true);
        _playerHealthBarInstance.UpdateAttackGauge(chargeLevel);
    }

    private void HandleWeaponCooldownChanged(float timeRemaining, float totalCooldown)
    {
        if (_playerHealthBarInstance == null || _weaponController.IsCharging) return;
        _playerHealthBarInstance.SetAttackGaugeVisibility(true);
        float fillAmount = (timeRemaining > 0 && totalCooldown > 0) ? (timeRemaining / totalCooldown) : 0f;
        _playerHealthBarInstance.UpdateAttackGauge(fillAmount);
    }

    private void HandlePlayerDeathForHealthBar()
    {
        if(_playerHealthBarInstance != null) _playerHealthBarInstance.SetVisibility(false);
    }

    private void HandleStageStarted(int stageNumber)
    {
        if (shopPanel != null && shopPanel.activeSelf) HideShopPanel();
        UpdateStageNumberUI(stageNumber);
        UpdateStageClearUI(stageNumber);
    }

    private void HandleStageCleared() => ShowStageClearScreen();
    private void HandleGameOver() => ShowGameOverScreem();

    private void HandleGameRestart()
    {
        HideGameOverScreem();
        HideShopPanel();
        if (_playerData != null) UpdateResourceDisplayUI(_playerData.currentResources);
        if (_playerHealthBarInstance != null) _playerHealthBarInstance.SetVisibility(true);
    }

    // --- UI 업데이트 --- 
    public void UpdatePlayerHpUI(float currentHp, float maxHp)
    {
        if (playerHpText != null) playerHpText.text = $"HP: {currentHp:F0}/{maxHp:F0}";
    }

    public void UpdateStageTimerUI(float timeLeft)
    {
        if (stageTimerText != null) stageTimerText.text = $"Time: {(timeLeft > 0 ? timeLeft : 0):00.00}";
    }

    public void UpdateStageNumberUI(int stageNum)
    {
        if (stageNumberText != null) stageNumberText.text = $"Stage: {stageNum}";
    }

    public void UpdateStageClearUI(int stageNum)
    {
        if (stageClearText != null)
        {
            stageClearText.text = $"{stageNum} Stage\nClear!";
        }
    }

    public void UpdateResourceDisplayUI(int currentResource)
    {
        if (playerResourceDisplayText != null) playerResourceDisplayText.text = $"Resource: {currentResource}";
        if (shopPanel != null && shopPanel.activeSelf && shopResourceText != null) shopResourceText.text = $"Resource: {currentResource}";
    }

    private void HandleCooldownDisplays()
    {
        if (_playerData == null) return;
        dashCooldownDisplay?.UpdateDisplay(_playerData.dashCooldownTimer, _playerData.currentDashCooldown);
        if (skillCooldownDisplay != null && _weaponController != null)
        {
            UpdateSkillIcon();
            if (_currentSkillData != null) skillCooldownDisplay.UpdateDisplay(_playerData.currentSkillCooldown, _currentSkillData.cooldown);
            else skillCooldownDisplay.UpdateDisplay(0, 0);
        }
    }

    private void UpdateSkillIcon()
    {
        if (_weaponController == null || _weaponController.currentWeaponData == null) return;
        SkillData weaponSkill = _weaponController.currentWeaponData.specialSkill;
        if (_currentSkillData != weaponSkill)
        {
            _currentSkillData = weaponSkill;
            if (skillIcon != null)
            {
                skillIcon.sprite = _currentSkillData?.skillIcon;
                skillIcon.enabled = _currentSkillData != null;
            }
        }
    }

    // --- 상점 관련 로직 --- 
    public void ShowShopPanel()
    {
        if (shopPanel == null) return;
        shopPanel.SetActive(true);
        Time.timeScale = 0f;
        _currentRerollCost = _initialRerollCost;
        PopulatePerks();
        if (_playerData != null) UpdateResourceDisplayUI(_playerData.currentResources);
    }
    
    public void HideShopPanel()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void UpdateShopStatsUI()
    {
        if (_playerData == null || _statUpgrader == null) return;

        maxHealthStatText.text = $"Max Health: {_playerData.maxHealth:F0}\n(cost: {_statUpgrader.MaxHealthUpgradeCost})";
        attackDamageStatText.text = $"Attack Power: {_playerData.currentAttackDamage:F1}\n(cost: {_statUpgrader.AttackDamageUpgradeCost})";
        attackSpeedStatText.text = $"Attack Rate: {_playerData.currentAttackSpeed:F2}\n(cost: {_statUpgrader.AttackSpeedUpgradeCost})";

        upgradeMaxHealthButton.interactable = _playerData.currentResources >= _statUpgrader.MaxHealthUpgradeCost;
        upgradeAttackDamageButton.interactable = _playerData.currentResources >= _statUpgrader.AttackDamageUpgradeCost;
        upgradeAttackSpeedButton.interactable = _playerData.currentResources >= _statUpgrader.AttackSpeedUpgradeCost;
        
        bool canHeal = _playerData.currentResources >= _healCost && _playerData.currentHealth < _playerData.maxHealth;
        healButton.interactable = canHeal;
        healCostText.text = $"HP recovery (+{_healAmount})\nCost: {_healCost}";
        if (_playerData.currentHealth >= _playerData.maxHealth) healCostText.text = "HP FULL!";

        rerollPerksButton.interactable = _playerData.currentResources >= _currentRerollCost;
        rerollCostText.text = $"Perk ReRoll\nCost: {_currentRerollCost}";
        
        foreach (var perkUI in _activePerkUIItems) perkUI.UpdateInteractable();
        if (shopResourceText != null) shopResourceText.text = $"current resources: {_playerData.currentResources}";
    }

    private void PopulatePerks()
    {
        foreach (Transform child in playerPerksPanel) Destroy(child.gameObject);
        foreach (Transform child in dronePerksPanel) Destroy(child.gameObject);
        _activePerkUIItems.Clear();

        var availablePlayerPerks = allPerks.Where(p => p.target == PerkTarget.Player).ToList();
        var availableDronePerks = allPerks.Where(p => p.target == PerkTarget.Drone).ToList();

        for (int i = 0; i < 2; i++)
        {
            if (!availablePlayerPerks.Any()) break;
            PerkData selectedPerk = GetWeightedRandomPerk(availablePlayerPerks);
            CreatePerkUI(selectedPerk, playerPerksPanel);
            availablePlayerPerks.Remove(selectedPerk);
        }
        for (int i = 0; i < 2; i++)
        {
            if (!availableDronePerks.Any()) break;
            PerkData selectedPerk = GetWeightedRandomPerk(availableDronePerks);
            CreatePerkUI(selectedPerk, dronePerksPanel);
            availableDronePerks.Remove(selectedPerk);
        }
        UpdateShopStatsUI();
    }

    private void CreatePerkUI(PerkData perk, Transform parentPanel)
    {
        GameObject perkGO = Instantiate(perkUIPrefab, parentPanel);
        if (perkGO.TryGetComponent<ShopPerkUI>(out var shopPerkUI))
        {
            shopPerkUI.Setup(perk, this);
            _activePerkUIItems.Add(shopPerkUI);
        }
    }

    private PerkData GetWeightedRandomPerk(List<PerkData> perks)
    {
        if (perks == null || !perks.Any()) return null;
        float totalWeight = perks.Sum(p => p.SpawnWeight);
        float randomPoint = Random.value * totalWeight;
        foreach (var perk in perks)
        {
            if (randomPoint < perk.SpawnWeight) return perk;
            randomPoint -= perk.SpawnWeight;
        }
        return perks.Last();
    }
    
    public bool CanAfford(int cost) => _playerData != null && _playerData.currentResources >= cost;

    public void TryPurchasePerk(PerkData perk, ShopPerkUI sourceUI)
    {
        if (CanAfford(perk.cost))
        {
            _playerData.SpendResources(perk.cost);
            ApplyPerkEffect(perk);
            Destroy(sourceUI.gameObject);
            _activePerkUIItems.Remove(sourceUI);
            UpdateShopStatsUI();
        }
    }
    
    private void ApplyPerkEffect(PerkData perk)
    {
        if (_playerData == null) return;
        switch (perk.effectType)
        {
            case PerkEffectType.PlayerMaxHealthAdd: _playerData.maxHealth += perk.effectValue; _playerData.Heal(perk.effectValue); break;
            case PerkEffectType.PlayerAttackDamageAdd: _playerData.currentAttackDamage += perk.effectValue; break;
            case PerkEffectType.PlayerAttackSpeedIncrease: _playerData.currentAttackSpeed = Mathf.Min(_playerData.currentAttackSpeed + perk.effectValue, 10f); break;
            case PerkEffectType.PlayerGlobalDamageMultiply: _playerData.globalDamageMultiplier += perk.effectValue; break;
            case PerkEffectType.PlayerGlobalAttackSpeedMultiply: _playerData.globalAttackCooldownMultiplier = Mathf.Max(0.1f, _playerData.globalAttackCooldownMultiplier - perk.effectValue); break;
        }
    }

    // --- 버튼 핸들러 ---
    public void OnUpgradeMaxHealthPressed() { _statUpgrader?.UpgradeMaxHealth(); UpdateShopStatsUI(); }
    public void OnUpgradeAttackDamageButtonPressed() { _statUpgrader?.UpgradeAttackDamage(); UpdateShopStatsUI(); }
    public void OnUpgradeAttackSpeedButtonPressed() { _statUpgrader?.UpgradeAttackSpeed(); UpdateShopStatsUI(); }
    public void OnHealButtonPressed()
    {
        if (_playerData != null && _playerData.currentHealth < _playerData.maxHealth && _playerData.SpendResources(_healCost))
        {
            _playerData.Heal(_healAmount);
            UpdateShopStatsUI();
        }
    }
    public void OnRerollPerksButtonPressed()
    {
        if (_playerData != null && _playerData.SpendResources(_currentRerollCost))
        {
            _currentRerollCost *= 2;
            PopulatePerks();
        }
    }
    public void OnShopProceedButtonPressed() => StageManager.Instance?.PlayerConfirmedShop();
    public void OnNextStageButtonPressed() => StageManager.Instance?.PlayerConfirmedNextStage();
    public void OnRestartButtonPressed()
    {
        Time.timeScale = 1f;
        StageManager.Instance?.RestartGame();
    }

    // --- 화면 전환 --- 
    public void ShowGameOverScreem() => gameOverPanel?.SetActive(true);
    public void HideGameOverScreem() => gameOverPanel?.SetActive(false);
    public void ShowStageClearScreen() => stageClearPanel?.SetActive(true);
    public void HideStageClearScreen() => stageClearPanel?.SetActive(false);
}
