using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CoinsHUD : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Text coinsText;            
    [SerializeField] private Image coinIcon;              
    [SerializeField] private RectTransform pulseTarget;  

    [Header("Pulse")]
    [SerializeField, Min(1f)] private float pulseScale = 1.12f;
    [SerializeField, Min(0f)] private float pulseTime  = 0.08f;

    private int lastShown = -1;
    private Vector3 baseScale = Vector3.one;
    private Coroutine pulseCo;

    private void Awake()
    {
        if (!pulseTarget && coinsText) pulseTarget = coinsText.rectTransform;
        if (pulseTarget) baseScale = pulseTarget.localScale;
        Refresh(true);
    }

    private void OnEnable()
    {
        if (MetaProgression.Instance)
            MetaProgression.Instance.onValuesChanged.AddListener(OnMetaChange);
        Refresh(true);
    }

    private void OnDisable()
    {
        if (MetaProgression.Instance)
            MetaProgression.Instance.onValuesChanged.RemoveListener(OnMetaChange);
        ResetScale();
    }

    private void OnDestroy() => OnDisable();

    private void OnMetaChange() => Refresh();

    public void Refresh(bool force = false)
    {
        var mp = MetaProgression.Instance;
        if (!mp) return;

        int coins = mp.Coins;
        if (coinsText) coinsText.text = coins.ToString();

        if (force) lastShown = coins;

        if (coins > lastShown) TriggerPulse();

        lastShown = coins;
    }

    private void TriggerPulse()
    {
        if (!pulseTarget) return;

        if (pulseCo != null) StopCoroutine(pulseCo);
        pulseTarget.localScale = baseScale;
        pulseCo = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float half = Mathf.Max(0.0001f, pulseTime);
        Vector3 big = baseScale * pulseScale;

        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / half);
            a = a * a * (3f - 2f * a);             
            pulseTarget.localScale = Vector3.Lerp(baseScale, big, a);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / half);
            a = a * a * (3f - 2f * a);
            pulseTarget.localScale = Vector3.Lerp(big, baseScale, a);
            yield return null;
        }

        ResetScale();
        pulseCo = null;
    }

    private void ResetScale()
    {
        if (pulseTarget) pulseTarget.localScale = baseScale;
    }
}
