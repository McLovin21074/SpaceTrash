// MetaProgression.cs
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-1000)]
public class MetaProgression : MonoBehaviour
{
    public static MetaProgression Instance { get; private set; }

    [Header("Boss unlock")]
    [SerializeField] private int bossUnlockExp = 100; // ?????????? ?????<?'?? ???>?? ?????????? ?+????????

    public int Coins { get; private set; }
    public int Exp   { get; private set; }
    public int MaxWaveReached { get; private set; }
    public bool HasNewBestWaveThisRun { get; private set; }
    public UnityEvent onValuesChanged;
    public UnityEvent onBestWaveChanged;

    private const string K_COINS    = "mp_coins";
    private const string K_EXP      = "mp_exp";
    private const string K_MAX_WAVE = "mp_max_wave";

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Coins          = PlayerPrefs.GetInt(K_COINS,    0);
        Exp            = PlayerPrefs.GetInt(K_EXP,      0);
        MaxWaveReached = PlayerPrefs.GetInt(K_MAX_WAVE, 0);
        HasNewBestWaveThisRun = false;

        onValuesChanged   ??= new UnityEvent();
        onBestWaveChanged ??= new UnityEvent();
    }

    public bool ConsumeNewBestWaveFlag()
    {
        if (!HasNewBestWaveThisRun)
            return false;
        HasNewBestWaveThisRun = false;
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        Coins += amount;
        PlayerPrefs.SetInt(K_COINS, Coins);
        onValuesChanged.Invoke();
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        Exp += amount;
        PlayerPrefs.SetInt(K_EXP, Exp);
        onValuesChanged.Invoke();
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || Coins < amount) return false;
        Coins -= amount;
        PlayerPrefs.SetInt(K_COINS, Coins);
        onValuesChanged.Invoke();
        return true;
    }

    public void ReportWaveCleared(int wave)
    {
        if (wave <= 0) return;
        if (wave <= MaxWaveReached) return;

        MaxWaveReached = wave;
        HasNewBestWaveThisRun = true;
        PlayerPrefs.SetInt(K_MAX_WAVE, MaxWaveReached);
        onBestWaveChanged.Invoke();
    }

    public bool BossUnlocked => Exp >= bossUnlockExp;
    public int  BossUnlockExp => bossUnlockExp;
    public void SetBossUnlockExp(int value) => bossUnlockExp = Mathf.Max(1, value);
}


