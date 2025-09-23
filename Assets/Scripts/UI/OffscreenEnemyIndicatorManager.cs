// OffscreenEnemyIndicatorManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class OffscreenEnemyIndicatorManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform indicatorPrefab;

    [Header("Tuning")]
    [SerializeField] private float edgePadding = 32f;
    [SerializeField, Range(0f, 0.2f)] private float onScreenMargin = 0.06f;
    [SerializeField] private bool useRendererVisibility = true;
    [SerializeField] private float rotationOffsetDeg = -90f;     

    private readonly Dictionary<Transform, RectTransform> map = new();
    private readonly Dictionary<Transform, Graphic[]> indicatorGraphics = new();
    private readonly Dictionary<Transform, Color> pendingColors = new();

    public static OffscreenEnemyIndicatorManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (!canvas)     canvas     = GetComponent<Canvas>();
        if (!canvasRect) canvasRect = GetComponent<RectTransform>();

        if (!targetCamera)
        {
            if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                targetCamera = canvas.worldCamera ? canvas.worldCamera : Camera.main;
            else
                targetCamera = Camera.main;
        }

        if (!indicatorPrefab)
            Debug.LogError("[Indicator] indicatorPrefab");
    }

    public void Register(Transform target)
    {
        if (!target || map.ContainsKey(target)) return;

        var inst = Instantiate(indicatorPrefab, canvasRect);
        inst.gameObject.SetActive(false);

        var graphics = inst.GetComponentsInChildren<Graphic>(includeInactive: true);
        for (int i = 0; i < graphics.Length; i++)
        {
            var g = graphics[i];
            if (g) g.raycastTarget = false;
        }

        map[target] = inst;
        indicatorGraphics[target] = graphics;

        if (pendingColors.TryGetValue(target, out var color))
        {
            ApplyColor(graphics, color);
            pendingColors.Remove(target);
        }
    }

    public void Unregister(Transform target)
    {
        if (!target) return;
        if (map.TryGetValue(target, out var inst))
        {
            Destroy(inst.gameObject);
            map.Remove(target);
        }
        indicatorGraphics.Remove(target);
        pendingColors.Remove(target);
    }

    public void SetIndicatorColor(Transform target, Color color)
    {
        if (!target) return;

        if (indicatorGraphics.TryGetValue(target, out var graphics))
        {
            ApplyColor(graphics, color);
        }
        else
        {
            pendingColors[target] = color;
        }
    }

    private static void ApplyColor(Graphic[] graphics, Color color)
    {
        if (graphics == null) return;
        for (int i = 0; i < graphics.Length; i++)
        {
            var g = graphics[i];
            if (g) g.color = color;
        }
    }

    private void LateUpdate()
    {
        if (!targetCamera || !canvasRect || !indicatorPrefab) return;

        Vector2 screenSize   = new(Screen.width, Screen.height);
        Vector2 screenCenter = screenSize * 0.5f;
        Camera uiCam = (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : targetCamera;

        foreach (var kv in map)
        {
            var t   = kv.Key;
            var ind = kv.Value;

            if (!t) { ind.gameObject.SetActive(false); continue; }

            bool onScreen;
            if (useRendererVisibility)
            {
                Renderer rend = t.GetComponentInChildren<Renderer>(true);
                if (rend != null)
                {
                    var sp = targetCamera.WorldToScreenPoint(t.position);
                    onScreen = (sp.z > 0f) && rend.isVisible; 
                }
                else
                {
                    Vector3 vp = targetCamera.WorldToViewportPoint(t.position);
                    onScreen = vp.z > 0f &&
                               vp.x > onScreenMargin && vp.x < 1f - onScreenMargin &&
                               vp.y > onScreenMargin && vp.y < 1f - onScreenMargin;
                }
            }
            else
            {
                Vector3 vp = targetCamera.WorldToViewportPoint(t.position);
                onScreen = vp.z > 0f &&
                           vp.x > onScreenMargin && vp.x < 1f - onScreenMargin &&
                           vp.y > onScreenMargin && vp.y < 1f - onScreenMargin;
            }

            if (onScreen) { ind.gameObject.SetActive(false); continue; }

            Vector3 spTar = targetCamera.WorldToScreenPoint(t.position);
            Vector2 dir   = (Vector2)(spTar - (Vector3)screenCenter);
            if (spTar.z < 0f) dir = -dir;

            Vector2 pos = GetEdgePoint(screenCenter, dir, screenSize, edgePadding);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pos, uiCam, out Vector2 uiPos);
            ind.anchoredPosition = uiPos;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffsetDeg;
            ind.localEulerAngles = new Vector3(0, 0, angle);

            ind.gameObject.SetActive(true);
        }
    }

    private static Vector2 GetEdgePoint(Vector2 center, Vector2 dir, Vector2 screen, float pad)
    {
        Vector2 half = new(screen.x * 0.5f - pad, screen.y * 0.5f - pad);
        if (dir.sqrMagnitude < 0.0001f) return center;

        float tx = float.PositiveInfinity, ty = float.PositiveInfinity;
        if (Mathf.Abs(dir.x) > 0.0001f) tx = ((dir.x > 0 ? half.x : -half.x) / dir.x);
        if (Mathf.Abs(dir.y) > 0.0001f) ty = ((dir.y > 0 ? half.y : -half.y) / dir.y);

        float t = Mathf.Min(Mathf.Abs(tx), Mathf.Abs(ty));
        return center + dir * t;
    }
}