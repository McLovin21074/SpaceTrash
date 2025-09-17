using System.Collections;
using UnityEngine;

public class EnemyIndicatorTarget : MonoBehaviour
{
    private Coroutine wait;

    private void OnEnable()
    {
        if (OffscreenEnemyIndicatorManager.Instance)
            OffscreenEnemyIndicatorManager.Instance.Register(transform);
        else
            wait = StartCoroutine(WaitAndRegister());
    }

    private IEnumerator WaitAndRegister()
    {
        // ждём, пока менеджер проснётся (если враг активируется раньше)
        while (OffscreenEnemyIndicatorManager.Instance == null) yield return null;
        OffscreenEnemyIndicatorManager.Instance.Register(transform);
        wait = null;
    }

    private void OnDisable()
    {
        if (wait != null) { StopCoroutine(wait); wait = null; }
        if (OffscreenEnemyIndicatorManager.Instance)
            OffscreenEnemyIndicatorManager.Instance.Unregister(transform);
    }
}
