using UnityEngine;

public class UIPlatformHelper : MonoBehaviour
{
    public GameObject mobileControlsRoot; // 조이스틱, 모바일 버튼들의 부모 오브젝트
    public GameObject pcSpecificUIRoot;   // PC 전용으로 보여줄 UI

    void Awake()
    {
#if UNITY_IOS || UNITY_ANDROID // 모바일 플랫폼
        if (mobileControlsRoot != null) mobileControlsRoot.SetActive(true);
        if (pcSpecificUIRoot != null) pcSpecificUIRoot.SetActive(false);
#elif UNITY_STANDALONE || UNITY_EDITOR // PC 또는 에디터
        
        if (Application.isEditor) // 에디터 셋팅
        {
            if (mobileControlsRoot != null) mobileControlsRoot.SetActive(false);
        }
        else // 실제 PC 빌드
        {
            if (mobileControlsRoot != null) mobileControlsRoot.SetActive(false);
        }
        if (pcSpecificUIRoot != null) pcSpecificUIRoot.SetActive(true);
#endif
    }
}