using TMPro;
using UnityEngine;

/// <summary>
/// µ¥¸öÉËº¦°××Ö
/// </summary>
public class DamageNumberPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float riseSpeed = 1.2f;
    [SerializeField] private float randomHorizontalOffset = 0.25f;

    private float timer;
    private Vector3 moveDirection;

    public void Initialize(int damage)
    {
        if (text != null)
        {
            text.text = damage.ToString();
            text.color = Color.white;
        }

        timer = lifetime;

        Vector2 random = Random.insideUnitCircle * randomHorizontalOffset;
        moveDirection = new Vector3(random.x, 1f, random.y).normalized;
    }

    private void Update()
    {
        transform.position += moveDirection * riseSpeed * Time.deltaTime;

        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.forward = cam.transform.forward;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }
}