using UnityEngine;
using UnityEngine.AI;

public class EnemyShooterAI : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int    baseHp        = 5;
    [SerializeField] private float  baseMoveSpeed = 3.5f;

    [Header("Targeting")]
    [SerializeField] private string playerTag     = "Player";
    [SerializeField] private float  desiredRange  = 4f;   // на таком расстоянии держимся
    [SerializeField] private float  reengageDelta = 0.6f; // гистерезис, чтобы не дёргался на грани дистанции

    [Header("Shooting")]
    [SerializeField] private float     fireRate     = 1.25f;
    [SerializeField] private int       bulletDamage = 1;
    [SerializeField] private float     bulletSpeed  = 10f;
    [SerializeField] private float     bulletRange  = 10f;
    [SerializeField] private float     bulletSize   = 1f;
    [SerializeField] private int       bulletCount  = 1;
    [SerializeField] private float     spreadDeg    = 0f;
    [SerializeField] private Transform firePoint;                // точка спавна (снаружи коллайдера!)
    [SerializeField] private BulletPool enemyBulletPool;         // ПУЛ ДОЛЖЕН БЫТЬ ДЛЯ ВРАГА

    [Header("Line of Sight")]
    [SerializeField] private LayerMask losObstacles;             // что мешает обзору
    [SerializeField] private bool      requireLineOfSight = true;

    private Transform     player;
    private NavMeshAgent  agent;
    private Health        health;
    private float         shootCooldown;

private void Awake()
{
    agent  = GetComponent<NavMeshAgent>();
    health = GetComponent<Health>() ?? gameObject.AddComponent<Health>();

    // Статы
    health.SetMaxFromSO(Mathf.Max(1, baseHp));

    // Настройка NavMeshAgent для 2D
    if (agent != null)
    {
        agent.speed            = Mathf.Max(0.1f, baseMoveSpeed);
        agent.updateRotation   = false;
        agent.updateUpAxis     = false;
        agent.stoppingDistance = Mathf.Max(0f, desiredRange * 0.9f);
    }

    // Поиск игрока
    var go = GameObject.FindGameObjectWithTag(playerTag);
    if (go != null) player = go.transform;
    else            player = FindFirstObjectByType<Player>()?.transform;

    // Подписка на смерть
    health.onDeath.AddListener(OnDeath);

    // Разруливание пула пуль врага
    if (enemyBulletPool == null)
    {
        // 1) попробуем найти на сцене объект с тегом EnemyBulletPool
        var poolGO = GameObject.FindWithTag("EnemyBulletPool");
        if (poolGO) enemyBulletPool = poolGO.GetComponent<BulletPool>();

        // 2) запасной вариант — первый созданный пул (если используешь синглтон)
        if (enemyBulletPool == null) enemyBulletPool = BulletPool.Instance;

        if (enemyBulletPool == null)
            Debug.LogWarning("[EnemyShooterAI] EnemyBulletPool not found — стрелять не смогу.");
    }
}


    private void OnDestroy()
    {
        if (health != null) health.onDeath.RemoveListener(OnDeath);
    }

    private void Update()
    {
        if (!player || !agent) return;

        // Двигаемся к игроку, но не вплотную
        agent.SetDestination(player.position);

        float dist = Vector2.Distance(transform.position, player.position);

        // Легкий гистерезис, чтобы агент не "дребезжал"
        if (dist <= desiredRange - reengageDelta) agent.isStopped = true;
        else if (dist >= desiredRange + reengageDelta) agent.isStopped = false;

        // Стрельба по кулдауну
        if (shootCooldown > 0f) shootCooldown -= Time.deltaTime;

        if (shootCooldown <= 0f && CanShoot(dist))
        {
            ShootAt(player.position);
            shootCooldown = 1f / Mathf.Max(0.01f, fireRate);
        }
    }

    private bool CanShoot(float distanceToPlayer)
    {
        if (enemyBulletPool == null) return false;

        // Не стреляем, если слишком близко — пусть чуть отойдёт
        if (distanceToPlayer < 0.5f) return false;

        if (!requireLineOfSight) return true;

        Vector3 origin = firePoint ? (Vector3)firePoint.position : transform.position;
        var hit = Physics2D.Linecast(origin, player.position, losObstacles);
        return hit.collider == null; // чистая линия — можно стрелять
    }

    private void ShootAt(Vector3 worldTarget)
    {
        if (enemyBulletPool == null) return;

        Vector3 origin = firePoint ? (Vector3)firePoint.position : transform.position;
        Vector2 to     = (worldTarget - origin).normalized;

        // базовый угол
        float baseAngle   = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
        float totalSpread = spreadDeg * (Mathf.Max(1, bulletCount) - 1);
        float startAngle  = baseAngle - totalSpread * 0.5f;

        for (int i = 0; i < Mathf.Max(1, bulletCount); i++)
        {
            float a   = startAngle + i * spreadDeg;
            Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));

            Bullet b = enemyBulletPool.Get();
            b.transform.position = origin;
            b.Fire(d, bulletSpeed, bulletRange, bulletDamage, bulletSize);
        }
    }

    private void OnDeath()
    {
        Destroy(gameObject);
    }

    // Для волн/баланса
    public void SetStats(int hp, float moveSpeed, float newFireRate)
    {
        baseHp = Mathf.Max(1, hp);
        baseMoveSpeed = Mathf.Max(0.1f, moveSpeed);
        fireRate = Mathf.Max(0.01f, newFireRate);

        health.SetMaxFromSO(baseHp);
        if (agent) agent.speed = baseMoveSpeed;
    }
}
