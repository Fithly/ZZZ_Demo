using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient fillGradient;

    [Header("World Space Follow")]
    [SerializeField] private bool followTarget = false;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private bool faceCamera = true;

    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    // 如果是怪物 则实时跟随
    private void LateUpdate()
    {
        if (!followTarget || target == null)
            return;

        transform.position = target.position + worldOffset;

        if (faceCamera)
        {
            if (mainCam == null)
                mainCam = Camera.main;

            if (mainCam != null)
            {
                transform.forward = mainCam.transform.forward;
            }
        }
    }

    // 绑定目标
    public void BindTarget(Transform newTarget, Vector3 offset)
    {
        target = newTarget;
        worldOffset = offset;
        followTarget = newTarget != null;
    }

    // 设置血量
    public void SetHealth(int currentHP, int maxHP)
    {
        if (slider == null)
            return;

        maxHP = Mathf.Max(1, maxHP);
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        slider.maxValue = maxHP;
        slider.value = currentHP;

        if (fillImage != null)
        {
            float t = (float)currentHP / maxHP;
            fillImage.color = fillGradient.Evaluate(t);
        }
    }

    // 血条是否可见
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}