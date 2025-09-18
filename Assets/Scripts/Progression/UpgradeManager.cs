using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    MoveSpeed,
    MaxHP,
    FireRate,
    BulletSpeed,
    BulletRange,
    BulletDamage,
    BulletSize,   // можно не использовать сейчас
    BulletCount   // можно не использовать сейчас
}

[System.Serializable]
public class UpgradeConfig
{
    public UpgradeType type;
    public string title;
    [TextArea] public string description;

    [Header("Cost")]
    public int startCost = 10;
    public int costStep  = 5;
    public int maxLevel  = 10;

    [Header("Effect per level (additive)")]
    public float addValue = 0f;  // для MaxHP/BulletDamage — это «+1 за уровень»

    [Header("Gating")]
    public int unlockExp = 0;    // 0 — доступно сразу
}


[DefaultExecutionOrder(-1000)] // раньше всего
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [SerializeField] private List<UpgradeConfig> configs;

    private readonly Dictionary<UpgradeType, int> levels = new();

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Инициализируем уровни для всех типов, даже если конфигов нет
        foreach (UpgradeType t in Enum.GetValues(typeof(UpgradeType)))
        {
            string key = "upg_" + t;
            int lvl = PlayerPrefs.GetInt(key, 0);
            levels[t] = Mathf.Max(0, lvl);
        }
    }

    private void SaveLevel(UpgradeType t) => PlayerPrefs.SetInt("upg_" + t, levels[t]);

    public int  GetLevel(UpgradeType t) => levels.TryGetValue(t, out var l) ? l : 0;

    public UpgradeConfig Get(UpgradeType t)
    {
        var cfg = (configs != null) ? configs.Find(c => c.type == t) : null;
        if (cfg == null)
            Debug.LogWarning($"[UpgradeManager] Нет конфига для {t}. Кнопка будет недоступна.");
        return cfg;
    }

    public int GetPrice(UpgradeType t)
    {
        var cfg = Get(t);
        if (cfg == null) return int.MaxValue;                 // нет конфига → считаем «очень дорого»
        int level = GetLevel(t);
        return cfg.startCost + cfg.costStep * level;
    }

    public bool IsUnlocked(UpgradeType t)
    {
        var cfg = Get(t);
        if (cfg == null) return false;                        // нет конфига → считаем закрытым
        return MetaProgression.Instance == null || MetaProgression.Instance.Exp >= cfg.unlockExp;
    }

    public bool IsMaxed(UpgradeType t)
    {
        var cfg = Get(t);
        if (cfg == null) return true;                         // нет конфига → считаем максимальным
        return GetLevel(t) >= Mathf.Max(0, cfg.maxLevel);
    }

    public bool TryBuy(UpgradeType t)
    {
        var cfg = Get(t);
        if (cfg == null) return false;
        if (!IsUnlocked(t) || IsMaxed(t)) return false;
        int price = GetPrice(t);
        if (MetaProgression.Instance == null || !MetaProgression.Instance.SpendCoins(price)) return false;
        levels[t] = GetLevel(t) + 1;
        SaveLevel(t);
        return true;
    }

    public void ApplyTo(PlayerSatatsSO rt)
    {
        if (!rt) return;

        rt.moveSpeed    += Get(UpgradeType.MoveSpeed)?.addValue   * GetLevel(UpgradeType.MoveSpeed)   ?? 0f;
        rt.maxHP        += Mathf.RoundToInt((Get(UpgradeType.MaxHP)?.addValue ?? 0f) * GetLevel(UpgradeType.MaxHP));
        rt.fireRate     += Get(UpgradeType.FireRate)?.addValue    * GetLevel(UpgradeType.FireRate)    ?? 0f;
        rt.bulletSpeed  += Get(UpgradeType.BulletSpeed)?.addValue * GetLevel(UpgradeType.BulletSpeed) ?? 0f;
        rt.bulletRange  += Get(UpgradeType.BulletRange)?.addValue * GetLevel(UpgradeType.BulletRange) ?? 0f;
        rt.bulletDamage += Mathf.RoundToInt((Get(UpgradeType.BulletDamage)?.addValue ?? 0f) * GetLevel(UpgradeType.BulletDamage));

        if (IsUnlocked(UpgradeType.BulletSize))
            rt.bulletSize  += Get(UpgradeType.BulletSize)?.addValue  * GetLevel(UpgradeType.BulletSize)  ?? 0f;

        if (IsUnlocked(UpgradeType.BulletCount))
            rt.bulletCount += Mathf.RoundToInt((Get(UpgradeType.BulletCount)?.addValue ?? 0f) * GetLevel(UpgradeType.BulletCount));

        // Страховки
        rt.moveSpeed    = Mathf.Max(0.1f, rt.moveSpeed);
        rt.fireRate     = Mathf.Max(0.01f, rt.fireRate);
        rt.bulletSpeed  = Mathf.Max(0.1f, rt.bulletSpeed);
        rt.bulletRange  = Mathf.Max(0.1f, rt.bulletRange);
        rt.bulletSize   = Mathf.Max(0.01f, rt.bulletSize);
        rt.bulletCount  = Mathf.Max(1,    rt.bulletCount);
        rt.maxHP        = Mathf.Max(1,    rt.maxHP);
        rt.bulletDamage = Mathf.Max(1,    rt.bulletDamage);
    }
}
