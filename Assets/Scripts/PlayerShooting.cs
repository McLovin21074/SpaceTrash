using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private PlayerSatatsSO stats;
    [SerializeField] private Transform firePoint; // если нужно стрелять не от игрока
    [SerializeField] private BulletPool pool;     // по умолчанию от инстанса пули

    private float cooldown;

    public PlayerSatatsSO Stats => stats;

    private void Awake()
    {
        if (pool == null) pool = BulletPool.Instance;
    }

    private void Update()
    {
        if (cooldown > 0f)
            cooldown -= Time.deltaTime;

        Vector2 shootDir = GameInput.Instance != null ? GameInput.Instance.GetShootingVector4() : Vector2.zero;
        if (shootDir == Vector2.zero) return;

        float fireInterval = 1f / Mathf.Max(0.01f, (stats != null ? stats.fireRate : 4f));
        if (cooldown > 0f) return;
        cooldown = fireInterval;

        Fire(shootDir);
    }

    private void Fire(Vector2 dir)
    {
        if (pool == null)
        {
            Debug.LogWarning("BulletPool not set in PlayerShooting and no instance found.");
            return;
        }

        int count = Mathf.Max(1, stats != null ? stats.bulletCount : 1);
        float spread = stats != null ? stats.spreadDeg : 0f;
        float speed = stats != null ? stats.bulletSpeed : 12f;
        float range = stats != null ? stats.bulletRange : 8f;
        int damage = stats != null ? stats.bulletDamage : 1;
        float size = stats != null ? stats.bulletSize : 1f;

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;


        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float totalSpread = spread * (count - 1);
        float startAngle = baseAngle - totalSpread * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + i * spread;
            Vector2 shotDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

            var b = pool.Get();
            b.transform.position = origin;
            b.Fire(shotDir, speed, range, damage, size);
        }
    }
}
