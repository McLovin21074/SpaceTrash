using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int baseHp = 1000;
    [SerializeField] private int baseDamage = 12;
    [SerializeField] private float baseMoveSpeed = 3.5f;

    [Header("Melee")]
    [SerializeField] private float contactDamageCooldown = 0.5f;
    [SerializeField] private string playerTag = "Player";

    private Transform player;
    private NavMeshAgent agent;
    private Health health;
    private float nextDamageTime;

    private int currentDamage;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();

        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }

        // Initialize stats
        health.SetMaxFromSO(Mathf.Max(1, baseHp));
        currentDamage = Mathf.Max(1, baseDamage);

        if (agent != null)
        {
            agent.speed = Mathf.Max(0.1f, baseMoveSpeed);
            // For 2D navmesh setups we typically don't want Y rotation/3D adjustments
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        // Find player by tag or component
        var playerGO = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGO != null)
        {
            player = playerGO.transform;
        }
        else
        {
            var p = FindObjectOfType<Player>();
            if (p != null) player = p.transform;
        }

        if (health != null)
        {
            health.onDeath.AddListener(OnDeath);
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDeath.RemoveListener(OnDeath);
        }
    }

    private void Update()
    {
        if (player == null || agent == null) return;

        agent.SetDestination(player.position);
    }

private void TryDamage(GameObject go) {
    if (Time.time < nextDamageTime) return;
    var h = go.GetComponent<Health>();
    if (h != null) {
        h.TakeDamage(currentDamage);
        nextDamageTime = Time.time + contactDamageCooldown;
        Debug.Log($"Slime hit {go.name} for {currentDamage}. Target HP now: {h.Current}");
    }
}

private void OnTriggerStay2D(Collider2D other) => TryDamage(other.gameObject);
private void OnCollisionStay2D(Collision2D col) => TryDamage(col.collider.gameObject);

    private void OnDeath()
    {
        Destroy(gameObject);
    }

    // Public API for wave scaling or runtime tuning
    public void SetStats(int hp, int damage, float moveSpeed)
    {
        baseHp = Mathf.Max(1, hp);
        baseDamage = Mathf.Max(1, damage);
        baseMoveSpeed = Mathf.Max(0.1f, moveSpeed);

        if (health != null) health.SetMaxFromSO(baseHp);
        currentDamage = baseDamage;
        if (agent != null) agent.speed = baseMoveSpeed;
    }

    public void ApplyWaveMultipliers(float hpMul, float dmgMul, float spdMul)
    {
        int newHp = Mathf.Max(1, Mathf.RoundToInt(baseHp * Mathf.Max(0.01f, hpMul)));
        int newDmg = Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(0.01f, dmgMul)));
        float newSpd = Mathf.Max(0.1f, baseMoveSpeed * Mathf.Max(0.01f, spdMul));
        SetStats(newHp, newDmg, newSpd);
    }
}
