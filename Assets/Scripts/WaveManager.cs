using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class WaveManager : MonoBehaviour
{
    [Serializable]
    private class WaveEnemyEntry
    {
        public GameObject prefab;
        [Min(1)] public int unlockWave = 1;
    }

    public static WaveManager Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] private List<WaveEnemyEntry> enemyPrefabs = new();
    [SerializeField] private Transform enemiesParent;
    [SerializeField] private string playerTag = "Player";

    [Header("Wave settings")]
    [SerializeField, Min(1)] private int baseEnemiesPerWave = 4;
    [SerializeField, Min(0f)] private float enemiesPerWaveGrowth = 1.5f;
    [SerializeField, Min(0f)] private float intermissionDuration = 6f;
    [SerializeField, Min(0f)] private float spawnRadiusMin = 6f;
    [SerializeField, Min(0f)] private float spawnRadiusMax = 12f;
    [SerializeField, Min(0.01f)] private float navMeshSampleRadius = 2f;

    [Header("Scaling every N waves")]
    [SerializeField, Min(1)] private int wavesPerStatIncrease = 3;
    [SerializeField] private float hpBonusPerStep = 0.15f;
    [SerializeField] private float damageBonusPerStep = 0.1f;
    [SerializeField] private float speedBonusPerStep = 0.05f;

    public event Action<int> WaveStarted;
    public event Action<int> WaveCompleted;
    public event Action<float> IntermissionTick;

    private readonly Dictionary<Health, UnityAction> deathHandlers = new();
    private readonly List<GameObject> spawnCandidates = new();
    private Transform player;
    private Coroutine intermissionRoutine;
    private int currentWave;
    private int enemiesRemaining;
    private float intermissionRemaining;
    private bool waveRunning;

    public int CurrentWave => currentWave;
    public int EnemiesRemaining => enemiesRemaining;
    public bool WaveActive => waveRunning;
    public bool InIntermission => intermissionRoutine != null;
    public float IntermissionRemaining => intermissionRemaining;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        CleanupTrackedEnemies();
    }

    private IEnumerator Start()
    {
        yield return WaitForPlayer();
        StartNextWave();
    }

    private IEnumerator WaitForPlayer()
    {
        while (!TryFindPlayer()) yield return null;
    }

    private bool TryFindPlayer()
    {
        if (player && player.gameObject.activeInHierarchy) return true;

        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go)
        {
            player = go.transform;
            return true;
        }

        var p = FindFirstObjectByType<Player>();
        if (p)
        {
            player = p.transform;
            return true;
        }

        return false;
    }

    public void ForceStartNextWave()
    {
        if (waveRunning)
        {
            Debug.LogWarning("[WaveManager] Cannot force start new wave while current wave is running.");
            return;
        }

        if (intermissionRoutine != null)
        {
            StopCoroutine(intermissionRoutine);
            intermissionRoutine = null;
        }

        StartNextWave();
    }

    private void StartNextWave()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("[WaveManager] No enemy prefabs configured.");
            return;
        }

        intermissionRoutine = null;
        intermissionRemaining = 0f;

        currentWave++;
        enemiesRemaining = 0;
        waveRunning = true;

        WaveStarted?.Invoke(currentWave);

        int spawnCount = CalculateEnemyCount(currentWave);
        Vector3 playerPos = player ? player.position : transform.position;

        var multipliers = CalculateStatMultipliers(currentWave);

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnEnemy(currentWave, playerPos, multipliers);
        }

        if (enemiesRemaining == 0)
        {
            HandleWaveCleared();
        }
    }

    private void SpawnEnemy(int wave, Vector3 origin, (float hp, float dmg, float spd) multipliers)
    {
        GameObject prefab = PickEnemyPrefab(wave);
        if (!prefab)
        {
            Debug.LogWarning("[WaveManager] No prefab unlocked for this wave.");
            return;
        }

        Vector3 spawnPos = FindSpawnPosition(origin);
        GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity, enemiesParent);

        if (!instance)
            return;

        enemiesRemaining++;

        var ai = instance.GetComponent<EnemyAI>();
        if (ai)
            ai.ApplyWaveMultipliers(multipliers.hp, multipliers.dmg, multipliers.spd);

        var health = instance.GetComponent<Health>();
        if (health)
            RegisterEnemy(health);
    }

    private GameObject PickEnemyPrefab(int wave)
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return null;

        spawnCandidates.Clear();
        GameObject fallback = null;
        int fallbackWave = int.MaxValue;

        foreach (var entry in enemyPrefabs)
        {
            if (entry == null || entry.prefab == null) continue;

            int unlock = Mathf.Max(1, entry.unlockWave);
            if (wave >= unlock)
            {
                spawnCandidates.Add(entry.prefab);
            }
            else if (unlock < fallbackWave)
            {
                fallbackWave = unlock;
                fallback = entry.prefab;
            }
        }

        if (spawnCandidates.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, spawnCandidates.Count);
            return spawnCandidates[index];
        }

        return fallback;
    }

    private void RegisterEnemy(Health health)
    {
        if (!health || deathHandlers.ContainsKey(health)) return;

        UnityAction handler = null;
        handler = () =>
        {
            health.onDeath.RemoveListener(handler);
            deathHandlers.Remove(health);
            OnEnemyKilled();
        };

        deathHandlers.Add(health, handler);
        health.onDeath.AddListener(handler);
    }

    private void OnEnemyKilled()
    {
        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        if (enemiesRemaining == 0 && waveRunning)
            HandleWaveCleared();
    }

    private void HandleWaveCleared()
    {
        waveRunning = false;
        CleanupTrackedEnemies();

        if (MetaProgression.Instance)
            MetaProgression.Instance.ReportWaveCleared(currentWave);

        WaveCompleted?.Invoke(currentWave);

        if (intermissionDuration > 0f)
        {
            intermissionRoutine = StartCoroutine(IntermissionCountdown());
        }
        else
        {
            StartNextWave();
        }
    }

    private IEnumerator IntermissionCountdown()
    {
        intermissionRemaining = intermissionDuration;
        while (intermissionRemaining > 0f)
        {
            IntermissionTick?.Invoke(intermissionRemaining);
            yield return null;
            intermissionRemaining = Mathf.Max(0f, intermissionRemaining - Time.deltaTime);
        }
        IntermissionTick?.Invoke(0f);
        StartNextWave();
    }

    private int CalculateEnemyCount(int wave)
    {
        float count = baseEnemiesPerWave + (wave - 1) * enemiesPerWaveGrowth;
        return Mathf.Max(1, Mathf.RoundToInt(count));
    }

    private (float hp, float dmg, float spd) CalculateStatMultipliers(int wave)
    {
        if (wavesPerStatIncrease <= 0)
            return (1f, 1f, 1f);

        int steps = Mathf.Max(0, (wave - 1) / wavesPerStatIncrease);
        float hpMul = 1f + steps * hpBonusPerStep;
        float dmgMul = 1f + steps * damageBonusPerStep;
        float spdMul = 1f + steps * speedBonusPerStep;
        return (Mathf.Max(0.01f, hpMul), Mathf.Max(0.01f, dmgMul), Mathf.Max(0.01f, spdMul));
    }

    private Vector3 FindSpawnPosition(Vector3 origin)
    {
        float minRadius = Mathf.Min(spawnRadiusMin, spawnRadiusMax);
        float maxRadius = Mathf.Max(spawnRadiusMin, spawnRadiusMax);

        if (maxRadius <= 0f)
            return origin;

        minRadius = Mathf.Max(0f, minRadius);
        if (Mathf.Approximately(maxRadius, 0f))
            maxRadius = 0.1f;

        const int maxAttempts = 8;
        float z = origin.z;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 dir = UnityEngine.Random.insideUnitCircle;
            if (dir == Vector2.zero) dir = Vector2.up;
            dir.Normalize();

            float distance = UnityEngine.Random.Range(Mathf.Max(0.1f, minRadius), Mathf.Max(0.1f, maxRadius));
            Vector3 candidate = origin + new Vector3(dir.x, dir.y, 0f) * distance;
            candidate.z = z;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                return new Vector3(hit.position.x, hit.position.y, z);
        }

        Vector2 fallbackDir2D = UnityEngine.Random.insideUnitCircle;
        if (fallbackDir2D == Vector2.zero)
            fallbackDir2D = Vector2.one;
        fallbackDir2D.Normalize();

        float fallbackDist = Mathf.Lerp(Mathf.Max(0.1f, minRadius), Mathf.Max(0.1f, maxRadius), 0.5f);
        Vector3 fallback = origin + new Vector3(fallbackDir2D.x, fallbackDir2D.y, 0f) * fallbackDist;
        fallback.z = z;
        return fallback;
    }

    private void CleanupTrackedEnemies()
    {
        if (deathHandlers.Count == 0) return;

        foreach (var kvp in deathHandlers)
        {
            if (kvp.Key)
                kvp.Key.onDeath.RemoveListener(kvp.Value);
        }
        deathHandlers.Clear();
    }
}