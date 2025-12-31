using UnityEngine;

public class PlayerInputBinder : MonoBehaviour
{
    [SerializeField] private Joystick joystick;
    [SerializeField] private CharacterMotor playerMotor;

    private void OnEnable()
    {
        joystick.OnInputVectorEvent += OnInput;
    }

    private void OnDisable()
    {
        joystick.OnInputVectorEvent -= OnInput;
    }

    private void OnInput(Vector2 v)
    {
        playerMotor.SetInput(v);
    }
}
