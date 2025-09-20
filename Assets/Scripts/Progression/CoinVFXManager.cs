using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class CoinVFXManager : MonoBehaviour
{
    public static CoinVFXManager Instance { get; private set; }

    [Header("Prefab & Pool")]
    [SerializeField] private CoinVFX coinPrefab;
    [SerializeField] private int poolSize = 128;

    [Header("Motion")]
    [SerializeField] private float idleTimeMin = 0.02f;
    [SerializeField] private float idleTimeMax = 0.08f;
    [SerializeField] private float accel = 60f;
    [SerializeField] private float speedMax = 16f;
    [SerializeField] private float pickupDistance = 0.5f;
    [SerializeField] private float lifetime = 2.5f;

    [Header("Spawn control")]
    [SerializeField] private int maxVisualPerDrop = 6;
    [SerializeField] private int coinsPerVisual = 3;
    [SerializeField] private int spawnBudgetPerFrame = 12;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource; 

    private readonly List<CoinVFX> active = new();
    private readonly Queue<CoinVFX> pool = new();
    private readonly Queue<(Vector3 pos, int amount)> spawnQueue = new();

    private Transform player;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!coinPrefab)
            Debug.LogError("[CoinVFXManager] Assign coinPrefab!");

        for (int i = 0; i < poolSize; i++)
        {
            var c = Instantiate(coinPrefab, transform);
            c.gameObject.SetActive(false);
            pool.Enqueue(c);
        }
    }

    private void LateUpdate()
    {
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        int budget = spawnBudgetPerFrame;
        while (budget-- > 0 && spawnQueue.Count > 0)
        {
            var (pos, amount) = spawnQueue.Dequeue();
            SpawnNow(pos, amount);
        }

        if (active.Count == 0 || !player) return;

        float dt = Time.deltaTime;
        Vector3 ppos = player.position;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var c = active[i];
            if (!c || !c.gameObject.activeInHierarchy) { active.RemoveAt(i); continue; }

            if (Time.time >= c.lifetimeUntil) { Collect(i); continue; }

            Vector3 to = ppos - c.transform.position;
            float d = to.magnitude;
            if (d <= pickupDistance) { Collect(i); continue; }

            if (Time.time < c.idleUntil) continue;

            c.speed = Mathf.Min(c.speed + accel * dt, speedMax);
            Vector3 step = (to / Mathf.Max(d, 0.0001f)) * (c.speed * dt);
            c.transform.position += step;
        }
    }

    private void Collect(int index)
    {
        var c = active[index];

        if (c.pickupFx) Instantiate(c.pickupFx, c.transform.position, Quaternion.identity);
        if (c.pickupSfx)
        {
            if (c.audioSource) c.audioSource.PlayOneShot(c.pickupSfx);
            else if (sfxSource) sfxSource.PlayOneShot(c.pickupSfx);
        }

        c.gameObject.SetActive(false);
        c.transform.SetParent(transform, false);
        pool.Enqueue(c);
        active.RemoveAt(index);
    }

    private void SpawnNow(Vector3 pos, int amount)
    {
        if (!coinPrefab) return;

        int count = Mathf.Clamp(Mathf.CeilToInt(amount / (float)coinsPerVisual), 1, maxVisualPerDrop);
        int per   = Mathf.Max(1, Mathf.RoundToInt(amount / (float)count));

        for (int i = 0; i < count; i++)
        {
            var c = pool.Count > 0 ? pool.Dequeue() : Instantiate(coinPrefab, transform);
            c.transform.SetParent(null, true);
            c.transform.position = pos + (Vector3)(Random.insideUnitCircle * 0.25f);
            c.Setup(per, Random.Range(idleTimeMin, idleTimeMax), lifetime);

            active.Add(c);
        }
    }


    public void SpawnVisualCoins(int amount, Vector3 worldPos)
    {
        if (amount <= 0) return;
        spawnQueue.Enqueue((worldPos, amount));
    }
}
