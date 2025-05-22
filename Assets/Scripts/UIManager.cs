using UnityEngine;
using TMPro;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("게임 플레이 UI")] [Tooltip("플레이어 텍스트 UI")]
    public TMP_Text playerHpText;
    public TMP_Text stageTimerText;
    public TMP_Text stageNumberText;
    public TMP_Text stageClearText;
    public TMP_Text playerResourceDisplayText;
    [Header("게임 오버 UI")] [Tooltip("게임 오버 시 활성화")]
    public GameObject gameOverPanel;
    
    [Header("스테이지 클리어UI")] [Tooltip("스테이지 클리어시 활성화")]
    public GameObject stageClearPanel;
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

    public void UpdateStageClearUI(int stageNum)
    {
        if (stageClearText != null)
        {
            stageClearText.text = $"{stageNum} Stage\nClear!";
        }
    }

    public void UpdateResourceDisplayUI(int currentResource)
    {
        if (playerResourceDisplayText != null)
        {
            playerResourceDisplayText.text = $"Resource: {currentResource} ";
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
