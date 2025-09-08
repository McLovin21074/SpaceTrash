using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    private PlayerInputActions playerInputAction;
    public static GameInput Instance { get; private set; }
    private Vector2 lastShootDir = Vector2.right;

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

    public Vector2 GetShootingVector4()
    {
        if (Keyboard.current == null) return Vector2.zero;

        bool up = Keyboard.current.upArrowKey.isPressed;
        bool down = Keyboard.current.downArrowKey.isPressed;
        bool left = Keyboard.current.leftArrowKey.isPressed;
        bool right = Keyboard.current.rightArrowKey.isPressed;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame) lastShootDir = Vector2.up;
        if (Keyboard.current.downArrowKey.wasPressedThisFrame) lastShootDir = Vector2.down;
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) lastShootDir = Vector2.left;
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) lastShootDir = Vector2.right;

        if (!up && !down && !left && !right) return Vector2.zero;

        if ((lastShootDir == Vector2.up && up) ||
            (lastShootDir == Vector2.down && down) ||
            (lastShootDir == Vector2.left && left) ||
            (lastShootDir == Vector2.right && right))
        {
            return lastShootDir;
        }

        if (up) return Vector2.up;
        if (down) return Vector2.down;
        if (left) return Vector2.left;
        if (right) return Vector2.right;

        return Vector2.zero;
    }
}
