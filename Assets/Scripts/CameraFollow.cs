using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField, Min(0f)] private float smoothTime = 0.15f;

    [Header("Bounds (choose one)")]
    [Tooltip("Use a Collider2D (e.g., BoxCollider2D/CompositeCollider2D) that encloses the map.")]
    [SerializeField] private Collider2D boundsCollider;
    [Tooltip("Or assign a Renderer (e.g., TilemapRenderer/SpriteRenderer) from which to take world bounds.")]
    [SerializeField] private Renderer boundsRenderer;
    [Tooltip("Or, enable and set manual world-space rect bounds.")]
    [SerializeField] private bool useManualBounds = false;
    [SerializeField] private Rect manualBounds = new Rect(-10, -10, 20, 20);

    private Vector3 currentVelocity;
    private Camera cam;

    private void Awake()
    {
        // Try to auto-assign the player if not set
        cam = GetComponent<Camera>();

        if (target == null)
        {
            var player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        // Keep camera Z if offset has 0 z set accidentally
        if (Mathf.Approximately(offset.z, 0f))
        {
            desired.z = transform.position.z;
        }

        // Clamp within bounds if available (orthographic camera assumed for 2D)
        if (TryGetWorldBounds(out var worldBounds) && (cam != null && cam.orthographic))
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            // If bounds are smaller than the camera view, center the camera on bounds in that axis
            float minX = worldBounds.min.x + halfW;
            float maxX = worldBounds.max.x - halfW;
            float minY = worldBounds.min.y + halfH;
            float maxY = worldBounds.max.y - halfH;

            if (minX > maxX)
            {
                desired.x = worldBounds.center.x;
            }
            else
            {
                desired.x = Mathf.Clamp(desired.x, minX, maxX);
            }

            if (minY > maxY)
            {
                desired.y = worldBounds.center.y;
            }
            else
            {
                desired.y = Mathf.Clamp(desired.y, minY, maxY);
            }
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref currentVelocity,
            smoothTime
        );
    }

    private bool TryGetWorldBounds(out Bounds bounds)
    {
        if (boundsCollider != null)
        {
            bounds = boundsCollider.bounds;
            return true;
        }
        if (boundsRenderer != null)
        {
            bounds = boundsRenderer.bounds;
            return true;
        }
        if (useManualBounds)
        {
            bounds = new Bounds(manualBounds.center, new Vector3(manualBounds.width, manualBounds.height, 100f));
            return true;
        }

        bounds = default;
        return false;
    }
}
