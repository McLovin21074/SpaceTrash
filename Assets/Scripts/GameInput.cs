using UnityEngine;

public class GameInput : MonoBehaviour
{
    private PlayerInputActions playerInputAction;
    public static GameInput Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        playerInputAction = new PlayerInputActions();
        playerInputAction.Enable();
    }

    public Vector2 GetMovementVector()
    {
        Vector2 inputVector = playerInputAction.Player.Move.ReadValue<Vector2>();
        return inputVector;
    }
}
