using UnityEngine;
using UnityEngine.UI;
public class TitleUIHandler : MonoBehaviour
{
    public Button startGameButton;

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
                Debug.LogError("TitleUIHandler: GameManager.Instance를 찾을 수 없습니다!");
            }
            if (startGameButton == null)
            {
                Debug.LogError("TitleUIHandler: startGameButton이 연결되지 않았습니다!");
            }
        }
    }
}
