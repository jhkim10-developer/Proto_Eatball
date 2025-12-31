using UnityEngine;

public class PlayerInputBinder : MonoBehaviour
{
    [SerializeField] Joystick joystick;
    [SerializeField] CharacterMotor motor;

    void OnEnable()
    {
        joystick.OnInputVectorEvent += motor.SetInput;
    }

    void OnDisable()
    {
        joystick.OnInputVectorEvent -= motor.SetInput;
    }
}
