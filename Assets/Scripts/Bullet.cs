using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float range = 8f;
    [SerializeField] private int damage = 1;

    private Vector2 direction = Vector2.right;
    private float traveled;
    private BulletPool pool;

    public void Init(BulletPool ownerPool)
    {
        pool = ownerPool;
    }

    public void Fire(Vector2 dir, float spd, float rng, int dmg, float size = 1f)
    {
        direction = dir.normalized;
        speed = spd;
        range = rng;
        damage = dmg;
        traveled = 0f;
        transform.localScale = Vector3.one * Mathf.Max(0.01f, size);
        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        traveled = 0f;
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)direction * step;
        traveled += step;
        if (traveled >= range)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!gameObject.activeInHierarchy) return;
        var dmg = other.GetComponent<IDamagable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            ReturnToPool();
        }
    }

    public void ReturnToPool()
    {
        if (pool != null)
        {
            pool.Return(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}

