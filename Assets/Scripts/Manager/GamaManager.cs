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

    // TODO:나중에 선택된 무기 데이터를 저장할 변수
    // public WeaponData SelectedWeapon { get; private set; }

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
        // TODO: 무기 선택이 완료되었는지 확인하는 로직 추가
        // if (SelectedWeapon == null)
        // {
        //     Debug.LogError("무기가 선택되지 않았습니다! 무기 선택 씬으로 돌아갑니다.");
        //     LoadWeaponSelectScene();
        //     return;
        // }
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