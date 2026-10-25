using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float mouseSensitivity = 150f;
    public float distance = 4f;
    public Vector2 pitchLimits = new Vector2(-10, 60);

    private float currentX = 0f;
    private float currentY = 20f;

    public Entity entity;

    void Start()
    {
        // 隐藏并锁定鼠标到屏幕中心
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        entity = GameEntry.Instance.world.CreateEntity();
        entity.Add(new InputComponent());
    }

    void LateUpdate()
    {
        if (entity == null) return;
        var inputComp = entity.Get<InputComponent>();

        currentX += inputComp.LookInput.x * mouseSensitivity * Time.deltaTime;
        currentY -= inputComp.LookInput.y * mouseSensitivity * Time.deltaTime;
        currentY = Mathf.Clamp(currentY, pitchLimits.x, pitchLimits.y);

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 direction = new Vector3(0, 0, -distance);

        // 相机位置 = 目标位置 + 旋转偏移
        transform.position = target.position + rotation * direction + Vector3.up * 1.5f;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}