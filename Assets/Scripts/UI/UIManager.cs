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
    
    [Header("진행 화면 전환 UI")] 
    [Tooltip("게임 오버 시 활성화")]
    public GameObject gameOverPanel;
    [Tooltip("스테이지 클리어시 활성화")]
    public GameObject stageClearPanel;
    
    [Header("상점 UI")]
    public GameObject shopPanel; 
    public TMP_Text shopResourceText; 
    // 능력치 업그레이드 버튼
    public Button upgradeMaxHealthButton;
    public Button upgradeAttackDamageButton;
    public Button upgradeAttackSpeedButton;
    // 능력치 현재 값 및 비용 표시 텍스트
    public TMP_Text maxHealthStatText;
    public TMP_Text attackDamageStatText;
    public TMP_Text attackSpeedStatText;
    // 다음 스테이지 진행 버튼 (상점 ui용)
    public Button proceedToNextStageButtonFromShop;

    public Button healButton;
    public TMP_Text healCostText;
    public Button rerollPerksButton;
    public TMP_Text rerollCostText;
    
    public List<PerkData> allPerks = new List<PerkData>();
    public GameObject perkUIPrefab;
    public Transform playerPerksPanel;
    public Transform dronePerksPanel;
    
    private List<ShopPerkUI> _activePerkUIItems = new List<ShopPerkUI>(); 
    
    private float _maxHealthUpgradeAmount = 20f;
    private float _attackDamageUpgradeAmount = 2f;
    private float _attackSpeedAmount = 0.2f;
    private float _attackSpeedCap = 10f;

    private int _healCost = 15;
    private float _healAmount = 50f;
    private int _initialRerollCost = 20;
    private int _currentRerollCost;
    
    private PlayerData _playerData; // 의존성 주입 방식
    
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Initialize(playerObj.GetComponent<PlayerData>());
        }
    }
    public void Initialize(PlayerData playerData)
    {
        _playerData = playerData;
        if (_playerData == null)
        {
            Debug.LogError($"[{gameObject.name}] Initialize: PlayerData가 null입니다!");
            return;
        }
        // PlayerData가 설정된 후 관련 UI 초기 업데이트
        if (_playerData != null)
        {
            UpdateResourceDisplayUI(_playerData.currentResources);
            UpdateShopStatsUI(); // 상점 스탯 UI도 PlayerData 참조 후 업데이트
        }
    }
    void Start()
    {
        // 게임 시작시 게임 오버 패널 setActive로 꺼버림
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        // 게임 시작시 스테이지 클리어 패널 setActive로 끔
        if (stageClearPanel != null) stageClearPanel.SetActive(false);
        
        // 게임 시작시 상점 패널 숨김
        if (shopPanel != null) shopPanel.SetActive(false);
        //상점 버튼 리스너 연결
        if (upgradeMaxHealthButton != null)
        {
            upgradeMaxHealthButton.onClick.AddListener(OnUpgradeMaxHealthPressed);
        }
        if (upgradeAttackDamageButton != null)
        {
            upgradeAttackDamageButton.onClick.AddListener(OnUpgradeAttackDamageButtonPressed);
        }
        if (upgradeAttackSpeedButton != null)
        {
            upgradeAttackSpeedButton.onClick.AddListener(OnUpgradeAttackSpeedButtonPressed);
        }
        // 상점에서 게임 시작 버튼
        if (proceedToNextStageButtonFromShop != null)
        {
            proceedToNextStageButtonFromShop.onClick.AddListener(OnNextStageButtonPressed);
        }
        if (healButton != null)
        {
            healButton.onClick.AddListener(OnHealButtonPressed);
        }
        if (rerollPerksButton != null)
        {
            rerollPerksButton.onClick.AddListener(OnRerollPerksButtonPressed);
        }
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted += HandleStageStarted;
            StageManager.Instance.OnStageCleared += HandleStageCleared;
            StageManager.Instance.OnGameOver += HandleGameOver;
            StageManager.Instance.OnGameRestart += HandleGameRestart;
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted -= HandleStageStarted;
            StageManager.Instance.OnStageCleared -= HandleStageCleared;
            StageManager.Instance.OnGameOver -= HandleGameOver;
            StageManager.Instance.OnGameRestart -= HandleGameRestart;
        }
    }
    
    private void HandleStageStarted(int stageNumber)
    {
        if (shopPanel != null && shopPanel.activeSelf)
        {
            HideShopPanel();
        }
        UpdateStageNumberUI(stageNumber);
        UpdateStageClearUI(stageNumber); // 다음 스테이지 클리어 텍스트 미리 준비
    }

    private void HandleStageCleared()
    {
        ShowStageClearScreen();
    }

    private void HandleGameOver()
    {
        ShowGameOverScreem();
    }

    private void HandleGameRestart()
    {
        HideGameOverScreem();
        HideShopPanel();
        if (_playerData != null)
        {
            UpdateResourceDisplayUI(_playerData.currentResources);
        }
    }

    /// <summary>
    /// 플레이어 체력UI
    /// </summary>
    /// <param name="currentHp">현재 체력</param>
    /// <param name="maxHp">최대 체력</param>
    public void UpdatePlayerHpUI(float currentHp, float maxHp)
    {
        if (playerHpText != null)
        {
            playerHpText.text = $"HP: {currentHp}/{maxHp}";
        }
    }
    /// <summary>
    /// 스테이지 타이머
    /// </summary>
    /// <param name="timeLeft">남은 시간</param>
    public void UpdateStageTimerUI(float timeLeft)
    {
        if (stageTimerText != null)
        {
            if (timeLeft < 0)
            {
                timeLeft = 0;
            }
            stageTimerText.text = $"Time: {timeLeft:00.00}";
        }
    }
    /// <summary>
    /// 스테이지 숫자 표시
    /// </summary>
    /// <param name="stageNum">스테이지 표시 숫자</param>
    public void UpdateStageNumberUI(int stageNum)
    {
        if (stageNumberText != null)
        {
            stageNumberText.text = $"Stage: {stageNum}";
        }
    }

    /// <summary>
    /// 리소스(자원) 디스플레이 ui
    /// </summary>
    /// <param name="currentResource">현재자원(소지자원)</param>
    public void UpdateResourceDisplayUI(int currentResource)
    {
        if (playerResourceDisplayText != null)
        {
            playerResourceDisplayText.text = $"Resource: {currentResource} ";
        }

        if (shopPanel != null && shopPanel.activeSelf && shopResourceText != null)
        {
            shopResourceText.text = $"Resource: {currentResource} ";
        }
    }
    
    // 상점 UI 관련 메서드
    public void ShowShopPanel()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            Time.timeScale = 0f; // 상점 열리면 시간 정지
            _currentRerollCost = _initialRerollCost;
            PopulatePerks(); // 내부에 UpdateShopStatsUI() 호출
            
            if (_playerData != null)
            {
                 UpdateResourceDisplayUI(_playerData.currentResources); // 상점 내 자원 텍스트 업데이트
            }
        }
        if (stageClearPanel != null) stageClearPanel.SetActive(false); 
    }
    
    public void HideShopPanel()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            Time.timeScale = 1f; // 상점 닫으면 시간 재개
        }
    }

    /// <summary>
    /// 상점 내 스탯 관련 UI (현재 값, 비용 등) 업데이트
    /// </summary>
    public void UpdateShopStatsUI()
    {
        if (_playerData == null) return;

        if (maxHealthStatText != null)
        {
            maxHealthStatText.text =
                $"Max Health: {_playerData.maxHealth:F0}\n(cost: {_playerData.maxHealthUpgradeCost} / +{_maxHealthUpgradeAmount:F0})";
        }

        if (attackDamageStatText != null)
        {
            attackDamageStatText.text =
                $"Attack Power: {_playerData.currentAttackDamage:F1}\n(cost: {_playerData.attackDamageUpgradeCost} / increase: +{_attackDamageUpgradeAmount:F1})";
        }

        if (attackSpeedStatText != null)
        {
            attackSpeedStatText.text =
                $"Attack Rate: {_playerData.currentAttackSpeed:F2}\n(cost: {_playerData.attackSpeedUpgradeCost} / +{_attackSpeedAmount:F2}회)";
        }

        // 버튼 활성화/비활성화 (자원 부족 시)
        if (upgradeMaxHealthButton != null)
        {
            upgradeMaxHealthButton.interactable = _playerData.currentResources >= _playerData.maxHealthUpgradeCost;
        }
        if (upgradeAttackDamageButton != null)
        {
            upgradeAttackDamageButton.interactable = _playerData.currentResources >= _playerData.attackDamageUpgradeCost;
        }
        if(upgradeAttackSpeedButton != null)
        {
            // 이미 최소 쿨다운에 도달했으면 더 이상 업그레이드 불가
            bool canUpgrade = _playerData.currentResources >= _playerData.attackSpeedUpgradeCost 
                              && _playerData.currentAttackSpeed > _attackSpeedCap;
            
            upgradeAttackSpeedButton.interactable = canUpgrade;
            if (_playerData.currentAttackSpeed <= _attackSpeedCap && attackSpeedStatText != null)
            {
                attackSpeedStatText.text += "\n(Max AttackCooldown)";
            }
        }
        
        if (healButton != null && healCostText != null)
        {
            bool canHeal = _playerData.currentResources >= _healCost && _playerData.currentHealth < _playerData.maxHealth;
            healButton.interactable = canHeal;
            healCostText.text = $"HP recovery (+{_healAmount})\nCost: {_healCost}";
            if (_playerData.currentHealth >= _playerData.maxHealth)
            {
                healCostText.text = "HP FULL!";
            }
        }

        // 특성 리롤 버튼 UI 업데이트
        if (rerollPerksButton != null && rerollCostText != null)
        {
            rerollPerksButton.interactable = _playerData.currentResources >= _currentRerollCost;
            rerollCostText.text = $"Perk ReRoll\nCost: {_currentRerollCost}";
        }
        
        foreach (var perkUI in _activePerkUIItems)
        {
            perkUI.UpdateInteractable();
        }
        // 상점 내 자원도 여기서 한번 더 갱신
        if (shopResourceText != null)
        {
            shopResourceText.text = $"current resources: {_playerData.currentResources}";
        }
    }

    private void PopulatePerks()
    {
        // 기존 퍽 UI 정리
        foreach (Transform child in playerPerksPanel) Destroy(child.gameObject);
        foreach (Transform child in dronePerksPanel) Destroy(child.gameObject);
        _activePerkUIItems.Clear();

        // 퍽 목록 필터링
        var availablePlayerPerks = allPerks.Where(p => p.target == PerkTarget.Player).ToList();
        var availableDronePerks = allPerks.Where(p => p.target == PerkTarget.Drone).ToList();

        // 플레이어 퍽 2개 랜덤 선택 및 생성
        for (int i = 0; i < 2; i++)
        {
            PerkData selectedPerk = GetWeightedRandomPerk(availablePlayerPerks);
            if (selectedPerk != null)
            {
                CreatePerkUI(selectedPerk, playerPerksPanel);
                availablePlayerPerks.Remove(selectedPerk); // 중복 방지
            }
        }

        // 드론 퍽 2개 랜덤 선택 및 생성
        for (int i = 0; i < 2; i++)
        {
            PerkData selectedPerk = GetWeightedRandomPerk(availableDronePerks);
            if (selectedPerk != null)
            {
                CreatePerkUI(selectedPerk, dronePerksPanel);
                availableDronePerks.Remove(selectedPerk); // 중복 방지
            }
        }
        UpdateShopStatsUI();
    }
    private void CreatePerkUI(PerkData perk, Transform parentPanel)
    {
        GameObject perkGO = Instantiate(perkUIPrefab, parentPanel);
        ShopPerkUI shopPerkUI = perkGO.GetComponent<ShopPerkUI>();
        if (shopPerkUI != null)
        {
            shopPerkUI.Setup(perk, this);
            _activePerkUIItems.Add(shopPerkUI);
        }
    }

    private PerkData GetWeightedRandomPerk(List<PerkData> perks)
    {
        if (perks == null || perks.Count == 0) return null;

        float totalWeight = perks.Sum(p => p.SpawnWeight);
        float randomPoint = Random.value * totalWeight;

        foreach (var perk in perks)
        {
            if (randomPoint < perk.SpawnWeight)
                return perk;
            else
                randomPoint -= perk.SpawnWeight;
        }
        return perks[perks.Count - 1];
    }
    
    // 구매 관련 (자원이 충분 한지 판별)
    public bool CanAfford(int cost)
    {
        return _playerData != null && _playerData.currentResources >= cost;
    }

    public void TryPurchasePerk(PerkData perk, ShopPerkUI sourceUI)
    {
        if (_playerData != null && _playerData.SpendResources(perk.cost))
        {
            ApplyPerkEffect(perk);
            Destroy(sourceUI.gameObject); // 구매 후 UI 제거
            _activePerkUIItems.Remove(sourceUI);
            UpdateShopStatsUI(); // 모든 버튼의 구매 가능 여부 갱신
        }
        else
        {
            Debug.Log($"{perk.perkName} 구매 실패: 자원 부족");
        }
    }
    
    private void ApplyPerkEffect(PerkData perk)
    {
        if (_playerData == null) return;
        
        switch (perk.effectType)
        {
            // Player Perks
            case PerkEffectType.PlayerMaxHealthAdd:
                _playerData.IncreaseMaxHealth(perk.effectValue);
                break;
            case PerkEffectType.PlayerAttackDamageAdd:
                _playerData.IncreaseAttackDamage(perk.effectValue);
                break;
            case PerkEffectType.PlayerAttackSpeedIncrease:
                _playerData.IncreaseAttackSpeed(perk.effectValue, _attackSpeedCap);
                break;
            case PerkEffectType.PlayerGlobalDamageMultiply:
                _playerData.globalDamageMultiplier += perk.effectValue;
                break;
            case PerkEffectType.PlayerGlobalAttackSpeedMultiply:
                _playerData.globalAttackCooldownMultiplier -= perk.effectValue;
                if (_playerData.globalAttackCooldownMultiplier < 0.1f) _playerData.globalAttackCooldownMultiplier = 0.1f;
                break;
        }
    }

    // 업그레이드 버튼 핸들러들
    public void OnUpgradeMaxHealthPressed()
    {
        if (_playerData != null && _playerData.SpendResources(_playerData.maxHealthUpgradeCost))
        {
            _playerData.IncreaseMaxHealth(_maxHealthUpgradeAmount);
        }
    }

    public void OnUpgradeAttackDamageButtonPressed()
    {
        if (_playerData != null && _playerData.SpendResources(_playerData.attackDamageUpgradeCost))
        {
            _playerData.IncreaseAttackDamage(_attackDamageUpgradeAmount);
        }
        else
        {
            Debug.Log("공격력 업그레이드 실패: 자원 부족 또는 PlayerData 없음");
        }
    }

    public void OnUpgradeAttackSpeedButtonPressed()
    {
        if (_playerData != null &&
            _playerData.currentAttackSpeed > _attackSpeedCap &&
            _playerData.SpendResources(_playerData.attackSpeedUpgradeCost))
        {
            _playerData.IncreaseAttackSpeed(_attackSpeedAmount, _attackSpeedCap);
        }
    }
    public void OnHealButtonPressed()
    {
        // 버튼이 눌릴 수 있는 조건은 이미 UpdateShopStatsUI에서 관리하지만, 한번 더 확인
        if (_playerData != null && _playerData.currentHealth < _playerData.maxHealth && _playerData.SpendResources(_healCost))
        {
            _playerData.Heal(_healAmount);
            UpdateShopStatsUI(); // HP 회복 후 UI 즉시 갱신
        }
    }

    public void OnRerollPerksButtonPressed()
    {
        if (_playerData != null && _playerData.SpendResources(_currentRerollCost))
        {
            _currentRerollCost *= 2;
            PopulatePerks(); // 특성 목록을 다시 생성 (이 메서드는 내부적으로 UpdateShopStatsUI를 호출함)
            Debug.Log("특성 리롤 완료");
        }
    }

    /// <summary>
    /// 스테이지 클리어 ui 텍스트
    /// </summary>
    /// <param name="stageNum">스테이지 번호</param>
    public void UpdateStageClearUI(int stageNum)
    {
        if (stageClearText != null)
        {
            stageClearText.text = $"{stageNum} Stage\nClear!";
        }
    }
    /// <summary>
    /// 게임 오버화면 표시
    /// </summary>
    public void ShowGameOverScreem()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 게임 오버 화면 숨기는 함수 (재시작시)
    /// </summary>
    public void HideGameOverScreem()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    public void ShowStageClearScreen()
    {
        if (stageClearPanel != null)
        {
            stageClearPanel.SetActive(true);
        }
    }

    public void HideStageClearScreen()
    {
        if (stageClearPanel != null)
        {
            stageClearPanel.SetActive(false);
        }
    }

    // 클리어 화면에서 "상점으로" 버튼 핸들러
    public void OnShopProceedButtonPressed()
    {
        Debug.Log("상점: 으로");
        
        if (StageManager.Instance != null)
        {
            StageManager.Instance.PlayerConfirmedShop(); // 기존 다음 스테이지 진행 로직 호출
        }
        else
        {
            Debug.LogError("[UIManager] StageManager 인스턴스가 없음");
        }
    }

    public void OnNextStageButtonPressed()
    {
        Debug.Log("다음 스테이지 버튼 클릭");
        if (StageManager.Instance != null)
        {
            StageManager.Instance.PlayerConfirmedNextStage();
        }
        else
        {
            Debug.LogError("[UIManager] StageManager 인스턴스가 없음");
        }
    }

    public void OnRestartButtonPressed()
    {
        Debug.Log("재시작 버튼 클릭");
        Time.timeScale = 1f;
        if (StageManager.Instance != null)
        {
            StageManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("[UIManager] 스테이지 메니져 인스턴스가 없음");
        }
    }
}
