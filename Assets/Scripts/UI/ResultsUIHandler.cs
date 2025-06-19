using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
public class ResultsUIHandler : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button backToTitleButton;

    [SerializeField] private List<TMP_Text> rankingTexts;
    void Start()
    {
        if (scoreText == null || backToTitleButton == null)
        {
            Debug.LogError("ResultsUIHandler에 UI 요소들이 연결되지 않았습니다!");
            return;
        }

        DisplayScore();
        DisplayRanking();
        
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
            scoreText.text = "기록을 불러올 수 없음";
            Debug.LogError("GameManager 인스턴스가 없음");
        }
    }
    /// <summary>
    /// 전체 랭킹을 UI에 표시
    /// </summary>
    void DisplayRanking()
    {
        if (GameManager.Instance != null && GameManager.Instance.Leaderboard != null)
        {
            List<int> highScores = GameManager.Instance.Leaderboard.HighScores;

            for (int i = 0; i < rankingTexts.Count; i++)
            {
                if (i < highScores.Count && highScores[i] > 0)
                {
                    rankingTexts[i].text = $"{i + 1}위: STAGE {highScores[i]}";
                }
                else
                {
                    rankingTexts[i].text = $"{i + 1}위: -";
                }
            }
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
