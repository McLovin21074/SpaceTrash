using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerSatatsSO stats;
    [SerializeField] private float movingSpeed = 1f;

    private Rigidbody2D rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (stats == null)
        {
            var shooter = GetComponent<PlayerShooting>();
            if (shooter != null)
            {
                stats = shooter.Stats;
            }
        }
    }



    private void FixedUpdate()
    {
        Vector2 inputVector = GameInput.Instance.GetMovementVector();

        inputVector = inputVector.normalized;

        float speed = stats != null ? stats.moveSpeed : movingSpeed;
        rb.MovePosition(rb.position + inputVector * (speed * Time.fixedDeltaTime));
    }
}
