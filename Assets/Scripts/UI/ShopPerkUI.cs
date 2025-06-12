using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPerkUI : MonoBehaviour
{
    public Image perkIcon;
    public TMP_Text perkNameText;
    public TMP_Text perkDescriptionText;
    public TMP_Text perkCostText;
    public Button buyButton;

    private PerkData _perkData;
    private UIManager _uiManager;

    public void Setup(PerkData data, UIManager manager)
    {
        _perkData = data;
        _uiManager = manager;

        perkIcon.sprite = data.icon;
        perkNameText.text = data.perkName;
        perkDescriptionText.text = data.description;
        perkCostText.text = $"Cost: {data.cost}";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyButtonPressed);

        UpdateInteractable();
    }

    public void UpdateInteractable()
    {
        if (_uiManager != null && _perkData != null)
        {
            buyButton.interactable = _uiManager.CanAfford(_perkData.cost);
        }
    }

    private void OnBuyButtonPressed()
    {
        if (_uiManager != null)
        {
            _uiManager.TryPurchasePerk(_perkData, this);
        }
    }
}
