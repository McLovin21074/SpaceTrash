// ApplyPlayerHealthFromStats.cs
using UnityEngine;

[RequireComponent(typeof(Health))]
public class ApplyPlayerHealthFromStats : MonoBehaviour
{
    private void Start()
    {
        var shooter = GetComponent<PlayerShooting>();
        var health  = GetComponent<Health>();
        if (shooter && shooter.Stats && health)
        {
            health.SetMaxFromSO(shooter.Stats.maxHP);
            Debug.Log($"[HP-Apply] Player MaxHP set to {shooter.Stats.maxHP}");
        }
        else
        {
            Debug.LogWarning("[HP-Apply] Missing shooter/stats/health");
        }
    }
}
