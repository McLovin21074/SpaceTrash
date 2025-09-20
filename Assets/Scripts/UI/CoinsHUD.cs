using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CoinsHUD : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image coinIcon;
    [SerializeField] private Text  coinsText;

    [Header("Display")]
    [SerializeField] private bool shortFormat = false;
    [SerializeField] private string prefix = "";

    [Header("Pulse on gain")]
    [SerializeField] private RectTransform pulseTarget; 
    [SerializeField] private float pulseScale = 1.12f;
    [SerializeField] private float pulseTime  = 0.08f;

    private int lastShown = -1;
    private Coroutine pulse;

    private void Reset()
    {
        if (!coinsText) coinsText = GetComponentInChildren<Text>(true);
        if (!coinIcon)  coinIcon  = GetComponentInChildren<Image>(true);
        if (!pulseTarget) pulseTarget = transform as RectTransform;
    }

    private void Awake()
    {
        if (!pulseTarget) pulseTarget = transform as RectTransform;
    }

    private void OnEnable()
    {
        if (MetaProgression.Instance)
            MetaProgression.Instance.onValuesChanged.AddListener(Refresh);
        Refresh(); 
    }

    private void OnDisable()
    {
        if (MetaProgression.Instance)
            MetaProgression.Instance.onValuesChanged.RemoveListener(Refresh);
    }

    public void Refresh()
    {
        var mp = MetaProgression.Instance;
        if (mp == null || coinsText == null) return;

        int coins = mp.Coins;
        coinsText.text = prefix + (shortFormat ? FormatShort(coins) : coins.ToString());

        if (lastShown >= 0 && coins > lastShown)
            DoPulse();

        lastShown = coins;
    }

    private void DoPulse()
    {
        if (pulseTarget == null) return;
        if (pulse != null) StopCoroutine(pulse);
        pulse = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        Vector3 baseScale = pulseTarget.localScale;
        Vector3 target    = baseScale * pulseScale;

        float t = 0f;
        while (t < pulseTime)
        {
            t += Time.unscaledDeltaTime;
            float k = t / pulseTime;
            pulseTarget.localScale = Vector3.Lerp(baseScale, target, k);
            yield return null;
        }
        t = 0f;
        while (t < pulseTime)
        {
            t += Time.unscaledDeltaTime;
            float k = t / pulseTime;
            pulseTarget.localScale = Vector3.Lerp(target, baseScale, k);
            yield return null;
        }
        pulseTarget.localScale = baseScale;
        pulse = null;
    }

    private static string FormatShort(int n)
    {
        if (n >= 1_000_000) return (n / 1_000_000f).ToString("0.#") + "M";
        if (n >= 1_000)     return (n / 1_000f).ToString("0.#") + "k";
        return n.ToString();
    }
}
