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
    // 능력치 현재 값 및 비용 표시 텍스트
    public TMP_Text moveSpeedStatText;
    public TMP_Text attackDamageStatText;
    // 다음 스테이지 진행 버튼 (상점 ui용)
    public Button proceedToNextStageButtonFromShop;
    
    
    // TODO: 업그레이드 코스트 증가량(나중에 Data(ScriptableObject)로 관리 예정)
    private int _moveSpeedUpgradeCost = 10;
    private float _moveSpeedUpgradeAmount = 0.5f;
    private int _attackDamageUpgradeCost = 15;
    private float _attackDamageUpgradeAmount = 2f;

    private PlayerData _playerDataCache;
    
    
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

    void Start()
    {
        // TODO: PlayerData 리펙토링
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerDataCache = playerObj.GetComponent<PlayerData>();
        }
        else
        {
            Debug.LogError("[UIManager] Player 오브젝트를 찾을 수 없습니다. 상점 기능이 제대로 작동하지 않을 수 있습니다.");
        }
        
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
            if (_playerDataCache != null)
            {
                 UpdateResourceDisplayUI(_playerDataCache.currentResources); // 상점 내 자원 텍스트 업데이트
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
        if (_playerDataCache == null) return;

        if (moveSpeedStatText != null)
        {
            moveSpeedStatText.text = $"이동 속도: {_playerDataCache.currentMoveSpeed:F1}\n(비용: {_moveSpeedUpgradeCost} / 증가량: +{_moveSpeedUpgradeAmount:F1})";
        }
        if (attackDamageStatText != null)
        {
            attackDamageStatText.text = $"공격력: {_playerDataCache.currentAttackDamage:F1}\n(비용: {_attackDamageUpgradeCost} / 증가량: +{_attackDamageUpgradeAmount:F1})";
        }
        
        // 버튼 활성화/비활성화 (자원 부족 시)
        if (upgradeMoveSpeedButton != null)
        {
            upgradeMoveSpeedButton.interactable = _playerDataCache.currentResources >= _moveSpeedUpgradeCost;
        }

        if (upgradeAttackDamageButton != null)
        {
            upgradeAttackDamageButton.interactable = _playerDataCache.currentResources >= _attackDamageUpgradeCost;
        }

        // 상점 내 자원도 여기서 한번 더 갱신
        if (shopResourceText != null)
        {
            shopResourceText.text = $"현재 자원: {_playerDataCache.currentResources}";
        }
    }


    // 업그레이드 버튼 핸들러들
    public void OnUpgradeMoveSpeedButtonPressed()
    {
        if (_playerDataCache != null && _playerDataCache.SpendResources(_moveSpeedUpgradeCost))
        {
            _playerDataCache.IncreaseMoveSpeed(_moveSpeedUpgradeAmount);
            //UpdateShopStatsUI(); // PlayerData의 Increase 메서드에서 호출하도록 변경
            // 비용 증가 로직 
            // _moveSpeedUpgradeCost = Mathf.CeilToInt(_moveSpeedUpgradeCost * 1.2f);
        }
        else
        {
            Debug.Log("이동 속도 업그레이드 실패: 자원 부족 또는 PlayerData 없음");
            // 자원 부족 알림 UI 표시 가능
        }
    }

    public void OnUpgradeAttackDamageButtonPressed()
    {
        if (_playerDataCache != null && _playerDataCache.SpendResources(_attackDamageUpgradeCost))
        {
            _playerDataCache.IncreaseAttackDamage(_attackDamageUpgradeAmount);
            //UpdateShopStatsUI(); // PlayerData의 Increase 메서드에서 호출하도록 변경
            // 비용 증가 로직
            // _attackDamageUpgradeCost = Mathf.CeilToInt(_attackDamageUpgradeCost * 1.2f);
        }
        else
        {
            Debug.Log("공격력 업그레이드 실패: 자원 부족 또는 PlayerData 없음");
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
        HideShopPanel();
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
