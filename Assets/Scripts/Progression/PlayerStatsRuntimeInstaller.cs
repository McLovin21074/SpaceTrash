// PlayerStatsRuntimeInstaller.cs
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PlayerStatsRuntimeInstaller : MonoBehaviour
{
    [SerializeField] private PlayerShooting shooting;
    [SerializeField] private PlayerSatatsSO baseStats;

    private void Awake()
    {
        if (!shooting) shooting = GetComponent<PlayerShooting>();
        if (!shooting || !baseStats) { Debug.LogWarning("[PSRI] No shooting/baseStats"); return; }

        var rt = ScriptableObject.Instantiate(baseStats);
        if (UpgradeManager.Instance) UpgradeManager.Instance.ApplyTo(rt);

        shooting.OverrideStats(rt);
        Debug.Log($"[PSRI] Applied stats → HP:{rt.maxHP} Move:{rt.moveSpeed} FR:{rt.fireRate} Dmg:{rt.bulletDamage}");
    }
}
