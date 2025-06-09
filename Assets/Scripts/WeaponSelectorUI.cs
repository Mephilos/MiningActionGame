using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectorUI : MonoBehaviour
{
    public Button startGameButton;
    public Toggle weaponToggle1;
    public Toggle weaponToggle2;
    public Toggle weaponToggle3;

    public WeaponData weaponDataForToggle1;
    public WeaponData weaponDataForToggle2;
    public WeaponData weaponDataForToggle3;

    private WeaponData _chosenWeaponData;

    void Start()
    {
        if (weaponToggle1 != null)
        {
            weaponToggle1.onValueChanged.AddListener((isOn) => { if (isOn) OnToggleSelected(weaponDataForToggle1, "무기1"); });
        }
        if (weaponToggle2 != null)
        {
            weaponToggle2.onValueChanged.AddListener((isOn) => { if (isOn) OnToggleSelected(weaponDataForToggle2, "무기2"); });
        }
        if (weaponToggle3 != null)
        {
            weaponToggle3.onValueChanged.AddListener((isOn) => { if (isOn) OnToggleSelected(weaponDataForToggle3, "무기3"); });
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(ConfirmSelectionAndStartGame);
        }
        
        // 초기 선택된 톡들에 대한 처리
        if (weaponToggle1 != null && weaponToggle1.isOn)
        {
            OnToggleSelected(weaponDataForToggle1, "무기1");
        }
        else if (weaponToggle2 != null && weaponToggle2.isOn)
        {
            OnToggleSelected(weaponDataForToggle2, "무기2");
        }
        else if (weaponToggle3 != null && weaponToggle3.isOn)
        {
            OnToggleSelected(weaponDataForToggle3, "무기3");
        }
        else
        {
            // 아무것도 선택되지 않았을 경우, 첫 번째 무기를 기본으로 선택
            if (weaponToggle1 != null)
            {
                weaponToggle1.isOn = true;
                OnToggleSelected(weaponDataForToggle1, "무기1");
            }
        }
    }
    void OnToggleSelected(WeaponData selectedData, string weaponNameForDebug)
    {
        _chosenWeaponData = selectedData;
        if (_chosenWeaponData != null)
        {
            Debug.Log($"토글 선택됨: {_chosenWeaponData.weaponName}");
        }
        else
        {
            Debug.LogWarning($"{weaponNameForDebug}에 연결된 WeaponData가 없습니다.");
        }
    }
    public void ConfirmSelectionAndStartGame()
    {
        if (_chosenWeaponData == null)
        {
            Debug.LogError("선택된 무기가 없습니다!");
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSelectedWeapon(_chosenWeaponData);
            
            GameManager.Instance.LoadGameplayScene();
        }
        else
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다");
        }
    }
}
