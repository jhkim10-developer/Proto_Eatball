using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SnowBallFollowAnchorMotor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform target;   // 추가: 플레이어 중심
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

    [Header("Min Distance Constraint")]
    [SerializeField] private float baseMinDistance = 0.7f;   // 공이 작을 때 기본 거리 (플레이어 앞)
    [SerializeField] private float radiusToDistance = 1.1f;  // growth.Radius → 거리 증분 비율 (공이 커질수록 멀어지는 정도)
    [SerializeField] private float extraBuffer = 0.05f;      // 충돌/떨림 방지 버퍼

    [Header("Growth (optional)")]
    [SerializeField] private SnowBallGrowth growth;          // 네 Growth 컴포넌트

    public void Bind(Transform owner, Transform anchor, SnowBallGrowth growth)
    {
        this.target = owner;
        this.anchor = anchor;
        this.growth = growth; // (선택) follow distance 연동하려면 유지
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (anchor == null || this.target == null) return;

        // 1) 목표점(anchor)을 "최소거리 링" 위로 보정한 constrainedTarget 생성
        Vector3 target = GetConstrainedTargetOnRing();

        Vector3 to = target - rb.position;
        to.y = 0f;

        float dist = to.magnitude;

        // 도착했으면 감속만
        if (dist < stopRadius)
        {
            ApplyDecel();
            return;
        }

        Vector3 dir = (dist > 0.0001f) ? (to / dist) : Vector3.zero;

        float t = (arriveRadius <= 0.0001f) ? 1f : Mathf.Clamp01(dist / arriveRadius);
        float targetMax = Mathf.Lerp(nearSpeed, maxSpeed, t);

        Vector3 targetVel = dir * targetMax;

        Vector3 vel = rb.linearVelocity;
        Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);

        Vector3 dv = targetVel - flatVel;
        Vector3 accelVec = Vector3.ClampMagnitude(dv / Time.fixedDeltaTime, accel);

        rb.AddForce(accelVec, ForceMode.Acceleration);
        LimitFlatSpeed(targetMax);
    }

    private Vector3 GetConstrainedTargetOnRing()
    {
        Vector3 playerPos = target.position;
        Vector3 desired = anchor.position;

        // 성장에 따른 최소 거리 계산
        float r = growth != null ? growth.Radius : 0f;
        float minDist = baseMinDistance + r * radiusToDistance + extraBuffer;

        Vector3 planar = desired - playerPos;
        planar.y = 0f;

        // 앵커가 플레이어 바로 위/겹침이면 전방을 기본 방향으로
        if (planar.sqrMagnitude < 0.000001f)
            planar = target.forward;

        float dist = planar.magnitude;

        // anchor가 minDist보다 안쪽이면 링 위로 밀어냄(투영)
        if (dist < minDist)
            planar = planar.normalized * minDist;

        Vector3 constrained = playerPos + planar;
        constrained.y = rb.position.y; // 공 높이 유지(필요시)
        return constrained;
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
