using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResultsUIHandler : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button backToTitleButton;

    void Start()
    {
        if (scoreText == null || backToTitleButton == null)
        {
            Debug.LogError("ResultsUIHandler에 UI 요소들이 연결되지 않았습니다!");
            return;
        }

        DisplayScore();
        
        backToTitleButton.onClick.AddListener(LoadTitleScene);
        // 마우스 커서 설정
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// GameManager로부터 최종 기록을 가져와 UI에 표시
    /// </summary>
    void DisplayScore()
    {
        if (GameManager.Instance != null)
        {
            int finalStage = GameManager.Instance.LastClearedStage;
            scoreText.text = $"최종 기록: STAGE {finalStage}";
        }
        else
        {
            scoreText.text = "기록을 불러올 수 없습니다.";
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 타이틀 씬으로 돌아가기
    /// </summary>
    void LoadTitleScene()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadTitleScene();
        }
    }
}
