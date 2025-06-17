using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashCooldownUI : MonoBehaviour
{
    public GameObject playerObject;

    public Image dashIcon;
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;
    
    private PlayerData _playerData;

    void Start()
    {
        if (playerObject == null)
        {
            Debug.LogError($"[{gameObject.name}] playerObject가 할당되지 않음");
            gameObject.SetActive(false);
            return;
        }
        _playerData = playerObject.GetComponent<PlayerData>();

        if (dashIcon.sprite != null)
        {
            dashIcon.enabled = true;
        }
        
        if (cooldownOverlay != null) cooldownOverlay.gameObject.SetActive(false);
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_playerData == null) return;

        float remainingCooldown = _playerData.dashCooldownTimer;
        float totalCooldown = _playerData.currentDashCooldown;

        if (remainingCooldown > 0)
        {
            cooldownOverlay.gameObject.SetActive(true);
            cooldownText.gameObject.SetActive(true);
            
            float fillAmount = remainingCooldown / totalCooldown;
            cooldownOverlay.fillAmount = fillAmount;
            
            cooldownText.text = remainingCooldown > 1f ? 
                Mathf.CeilToInt(remainingCooldown).ToString() : remainingCooldown.ToString("F1");
        }
        else
        {
            if (cooldownOverlay.gameObject.activeSelf) cooldownOverlay.gameObject.SetActive(false);
            if (cooldownText.gameObject.activeSelf) cooldownText.gameObject.SetActive(false);
        }
    }
}
