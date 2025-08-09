using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Mobile Controls")] 
    public Joystick movementJoystick;

    public Vector2 MoveInput { get; private set; }
    public bool IsJumpPressed { get; private set; }
    public bool IsDashButtonPressed { get; private set; }
    public bool IsAiming { get; private set; }
    public bool IsFirePressed { get; private set; }
    public bool IsFireReleased { get; private set; }
    public bool IsFireHeld { get; private set; }
    public bool IsSkillButtonPressed { get; private set; }
    public bool IsSkillButtonHeld { get; private set; }
    public bool IsSkillButtonReleased { get; private set; }
    public Vector2 MousePosition { get; private set; }

    void Update()
    {
        // 상태 설정
        IsJumpPressed = false;
        IsDashButtonPressed = false;
        IsFirePressed = false;
        IsFireReleased = false;
        IsSkillButtonPressed = false;
        IsSkillButtonReleased = false;

        // 플랫폼별 입력 처리
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        HandleStandaloneInput();
#elif UNITY_IOS || UNITY_ANDROID
        HandleMobileInput();
#endif
    }

    private void HandleStandaloneInput()
    {
        // 이동
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // 기타 액션
        if (Input.GetKeyDown(KeyCode.Space))
        {
            IsJumpPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            IsDashButtonPressed = true;
        }

        // 조준
        IsAiming = Input.GetMouseButton(1);

        // 발사 상태
        IsFirePressed = Input.GetMouseButtonDown(0);
        IsFireReleased = Input.GetMouseButtonUp(0);
        IsFireHeld = Input.GetMouseButton(0);

        // 스킬
        IsSkillButtonPressed = Input.GetKeyDown(KeyCode.Q);
        IsSkillButtonHeld = Input.GetKey(KeyCode.Q);
        IsSkillButtonReleased = Input.GetKeyUp(KeyCode.Q);
        
        // 마우스 인풋
        MousePosition = Input.mousePosition;
    }

    private void HandleMobileInput()
    {
        // 모바일 이동
        if (movementJoystick != null && movementJoystick.Direction.sqrMagnitude > 0.01f)
        {
            MoveInput = new Vector2(movementJoystick.Horizontal, movementJoystick.Vertical);
        }
        else
        {
            MoveInput = Vector2.zero;
        }
        
        MousePosition = new Vector2(Screen.width / 2, Screen.height / 2);
    }
    
    // 모바일 버튼 (임시)
    // TODO: 추후 모바일 조작 추가 예정
    public void OnJumpButtonPressed()
    {
        IsJumpPressed = true;
    }

    public void OnDashButtonPressed()
    {
        IsDashButtonPressed = true;
    }

    public void OnFireButtonDown()
    {
        IsFirePressed = true;
        IsFireHeld = true;
    }

    public void OnFireButtonUp()
    {
        IsFireReleased = true;
        IsFireHeld = false;
    }
    
    public void OnAimButtonDown()
    {
        IsAiming = true;
    }

    public void OnAimButtonUp()
    {
        IsAiming = false;
    }
}
