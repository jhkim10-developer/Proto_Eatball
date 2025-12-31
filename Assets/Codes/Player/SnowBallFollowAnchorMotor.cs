using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SnowBallFollowAnchorMotor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform anchor;
    [SerializeField] private Rigidbody rb;

    [Header("Follow")]
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float accel = 35f;
    [SerializeField] private float decel = 45f;

    [Header("Arrive")]
    [SerializeField] private float arriveRadius = 0.8f;   // 가까워지면 속도 줄이기 시작
    [SerializeField] private float stopRadius = 0.05f;    // 거의 도착
    [SerializeField] private float nearSpeed = 2f;        // 가까울 때 최대 속도

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (anchor == null) return;

        Vector3 to = anchor.position - rb.position;
        to.y = 0f;

        float dist = to.magnitude;

        // 도착했으면 감속만
        if (dist < stopRadius)
        {
            ApplyDecel();
            return;
        }

        Vector3 dir = (dist > 0.0001f) ? (to / dist) : Vector3.zero;

        // 거리 기반 목표 속도 (arrive)
        float t = (arriveRadius <= 0.0001f) ? 1f : Mathf.Clamp01(dist / arriveRadius);
        float targetMax = Mathf.Lerp(nearSpeed, maxSpeed, t);
        float targetSpeed = targetMax;

        Vector3 targetVel = dir * targetSpeed;

        Vector3 vel = rb.linearVelocity;
        Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);

        Vector3 dv = targetVel - flatVel;
        Vector3 accelVec = Vector3.ClampMagnitude(dv / Time.fixedDeltaTime, accel);

        rb.AddForce(accelVec, ForceMode.Acceleration);

        LimitFlatSpeed(targetMax);
    }

    private void ApplyDecel()
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);
        if (flatVel.sqrMagnitude < 0.0001f) return;

        rb.AddForce(-flatVel.normalized * decel, ForceMode.Acceleration);
    }

    private void LimitFlatSpeed(float limit)
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);

        if (flatVel.magnitude > limit)
        {
            flatVel = flatVel.normalized * limit;
            rb.linearVelocity = new Vector3(flatVel.x, vel.y, flatVel.z);
        }
    }
}
