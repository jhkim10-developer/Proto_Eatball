using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterMotor : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] float maxSpeed = 6f;
    [SerializeField] float accel = 25f;        // 가속
    [SerializeField] float decel = 30f;        // 감속

    [Header("Rotation")]
    [SerializeField] float turnSpeed = 720f;   // 초당 회전각

    Rigidbody rb;
    Vector2 input; // joystick (-1 ~ 1)

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Joystick에서 호출
    /// </summary>
    public void SetInput(Vector2 v)
    {
        input = Vector2.ClampMagnitude(v, 1f);
    }

    void FixedUpdate()
    {
        // 1. 입력 → 월드 이동 방향
        Vector3 desiredDir = new Vector3(input.x, 0f, input.y);

        Vector3 vel = rb.linearVelocity;
        Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);

        if (desiredDir.sqrMagnitude > 0.0001f)
        {
            // 2. 목표 속도 (입력 크기에 비례)
            Vector3 targetVel = desiredDir.normalized * (maxSpeed * input.magnitude);

            // 3. 현재 → 목표 속도로 가속
            Vector3 dv = targetVel - flatVel;
            Vector3 accelVec = Vector3.ClampMagnitude(
                dv / Time.fixedDeltaTime,
                accel
            );

            rb.AddForce(accelVec, ForceMode.Acceleration);

            // 4. 회전 (이동 방향 바라보기)
            Quaternion targetRot = Quaternion.LookRotation(desiredDir);
            rb.MoveRotation(
                Quaternion.RotateTowards(
                    rb.rotation,
                    targetRot,
                    turnSpeed * Time.fixedDeltaTime
                )
            );
        }
        else
        {
            // 5. 입력 없을 때 감속 (마찰 느낌)
            if (flatVel.sqrMagnitude > 0.001f)
            {
                Vector3 decelVec = -flatVel.normalized * decel;
                rb.AddForce(decelVec, ForceMode.Acceleration);
            }
        }

        // 6. 최고속도 제한
        vel = rb.linearVelocity;
        flatVel = new Vector3(vel.x, 0f, vel.z);

        if (flatVel.magnitude > maxSpeed)
        {
            flatVel = flatVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(flatVel.x, vel.y, flatVel.z);
        }
    }
}
