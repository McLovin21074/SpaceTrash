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

    [Header("Fallback spawn ring (used when no spawn points are set)")]
    [SerializeField, Min(0f)] private float spawnRadiusMin = 6f;
    [SerializeField, Min(0f)] private float spawnRadiusMax = 12f;
    [SerializeField, Min(0.01f)] private float navMeshSampleRadius = 2f;
    [Tooltip("Maximum random attempts when searching for a valid navmesh position.")]
    [SerializeField, Min(1)] private int spawnAttemptsPerEnemy = 16;

    [Header("Spawn points")]
    [Tooltip("Preferred world positions enemies should spawn at. Fill with scene transforms.")]
    [SerializeField] private List<Transform> enemySpawnPoints = new();
    [Tooltip("Adds a small random offset around each spawn point (0 = exactly the transform position).")]
    [SerializeField, Min(0f)] private float spawnPointJitterRadius = 0.75f;
    [Tooltip("Shuffle spawn points at the start of every wave.")]
    [SerializeField] private bool randomizeSpawnOrderEachWave = true;

    [Header("Intermission pickups")]
    [SerializeField] private GameObject medkitPrefab;
    [SerializeField] private Transform medkitParent;
    [SerializeField, Min(0)] private int medkitsMin = 1;
    [SerializeField, Min(0)] private int medkitsMax = 2;
    [SerializeField, Min(1)] private int medkitHealMin = 1;
    [SerializeField, Min(1)] private int medkitHealMax = 2;
    [SerializeField, Min(0f)] private float medkitSpawnRadiusMin = 2f;
    [SerializeField, Min(0f)] private float medkitSpawnRadiusMax = 8f;

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
    private readonly List<MedkitPickup> activeMedkits = new();
    private readonly Queue<Transform> spawnPointQueue = new();
    private readonly List<Transform> spawnPointBuffer = new();

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
        if (Instance == this)
            Instance = null;

        CleanupTrackedEnemies();
        CleanupMedkits();
    }

    private IEnumerator Start()
    {
        yield return WaitForPlayer();
        StartNextWave();
    }

    private IEnumerator WaitForPlayer()
    {
        while (!TryFindPlayer())
            yield return null;
    }

    private bool TryFindPlayer()
    {
        if (player && player.gameObject.activeInHierarchy)
            return true;

        GameObject tagged = GameObject.FindGameObjectWithTag(playerTag);
        if (tagged)
        {
            player = tagged.transform;
            return true;
        }

        Player found = FindFirstObjectByType<Player>();
        if (found)
        {
            player = found.transform;
            return true;
        }

        return false;
    }

    public void ForceStartNextWave()
    {
        if (waveRunning)
        {
            Debug.LogWarning("[WaveManager] Cannot force start new wave while the current wave is running.");
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
        CleanupMedkits();

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

        PrepareSpawnPointQueue();

        int spawnCount = CalculateEnemyCount(currentWave);
        Vector3 origin = player ? player.position : transform.position;
        var multipliers = CalculateStatMultipliers(currentWave);

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnEnemy(origin, multipliers);
        }

        if (enemiesRemaining == 0)
            HandleWaveCleared();
    }

    private void SpawnEnemy(Vector3 playerOrigin, (float hp, float dmg, float spd) multipliers)
    {
        GameObject prefab = PickEnemyPrefab(currentWave);
        if (!prefab)
            return;

        Vector3 spawnPos = GetNextEnemySpawnPosition(playerOrigin);
        GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity, enemiesParent);
        if (!instance)
            return;

        enemiesRemaining++;

        if (instance.TryGetComponent(out EnemyAI ai))
            ai.ApplyWaveMultipliers(multipliers.hp, multipliers.dmg, multipliers.spd);

        if (instance.TryGetComponent(out Health health))
            RegisterEnemy(health);
    }

    private GameObject PickEnemyPrefab(int wave)
    {
        spawnCandidates.Clear();
        GameObject fallback = null;
        int fallbackUnlock = int.MaxValue;

        foreach (WaveEnemyEntry entry in enemyPrefabs)
        {
            if (entry == null || entry.prefab == null)
                continue;

            int unlockWave = Mathf.Max(1, entry.unlockWave);
            if (wave >= unlockWave)
            {
                spawnCandidates.Add(entry.prefab);
            }
            else if (unlockWave < fallbackUnlock)
            {
                fallbackUnlock = unlockWave;
                fallback = entry.prefab;
            }
        }

        if (spawnCandidates.Count == 0)
            return fallback;

        int index = UnityEngine.Random.Range(0, spawnCandidates.Count);
        return spawnCandidates[index];
    }

    private void RegisterEnemy(Health health)
    {
        if (!health || deathHandlers.ContainsKey(health))
            return;

        UnityAction handler = OnEnemyKilled;
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
            SpawnIntermissionMedkits();
            intermissionRoutine = StartCoroutine(IntermissionCountdown());
        }
        else
        {
            StartNextWave();
        }
    }

    private void SpawnIntermissionMedkits()
    {
        if (!medkitPrefab)
            return;

        int minCount = Mathf.Max(0, Mathf.Min(medkitsMin, medkitsMax));
        int maxCount = Mathf.Max(minCount, Mathf.Max(medkitsMin, medkitsMax));
        if (maxCount <= 0)
            return;

        int spawnCount = UnityEngine.Random.Range(minCount, maxCount + 1);
        if (spawnCount <= 0)
            return;

        Vector3 origin = player ? player.position : transform.position;
        int healMin = Mathf.Max(1, Mathf.Min(medkitHealMin, medkitHealMax));
        int healMax = Mathf.Max(healMin, Mathf.Max(medkitHealMin, medkitHealMax));

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = FindNavMeshPosition(origin, medkitSpawnRadiusMin, medkitSpawnRadiusMax);
            GameObject instance = Instantiate(medkitPrefab, spawnPos, Quaternion.identity, medkitParent);
            if (!instance)
                continue;

            MedkitPickup pickup = instance.GetComponent<MedkitPickup>() ?? instance.AddComponent<MedkitPickup>();
            pickup.Configure(this, healMin, healMax);

            if (!instance.GetComponent<EnemyIndicatorTarget>())
                instance.AddComponent<EnemyIndicatorTarget>();

            activeMedkits.Add(pickup);
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

    public void NotifyMedkitRemoved(MedkitPickup pickup)
    {
        if (!pickup)
            return;

        activeMedkits.Remove(pickup);
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

    private Vector3 GetNextEnemySpawnPosition(Vector3 playerOrigin)
    {
        if (enemySpawnPoints != null && enemySpawnPoints.Count > 0)
        {
            if (spawnPointQueue.Count == 0)
                RefillSpawnPointQueue();

            if (spawnPointQueue.Count > 0)
            {
                Transform point = spawnPointQueue.Dequeue();
                if (point)
                {
                    Vector3 basePos = point.position;
                    if (spawnPointJitterRadius > 0f)
                    {
                        Vector2 jitter = UnityEngine.Random.insideUnitCircle * spawnPointJitterRadius;
                        basePos += new Vector3(jitter.x, jitter.y, 0f);
                    }

                    if (TryGetValidSpawnPoint(basePos, out Vector3 result))
                        return result;
                }
            }
        }

        // Fallback to ring sampling around the player
        return FindNavMeshPosition(playerOrigin, spawnRadiusMin, spawnRadiusMax);
    }

    private void PrepareSpawnPointQueue()
    {
        spawnPointQueue.Clear();
        RefillSpawnPointQueue();
    }

    private void RefillSpawnPointQueue()
    {
        spawnPointBuffer.Clear();

        if (enemySpawnPoints != null)
        {
            foreach (Transform point in enemySpawnPoints)
            {
                if (point)
                    spawnPointBuffer.Add(point);
            }
        }

        if (spawnPointBuffer.Count == 0)
            return;

        if (randomizeSpawnOrderEachWave)
            Shuffle(spawnPointBuffer);

        foreach (Transform point in spawnPointBuffer)
            spawnPointQueue.Enqueue(point);
    }

    private bool TryGetValidSpawnPoint(Vector3 candidate, out Vector3 result)
    {
        result = candidate;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            result = new Vector3(hit.position.x, hit.position.y, candidate.z);
            return true;
        }

        return false;
    }

    private Vector3 FindNavMeshPosition(Vector3 origin, float minRadius, float maxRadius)
    {
        float min = Mathf.Max(0f, Mathf.Min(minRadius, maxRadius));
        float max = Mathf.Max(min, Mathf.Max(minRadius, maxRadius));
        if (max <= 0f)
            return origin;

        int attempts = Mathf.Max(4, spawnAttemptsPerEnemy);
        float z = origin.z;

        for (int i = 0; i < attempts; i++)
        {
            Vector2 dir = UnityEngine.Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector2.up;
            dir.Normalize();

            float distance = UnityEngine.Random.Range(min, max);
            Vector3 candidate = origin + new Vector3(dir.x, dir.y, 0f) * distance;
            candidate.z = z;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                return new Vector3(hit.position.x, hit.position.y, z);
        }

        return origin;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void CleanupTrackedEnemies()
    {
        if (deathHandlers.Count == 0)
            return;

        foreach (var pair in deathHandlers)
        {
            if (pair.Key)
                pair.Key.onDeath.RemoveListener(pair.Value);
        }

        deathHandlers.Clear();
    }

    private void CleanupMedkits()
    {
        if (activeMedkits.Count == 0)
            return;

        foreach (MedkitPickup pickup in activeMedkits)
        {
            if (pickup)
                Destroy(pickup.gameObject);
        }

        activeMedkits.Clear();
    }
}
