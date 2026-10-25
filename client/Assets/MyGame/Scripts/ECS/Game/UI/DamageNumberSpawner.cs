using UnityEngine;

/// <summary>
/// 世界伤害白字生成器
/// 场景里挂一个即可
/// </summary>
public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance { get; private set; }

    [SerializeField] private DamageNumberPopup popupPrefab;
    [SerializeField] private float VerticalOffset = -0.2f;
    [SerializeField] private float randomHorizontalOffset = 0.25f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void Spawn(int damage, Vector3 worldPosition)
    {
        if (Instance == null || Instance.popupPrefab == null)
            return;

        Vector2 random = Random.insideUnitCircle * Instance.randomHorizontalOffset;
        Vector3 newPos = new Vector3(random.x, Instance.VerticalOffset, random.y);  

        var popup = Instantiate(
            Instance.popupPrefab,
            worldPosition + newPos,
            Quaternion.identity);

        popup.Initialize(damage);
    }
}