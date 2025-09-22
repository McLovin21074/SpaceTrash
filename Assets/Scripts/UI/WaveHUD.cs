using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WaveHUD : MonoBehaviour
{
    [SerializeField] private Text waveText;
    [SerializeField] private Text timerText;
    [SerializeField] private string waveFormat = "Волна № {0}";
    [SerializeField] private string timerFormat = "Следующая волна через {0:0}";
    [SerializeField] private bool hideTimerDuringWave = true;

    private WaveManager waveManager;
    private Coroutine waitRoutine;

    private void Reset()
    {
        if (!waveText)
        {
            var texts = GetComponentsInChildren<Text>(true);
            if (texts.Length > 0) waveText = texts[0];
            if (texts.Length > 1) timerText = texts[1];
        }
    }

    private void OnEnable()
    {
        TrySubscribe();
        if (waveManager == null && waitRoutine == null)
            waitRoutine = StartCoroutine(WaitForManager());
        RefreshUI();
    }

    private void OnDisable()
    {
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }
        Unsubscribe();
    }

    private IEnumerator WaitForManager()
    {
        while (WaveManager.Instance == null)
            yield return null;
        waitRoutine = null;
        TrySubscribe();
        RefreshUI();
    }

    private void TrySubscribe()
    {
        var instance = WaveManager.Instance;
        if (!instance || waveManager == instance)
            return;

        Unsubscribe();
        waveManager = instance;
        waveManager.WaveStarted += HandleWaveStarted;
        waveManager.WaveCompleted += HandleWaveCompleted;
        waveManager.IntermissionTick += HandleIntermissionTick;
    }

    private void Unsubscribe()
    {
        if (waveManager == null) return;
        waveManager.WaveStarted -= HandleWaveStarted;
        waveManager.WaveCompleted -= HandleWaveCompleted;
        waveManager.IntermissionTick -= HandleIntermissionTick;
        waveManager = null;
    }

    private void RefreshUI()
    {
        int wave = waveManager != null && waveManager.CurrentWave > 0 ? waveManager.CurrentWave : 1;
        UpdateWaveLabel(wave);

        bool showTimer = waveManager != null && waveManager.InIntermission && waveManager.IntermissionRemaining > 0f;
        float remaining = waveManager != null ? waveManager.IntermissionRemaining : 0f;
        UpdateTimerLabel(remaining, showTimer);
    }

    private void HandleWaveStarted(int wave)
    {
        UpdateWaveLabel(wave);
        if (hideTimerDuringWave)
            UpdateTimerLabel(0f, false);
    }

    private void HandleWaveCompleted(int wave)
    {
        if (waveManager != null)
            UpdateTimerLabel(waveManager.IntermissionRemaining, waveManager.IntermissionRemaining > 0f);
    }

    private void HandleIntermissionTick(float seconds)
    {
        UpdateTimerLabel(seconds, seconds > 0.01f);
    }

    private void UpdateWaveLabel(int wave)
    {
        if (!waveText) return;
        waveText.text = string.Format(waveFormat, Mathf.Max(1, wave));
    }

    private void UpdateTimerLabel(float seconds, bool show)
    {
        if (!timerText)
            return;

        bool active = show || !hideTimerDuringWave;
        if (timerText.gameObject.activeSelf != active)
            timerText.gameObject.SetActive(active);

        if (!active)
            return;

        float clamped = Mathf.Max(0f, seconds);
        float value = Mathf.Ceil(clamped);
        timerText.text = string.Format(timerFormat, value);
    }
}