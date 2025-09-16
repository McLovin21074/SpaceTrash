using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Idle Frames")]
    [SerializeField] private Sprite idleUp;
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleRight;

    [Header("Walk Frames (2 per direction)")]
    [SerializeField] private Sprite[] walkUp = new Sprite[2];
    [SerializeField] private Sprite[] walkDown = new Sprite[2];
    [SerializeField] private Sprite[] walkLeft = new Sprite[2];
    [SerializeField] private Sprite[] walkRight = new Sprite[2];

    [Header("Playback")]
    [SerializeField, Min(0f)] private float framesPerSecond = 6f;
    [SerializeField, Min(0f)] private float moveEpsilon = 0.01f;

    private Vector2 lastFacing = Vector2.down;
    private float frameTimer;
    private int frameIndex;

    private NavMeshAgent agent;
    private Vector3 prevPos;
    private Health health;

    private void Reset()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        prevPos = transform.position;

        if (health != null)
        {
            health.onDeath.AddListener(OnDeath);
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDeath.RemoveListener(OnDeath);
        }
    }

    private void Update()
    {
        Vector2 input = GetMovementVector();
        Animate(input);
    }

    private Vector2 GetMovementVector()
    {
        // Prefer NavMeshAgent velocity if available, otherwise compute from position delta
        if (agent != null)
        {
            return agent.velocity;
        }
        else
        {
            Vector3 delta = transform.position - prevPos;
            prevPos = transform.position;
            return new Vector2(delta.x, delta.y) / Mathf.Max(Time.deltaTime, 0.0001f);
        }
    }

    private void Animate(Vector2 input)
    {
        bool moving = input.sqrMagnitude > (moveEpsilon * moveEpsilon);
        Vector2 dir = moving ? input : lastFacing;

        // Reduce to 4 main directions
        Vector2 cardinal = Cardinalize(dir);
        if (moving)
            lastFacing = cardinal;

        if (!moving)
        {
            SetIdle(cardinal);
            return;
        }

        // Advance frame timer
        float interval = framesPerSecond > 0f ? (1f / framesPerSecond) : 0f;
        if (interval <= 0f)
        {
            SetWalk(cardinal, 0);
            return;
        }

        frameTimer += Time.deltaTime;
        if (frameTimer >= interval)
        {
            frameTimer -= interval;
            frameIndex = (frameIndex + 1) % 2; // two frames
        }

        SetWalk(cardinal, frameIndex);
    }

    private static Vector2 Cardinalize(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x >= 0 ? Vector2.right : Vector2.left;
        else
            return v.y >= 0 ? Vector2.up : Vector2.down;
    }

    private void SetIdle(Vector2 dir)
    {
        if (spriteRenderer == null) return;

        if (dir == Vector2.up && idleUp != null) spriteRenderer.sprite = idleUp;
        else if (dir == Vector2.down && idleDown != null) spriteRenderer.sprite = idleDown;
        else if (dir == Vector2.left && idleLeft != null) spriteRenderer.sprite = idleLeft;
        else if (dir == Vector2.right && idleRight != null) spriteRenderer.sprite = idleRight;
    }

    private void SetWalk(Vector2 dir, int idx)
    {
        if (spriteRenderer == null) return;
        idx = Mathf.Clamp(idx, 0, 1);

        if (dir == Vector2.up && walkUp != null && walkUp.Length >= 2 && walkUp[idx] != null)
            spriteRenderer.sprite = walkUp[idx];
        else if (dir == Vector2.down && walkDown != null && walkDown.Length >= 2 && walkDown[idx] != null)
            spriteRenderer.sprite = walkDown[idx];
        else if (dir == Vector2.left && walkLeft != null && walkLeft.Length >= 2 && walkLeft[idx] != null)
            spriteRenderer.sprite = walkLeft[idx];
        else if (dir == Vector2.right && walkRight != null && walkRight.Length >= 2 && walkRight[idx] != null)
            spriteRenderer.sprite = walkRight[idx];
    }

    private void OnDeath()
    {
        // Freeze on idle facing when dead
        SetIdle(lastFacing);
        enabled = false;
    }
}

