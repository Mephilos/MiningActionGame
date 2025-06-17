using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillCooldownUI : MonoBehaviour
{
    public GameObject playerObject;

    public Image skillIcon;
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;
    
    private PlayerData _playerData;
    private WeaponController _weaponController;
    private SkillData _currentSkillData;

    void Start()
    {
        if (playerObject == null)
        {
            Debug.LogError($"[{gameObject.name}] playerobject할당이 필요합니다");
            gameObject.SetActive(false);
            return;
        }
        
        _playerData = playerObject.GetComponent<PlayerData>();
        _weaponController = playerObject.GetComponent<WeaponController>();
        
        if (cooldownOverlay != null) cooldownOverlay.gameObject.SetActive(false);
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_playerData == null || _weaponController == null) return;
        
        UpdateSkillIcon();

        if (_currentSkillData == null)
        {
            if (cooldownOverlay.IsActive()) cooldownOverlay.gameObject.SetActive(false);
            if (cooldownText.IsActive()) cooldownText.gameObject.SetActive(false);
            return;
        }

        float remainingCooldown = _playerData.currentSkillCooldown;

        if (remainingCooldown > 0)
        {
            cooldownOverlay.gameObject.SetActive(true);
            cooldownText.gameObject.SetActive(true);
            
            float fillAmount = remainingCooldown / _currentSkillData.cooldown;
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

    private void UpdateSkillIcon()
    {
        SkillData weaponSkill = _weaponController.currentWeaponData.specialSkill;

        if (_currentSkillData != weaponSkill)
        {
            _currentSkillData = weaponSkill;
            if (_currentSkillData != null)
            {
                skillIcon.sprite = _currentSkillData.skillIcon;
                skillIcon.enabled = true;
            }
            else
            {
                skillIcon.enabled = false;
            }
        }
    }
}
