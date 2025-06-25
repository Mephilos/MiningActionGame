using UnityEngine;
using UnityEngine.UI;
public class TitleUIHandler : MonoBehaviour
{
    public Button startGameButton;
    public GameObject controlSheetPanel;

    void Start()
    {
        if (GameManager.Instance != null && startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(GameManager.Instance.LoadPlayerReadyScene);
        }
        else
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("TitleUIHandler: GameManager.Instance를 찾을 수 없음");
            }
            if (startGameButton == null)
            {
                Debug.LogError("TitleUIHandler: startGameButton 확인 필요");
            }
        }

        if (controlSheetPanel != null)
        {
            controlSheetPanel.SetActive(false);
        }
    }

    public void ShowControlSheetPanel()
    {
        if (controlSheetPanel != null)
        {
            controlSheetPanel.SetActive(true);
        }
    }

    public void HideControlSheetPanel()
    {
        if (controlSheetPanel != null)
        {
            controlSheetPanel.SetActive(false);
        }
    }
}
