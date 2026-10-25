using UnityEngine;

// InputSystem 只记录输入
// 不做逻辑
public class InputSystem : ISystem
{
    private GameInputActions _inputActions;

    private bool _isPlayerControlEnabled = true;   // 玩家操作是否允许

    // 系统初始化时调用
    public InputSystem()
    {
        _inputActions = new GameInputActions();
        _inputActions.Player.Enable();
        SetCursorLock(true);
    }

    public void Update(World world, float deltaTime)
    {
        Vector2 moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
        Vector2 lookInput = _inputActions.Player.Look.ReadValue<Vector2>();

        bool attackPressed = _inputActions.Player.Attack.WasPressedThisFrame();
        bool skillPressed = _inputActions.Player.Skill.WasPressedThisFrame();

        bool altPressed = _inputActions.Player.Alt.WasPressedThisFrame();
        bool backpackPressed = _inputActions.Player.BackpackPanel.WasCompletedThisFrame();
        bool characterPressed = _inputActions.Player.CharactorPanel.WasCompletedThisFrame();

        // Alt 是全局输入，只能在这里切换一次，不能放进 entity 循环
        if (altPressed)
        {
            _isPlayerControlEnabled = !_isPlayerControlEnabled;
            SetCursorLock(_isPlayerControlEnabled);
        }

        bool isMainPanelOpen = UIManager.Instance != null && UIManager.Instance.IsAnyMainPanelOpen();
        bool willOpenMainPanelThisFrame = backpackPressed || characterPressed;

        bool shouldBlockPlayerInput =
            isMainPanelOpen ||
            willOpenMainPanelThisFrame ||
            !_isPlayerControlEnabled;

        // 暂停玩家输入
        if (shouldBlockPlayerInput)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            attackPressed = false;
            skillPressed = false;
        }

        Debug.Log("PlayerControlEnabled: " + _isPlayerControlEnabled);

        foreach (var entity in world.entities)
        {
            if (!entity.Has<InputComponent>())
                continue;

            var inputComp = entity.Get<InputComponent>();

            inputComp.MoveInput = moveInput;
            inputComp.LookInput = lookInput;
            inputComp.Attack = attackPressed;
            inputComp.Skill = skillPressed;
            inputComp.MouseLook = altPressed;
            inputComp.BackPackPanel = backpackPressed;
            inputComp.CharactorPanel = characterPressed;

            entity.Add(inputComp);

            if (entity.Has<MoveIntentComponent>())
            {
                var moveIntent = entity.Get<MoveIntentComponent>();
                moveIntent.MoveInput = inputComp.MoveInput;
                entity.Add(moveIntent);
            }
        }
    }

    private void SetCursorLock(bool lockCursor)
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // 系统销毁时调用（极其重要，防止内存泄漏）
    public void Destroy()
    {
        if (_inputActions != null)
        {
            _inputActions.Player.Disable(); // 禁用输入
            _inputActions.Dispose();        // 释放非托管资源
        }
    }
}