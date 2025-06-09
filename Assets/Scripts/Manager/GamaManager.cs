using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Scene Names")]
    public string titleSceneName = "Title";
    public string playerReadySceneName = "PlayerReady";
    public string gameplaySceneName = "MainScene";
    public string resultsSceneName = "Result";

    public WeaponData SelectedWeapon { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetSelectedWeapon(WeaponData weapon)
    {
        SelectedWeapon = weapon;
        Debug.Log($"[{gameObject.name}] 선택무기: {weapon.weaponName}");
    }

    /// <summary>
    /// 씬 로드
    /// </summary>
    public void LoadTitleScene()
    {
        SceneManager.LoadScene(titleSceneName);
        Debug.Log($"Loading Scene: {titleSceneName}");
    }

    public void LoadPlayerReadyScene()
    {
        SceneManager.LoadScene(playerReadySceneName);
        Debug.Log($"Loading Scene: {playerReadySceneName}");
    }

    public void LoadGameplayScene()
    {
        if (SelectedWeapon == null)
        {
            Debug.LogError("무기가 선택되지 않음.");
            return;
        }
        
        SceneManager.LoadScene(gameplaySceneName);
        Debug.Log($"Loading Scene: {gameplaySceneName}");
    }

    public void LoadResultsScene()
    {
        SceneManager.LoadScene(resultsSceneName);
        Debug.Log($"Loading Scene: {resultsSceneName}");
    }

    // TODO:무기 데이터 설정 
    // public void SetSelectedWeapon(WeaponData weapon)
    // {
    //     SelectedWeapon = weapon;
    //     Debug.Log($"GameManager: Weapon '{weapon.weaponName}' selected.");
    // }
}