using UnityEngine;

public class CoinVFX : MonoBehaviour
{
    [HideInInspector] public int amount;
    [HideInInspector] public float idleUntil;
    [HideInInspector] public float speed;
    [HideInInspector] public float lifetimeUntil;

    [Header("Optional FX")]
    public ParticleSystem pickupFx;
    public AudioClip pickupSfx;
    [Tooltip("Оставь пустым — менеджер сам проиграет на AudioSource")]
    public AudioSource audioSource;

    public void Setup(int amount, float idle, float lifetime)
    {
        this.amount = amount;
        idleUntil = Time.time + idle;
        lifetimeUntil = Time.time + lifetime;
        speed = 0f;
        gameObject.SetActive(true);
    }
}
