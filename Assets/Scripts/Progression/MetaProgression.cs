// MetaProgression.cs
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-1000)]
public class MetaProgression : MonoBehaviour
{
    public static MetaProgression Instance { get; private set; }

    [Header("Boss unlock")]
    [SerializeField] private int bossUnlockExp = 100; // порог опыта для кнопки босса

    public int Coins { get; private set; }
    public int Exp   { get; private set; }

    public UnityEvent onValuesChanged;

    private const string K_COINS = "mp_coins";
    private const string K_EXP   = "mp_exp";

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Coins = PlayerPrefs.GetInt(K_COINS, 0);
        Exp   = PlayerPrefs.GetInt(K_EXP,   0);
        onValuesChanged ??= new UnityEvent();
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

    public bool BossUnlocked => Exp >= bossUnlockExp;
    public int  BossUnlockExp => bossUnlockExp;
    public void SetBossUnlockExp(int value) => bossUnlockExp = Mathf.Max(1, value);
}
