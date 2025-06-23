using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CooldownDisplay : MonoBehaviour
{
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;

    void Start()
    {
        if (cooldownOverlay !=  null) cooldownOverlay.gameObject.SetActive(false);
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
    }
    public void UpdateDisplay(float remainingTime, float totalTime)
    {
        bool isCooldownActive = remainingTime > 0;
        if (cooldownOverlay != null && cooldownOverlay.gameObject.activeSelf != isCooldownActive)
        {
            cooldownOverlay.gameObject.SetActive(isCooldownActive);
        }

        if (cooldownText != null && cooldownText.gameObject.activeSelf != isCooldownActive)
        {
            cooldownText.gameObject.SetActive(isCooldownActive);
        }

        if (isCooldownActive)
        {
            if (cooldownOverlay != null && totalTime > 0)
            {
                cooldownOverlay.fillAmount = remainingTime / totalTime;
            }

            if (cooldownText != null)
            {
                cooldownText.text = remainingTime > 1f ?
                    Mathf.CeilToInt(remainingTime).ToString() : remainingTime.ToString("F1");
            }
        }
    }
}
