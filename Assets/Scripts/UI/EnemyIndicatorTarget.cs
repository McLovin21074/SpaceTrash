using System.Collections;
using UnityEngine;

public class EnemyIndicatorTarget : MonoBehaviour
{
    [Header("Indicator style")]
    [SerializeField] private bool overrideColor = false;
    [SerializeField] private Color indicatorColor = Color.red;

    private Coroutine wait;

    private void OnEnable()
    {
        if (OffscreenEnemyIndicatorManager.Instance)
        {
            OffscreenEnemyIndicatorManager.Instance.Register(transform);
            ApplyOverrides();
        }
        else
        {
            wait = StartCoroutine(WaitAndRegister());
        }
    }

    private IEnumerator WaitAndRegister()
    {
        while (OffscreenEnemyIndicatorManager.Instance == null) yield return null;
        OffscreenEnemyIndicatorManager.Instance.Register(transform);
        ApplyOverrides();
        wait = null;
    }

    private void ApplyOverrides()
    {
        if (!overrideColor) return;
        OffscreenEnemyIndicatorManager.Instance?.SetIndicatorColor(transform, indicatorColor);
    }

    public void SetOverrideColor(Color color)
    {
        indicatorColor = color;
        overrideColor = true;
        if (isActiveAndEnabled)
            ApplyOverrides();
        else if (OffscreenEnemyIndicatorManager.Instance)
            OffscreenEnemyIndicatorManager.Instance.SetIndicatorColor(transform, indicatorColor);
    }

    private void OnDisable()
    {
        if (wait != null) { StopCoroutine(wait); wait = null; }
        if (OffscreenEnemyIndicatorManager.Instance)
            OffscreenEnemyIndicatorManager.Instance.Unregister(transform);
    }
}