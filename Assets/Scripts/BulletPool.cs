using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private int initialSize = 16;

    private readonly Queue<Bullet> pool = new Queue<Bullet>();

    public static BulletPool Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        Prewarm();
    }

    private void Prewarm()
    {
        if (bulletPrefab == null) return;
        for (int i = 0; i < Mathf.Max(0, initialSize); i++)
        {
            var b = CreateNew();
            Return(b);
        }
    }

    private Bullet CreateNew()
    {
        var b = Instantiate(bulletPrefab, transform);
        b.gameObject.SetActive(false);
        b.Init(this);
        return b;
    }

    public Bullet Get()
    {
        Bullet b = pool.Count > 0 ? pool.Dequeue() : CreateNew();
        b.transform.SetParent(transform, false);
        b.gameObject.SetActive(true);
        return b;
    }

    public void Return(Bullet b)
    {
        if (b == null) return;
        b.gameObject.SetActive(false);
        b.transform.SetParent(transform, false);
        pool.Enqueue(b);
    }
}

