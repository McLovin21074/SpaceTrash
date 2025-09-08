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

    private void Awake()
    {
        Current = maxHp;
    }

    public void TakeDamage(int amount)
    {
        if (Current <= 0) return;
        Current -= Mathf.Max(1, amount);
        if(Current <= 0) onDeath?.Invoke();
    }

    public void SetMaxFromSO(int value)
    {
        maxHp = value;
        Current = maxHp;
    }

}
