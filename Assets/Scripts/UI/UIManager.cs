using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("게임 플레이 UI")] [Tooltip("플레이어 텍스트 UI")]
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
    public Button upgradeMoveSpeedButton;
    public Button upgradeAttackDamageButton;
    public Button upgradeAttackSpeedButton;
    // 능력치 현재 값 및 비용 표시 텍스트
    public TMP_Text moveSpeedStatText;
    public TMP_Text attackDamageStatText;
    public TMP_Text attackSpeedStatText;
    // 다음 스테이지 진행 버튼 (상점 ui용)
    public Button proceedToNextStageButtonFromShop;
    
    
    // TODO: 업그레이드 코스트 증가량(나중에 Data(ScriptableObject)로 관리 예정)
    private int _moveSpeedUpgradeCost = 10;
    private float _moveSpeedUpgradeAmount = 0.5f;
    private int _attackDamageUpgradeCost = 15;
    private float _attackDamageUpgradeAmount = 2f;
    private int _attackSpeedUpgradeCost = 20;
    private float _attackSpeedAmount = 0.05f;
    private float _attackSpeedCap = 0.1f;

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
    }
    public void Initialize(PlayerData playerData)
    {
        _playerData = playerData;
        if (_playerData == null)
        {
            Debug.LogError($"[{gameObject.name}] Initialize: PlayerData가 null입니다!");
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
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        // 게임 시작시 스테이지 클리어 패널 setActive로 끔
        if (stageClearPanel != null)
        {
            stageClearPanel.SetActive(false);
        }
        // 게임 시작시 상점 패널 숨김
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            
        }
        //상점 버튼 리스너 연결
        if (upgradeMoveSpeedButton != null)
        {
            upgradeMoveSpeedButton.onClick.AddListener(OnUpgradeMoveSpeedButtonPressed);
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
            UpdateShopStatsUI(); // 상점 열 때 스탯 정보 업데이트
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

        if (moveSpeedStatText != null)
        {
            moveSpeedStatText.text = $"Movement Speed: {_playerData.currentMaxSpeed:F1}\n(cost: {_moveSpeedUpgradeCost} / incease: +{_moveSpeedUpgradeAmount:F1})";
        }
        if (attackDamageStatText != null)
        {
            attackDamageStatText.text = $"Attack Power: {_playerData.currentAttackDamage:F1}\n(cost: {_attackDamageUpgradeCost} / increase: +{_attackDamageUpgradeAmount:F1})";
        }
        if (attackSpeedStatText != null)
        {
            attackSpeedStatText.text = $"Attack Cooldown: { 1f/_playerData.currentAttackSpeed:F2}sec\n(cost: {_attackSpeedUpgradeCost} / -{_attackSpeedAmount:F2}sec)";
        }
        
        // 버튼 활성화/비활성화 (자원 부족 시)
        if (upgradeMoveSpeedButton != null)
        {
            upgradeMoveSpeedButton.interactable = _playerData.currentResources >= _moveSpeedUpgradeCost;
        }

        if (upgradeAttackDamageButton != null)
        {
            upgradeAttackDamageButton.interactable = _playerData.currentResources >= _attackDamageUpgradeCost;
        }
        if(upgradeAttackSpeedButton != null)
        {
            // 이미 최소 쿨다운에 도달했으면 더 이상 업그레이드 불가
            bool canUpgrade = _playerData.currentResources >= _attackSpeedUpgradeCost 
                              && _playerData.currentAttackSpeed > _attackSpeedCap;
            
            upgradeAttackSpeedButton.interactable = canUpgrade;
            if (_playerData.currentAttackSpeed <= _attackSpeedCap && attackSpeedStatText != null)
            {
                attackSpeedStatText.text += "\n(Max AttackCooldown)";
            }
        }

        // 상점 내 자원도 여기서 한번 더 갱신
        if (shopResourceText != null)
        {
            shopResourceText.text = $"current resources: {_playerData.currentResources}";
        }
    }


    // 업그레이드 버튼 핸들러들
    public void OnUpgradeMoveSpeedButtonPressed()
    {
        if (_playerData != null && _playerData.SpendResources(_moveSpeedUpgradeCost))
        {
            _playerData.IncreaseMoveSpeed(_moveSpeedUpgradeAmount);
        }
        else
        {
            Debug.Log("이동 속도 업그레이드 실패: 자원 부족 또는 PlayerData 없음");
        }
    }

    public void OnUpgradeAttackDamageButtonPressed()
    {
        if (_playerData != null && _playerData.SpendResources(_attackDamageUpgradeCost))
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
            _playerData.SpendResources(_attackSpeedUpgradeCost))
        {
            _playerData.IncreaseAttackSpeed(_attackSpeedAmount, _attackSpeedCap);
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
