using UnityEngine;
using UnityEngine.Events;

public interface IDamagable
{
    void TakeDamage(int amount);
}

public class Health : MonoBehaviour, IDamagable
{
    [SerializeField] private int maxHp = 5;
    public UnityEvent onDeath;
    public int Current {  get; private set; }
     public int Max => maxHp;

    [Header("Damage Flash")]
    [SerializeField] private Color damageFlashColor = new Color(1f, 0f, 0f, 0.6f);
    [SerializeField, Min(0f)] private float damageFlashDuration = 0.15f;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Coroutine flashRoutine;

    private void Awake()
    {
        Current = maxHp;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                    originalColors[i] = spriteRenderers[i].color;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (Current <= 0) return;
        Current -= Mathf.Max(1, amount);
        Debug.Log($"[Health] {name} took {amount}, now {Current}/{maxHp}");
        TriggerDamageFlash();
        if (Current <= 0) onDeath?.Invoke();

    }

    public void SetMaxFromSO(int value)
    {
        maxHp = value;
        Current = maxHp;
    }

    private void TriggerDamageFlash()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreOriginalColors();
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            var sr = spriteRenderers[i];
            if (sr != null)
                sr.color = damageFlashColor;
        }

        flashRoutine = StartCoroutine(DamageFlashCoroutine());
    }

    private System.Collections.IEnumerator DamageFlashCoroutine()
    {
        float t = 0f;
        while (t < damageFlashDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }
        RestoreOriginalColors();
        flashRoutine = null;
    }

    private void RestoreOriginalColors()
    {
        if (spriteRenderers == null || originalColors == null) return;
        int len = Mathf.Min(spriteRenderers.Length, originalColors.Length);
        for (int i = 0; i < len; i++)
        {
            var sr = spriteRenderers[i];
            if (sr != null)
                sr.color = originalColors[i];
        }
    }
}
