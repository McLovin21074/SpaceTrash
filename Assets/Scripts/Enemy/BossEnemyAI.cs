using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Boss behaviour that combines melee pursuit, heavy projectile attacks, and periodic minion summoning.
/// Designed so an existing enemy prefab can swap its AI script for this one with minimal setup.
/// </summary>
[RequireComponent(typeof(Health))]
public class BossEnemyAI : MonoBehaviour
{
    public static event Action<BossEnemyAI> OnBossSpawned;
    public static event Action<BossEnemyAI> OnBossDefeated;

    [Header("Core Stats")]
    [SerializeField] private int baseHp = 600;
    [SerializeField] private int contactDamage = 20;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float contactDamageCooldown = 1.0f;

    [Header("Targeting")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float stopDistance = 1.5f;

    [Header("Shooting")]
    [SerializeField, Min(0.1f)] private float fireInterval = 3.5f;
    [SerializeField] private int bulletDamage = 12;
    [SerializeField] private float bulletSpeed = 4f;
    [SerializeField] private float bulletRange = 10f;
    [SerializeField] private float bulletSize = 2.5f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private BulletPool enemyBulletPool;
    [SerializeField] private LayerMask losObstacles;
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Summoning")]
    [SerializeField, Tooltip("Prefab of the minion to summon (e.g. Slime)")]
    private GameObject slimePrefab;
    [SerializeField, Min(0.5f)] private float summonInterval = 6f;
    [SerializeField] private Vector2 summonRadius = new Vector2(1.5f, 3f);
    [SerializeField] private Vector2Int summonCountRange = new Vector2Int(1, 2);

    private Transform player;
    private NavMeshAgent agent;
    private Rigidbody2D body;
    private Health health;
    private float nextContactDamageTime;
    private float shootTimer;
    private float summonTimer;
    private bool spawnNotified;

    public Health Health => health;

    private void Awake()
    {
        BossFightManager.EnsureExists();

        agent = GetComponent<NavMeshAgent>();
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>() ?? gameObject.AddComponent<Health>();

        health.SetMaxFromSO(Mathf.Max(1, baseHp));
        health.onDeath.AddListener(HandleDeath);

        if (agent != null)
        {
            agent.speed = Mathf.Max(0.1f, moveSpeed);
            agent.acceleration = Mathf.Max(agent.acceleration, moveSpeed * 4f);
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.stoppingDistance = Mathf.Max(0f, stopDistance * 0.75f);
        }
        else
        {
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody2D>();
            }
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.linearDamping = Mathf.Max(body.linearDamping, 4f);
        }

        TryFindPlayer();
        EnsureBulletPool();

        fireInterval = Mathf.Max(0.1f, fireInterval);
        summonInterval = Mathf.Max(0.5f, summonInterval);
        shootTimer = UnityEngine.Random.Range(0f, fireInterval * 0.5f);
        summonTimer = UnityEngine.Random.Range(0f, summonInterval * 0.5f);
    }

    private void Start()
    {
        NotifySpawned();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDeath.RemoveListener(HandleDeath);
        }
    }

    private void Update()
    {
        if (!player)
        {
            TryFindPlayer();
        }

        HandleMovement();
        HandleShooting();
        HandleSummoning();
    }

    private void FixedUpdate()
    {
        if (agent != null) return; // NavMeshAgent handles its own movement in Update.
        if (!player || !body) return;

        Vector2 toPlayer = player.position - transform.position;
        if (toPlayer.sqrMagnitude <= stopDistance * stopDistance)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 desiredVel = toPlayer.normalized * moveSpeed;
        body.linearVelocity = Vector2.Lerp(body.linearVelocity, desiredVel, 0.2f);
    }

    private void HandleMovement()
    {
        if (agent == null || !player) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= stopDistance)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    private void HandleShooting()
    {
        if (enemyBulletPool == null || !player) return;

        shootTimer -= Time.deltaTime;
        if (shootTimer > 0f) return;

        Vector3 origin = firePoint ? firePoint.position : transform.position;
        Vector3 target = player.position;

        if (requireLineOfSight)
        {
            var hit = Physics2D.Linecast(origin, target, losObstacles);
            if (hit.collider != null)
            {
                shootTimer = Mathf.Min(fireInterval * 0.5f, 1.25f);
                return;
            }
        }

        Vector2 dir = (target - origin).normalized;
        Bullet bullet = enemyBulletPool.Get();
        bullet.transform.position = origin;
        bullet.Fire(dir, bulletSpeed, bulletRange, bulletDamage, bulletSize);

        shootTimer = fireInterval;
    }

    private void HandleSummoning()
    {
        if (slimePrefab == null) return;

        summonTimer -= Time.deltaTime;
        if (summonTimer > 0f) return;

        summonTimer = summonInterval;

        int min = Mathf.Max(1, summonCountRange.x);
        int max = Mathf.Max(min, summonCountRange.y);
        int count = UnityEngine.Random.Range(min, max + 1);

        float minRadius = Mathf.Max(0f, Mathf.Min(summonRadius.x, summonRadius.y));
        float maxRadius = Mathf.Max(minRadius + 0.1f, Mathf.Max(summonRadius.x, summonRadius.y));

        for (int i = 0; i < count; i++)
        {
            float radius = UnityEngine.Random.Range(minRadius, maxRadius);
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Vector3 spawnPos = transform.position + offset;
            Instantiate(slimePrefab, spawnPos, Quaternion.identity);
        }
    }

    private void TryFindPlayer()
    {
        var playerGO = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGO != null)
        {
            player = playerGO.transform;
            return;
        }

        var p = FindPlayerFallback();
        if (p != null)
        {
            player = p.transform;
        }
    }

    private Player FindPlayerFallback()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<Player>();
#else
        return FindObjectOfType<Player>();
#endif
    }

    private void EnsureBulletPool()
    {
        if (enemyBulletPool != null) return;

        var poolGO = GameObject.FindWithTag("EnemyBulletPool");
        if (poolGO)
        {
            enemyBulletPool = poolGO.GetComponent<BulletPool>();
        }

        if (enemyBulletPool == null)
        {
            enemyBulletPool = BulletPool.Instance;
        }

        if (enemyBulletPool == null)
        {
            Debug.LogWarning("[BossEnemyAI] Enemy bullet pool not found. Boss will skip shooting.");
        }
    }

    private void HandleDeath()
    {
        OnBossDefeated?.Invoke(this);
        spawnNotified = false;
        Destroy(gameObject);
    }

    private void NotifySpawned()
    {
        if (spawnNotified) return;
        spawnNotified = true;
        OnBossSpawned?.Invoke(this);
    }

    private void TryDamage(GameObject go)
    {
        if (Time.time < nextContactDamageTime) return;
        if (!go.CompareTag(playerTag)) return;

        var targetHealth = go.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(contactDamage);
            nextContactDamageTime = Time.time + contactDamageCooldown;
        }
    }

    private void OnTriggerStay2D(Collider2D other) => TryDamage(other.gameObject);
    private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider.gameObject);
}
