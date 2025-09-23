using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MedkitPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int healMin = 1;
    [SerializeField, Min(1)] private int healMax = 2;
    [SerializeField] private string playerTag = "Player";

    private Collider2D trigger;
    private WaveManager owner;
    private int healAmount;
    private bool collected;

    private void Awake()
    {
        trigger = GetComponent<Collider2D>();
        if (trigger) trigger.isTrigger = true;
    }

    private void Start()
    {
        if (healAmount <= 0)
            healAmount = Random.Range(Mathf.Max(1, healMin), Mathf.Max(healMin, healMax) + 1);
    }

    public void Configure(WaveManager manager, int minHeal, int maxHeal)
    {
        owner     = manager;
        healMin   = Mathf.Max(1, minHeal);
        healMax   = Mathf.Max(healMin, maxHeal);
        healAmount = Random.Range(healMin, healMax + 1);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag(playerTag)) return;

        var health = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (!health) return;

        if (!health.Heal(healAmount)) return;

        collected = true;
        owner?.NotifyMedkitRemoved(this);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!collected)
            owner?.NotifyMedkitRemoved(this);
    }
}