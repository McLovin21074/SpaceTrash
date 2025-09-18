// RewardOnDeath.cs
using UnityEngine;

[RequireComponent(typeof(Health))]
public class RewardOnDeath : MonoBehaviour
{
    [Min(0)] public int coinsMin = 1;
    [Min(0)] public int coinsMax = 3;
    [Min(0)] public int expMin   = 1;
    [Min(0)] public int expMax   = 2;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.onDeath.AddListener(GrantRewards);
    }

    private void OnDestroy()
    {
        if (health) health.onDeath.RemoveListener(GrantRewards);
    }

    private void GrantRewards()
    {
        int c = Random.Range(coinsMin, coinsMax + 1);
        int e = Random.Range(expMin,   expMax   + 1);
        if (MetaProgression.Instance)
        {
            MetaProgression.Instance.AddCoins(c);
            MetaProgression.Instance.AddExp(e);
        }
    }
}
