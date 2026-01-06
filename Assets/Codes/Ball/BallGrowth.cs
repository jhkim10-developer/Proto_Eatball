using UnityEngine;
using UnityEngine.XR.WindowsMR.Input;

[DisallowMultipleComponent]
public sealed class BallGrowth : MonoBehaviour, IBallSizeProvider
{
    [Header("Size")]
    [SerializeField] private float startRadius = 0.2f;   // 월드 반지름(미터)
    [SerializeField] private float maxRadius = 3.5f;

    [Header("Growth (distance based)")]
    [Tooltip("이동 거리 1m 당 반지름이 얼마나 증가하는지")]
    [SerializeField] private float radiusPerMeter = 0.02f;

    [Tooltip("이 값보다 느리면(미세한 떨림) 성장 누적 안 함")]
    [SerializeField] private float minMoveDelta = 0.002f;

    [Header("Smoothing")]
    [SerializeField] private float scaleSmooth = 12f;

    [Header("Optional Curve (0~1)")]
    [SerializeField] private AnimationCurve growthCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // === Interface ===
    public float Radius { get; private set; } // 외부에서 읽기용 (브러시/깊이 동기화에 사용), 
    public float Normalized => Mathf.InverseLerp(startRadius, maxRadius, Radius);
    // ======================================================================================

    private Vector3 _lastPos;
    private float _accumDistance;

    private void Awake()
    {
        Radius = Mathf.Clamp(startRadius, 0.0001f, maxRadius);
        _lastPos = transform.position;

        ApplyUniformScaleInstant(Radius);
    }

    private void Update()
    {
        //rawRadius(d) = startRadius + d * radiusPerMeter
        //평균 속도 v일 때
        //rawRadius(t) = startRadius + (v * t) * radiusPerMeter
        //즉 ΔRadius/초(선형) = v * radiusPerMeter

        // 1) 이동 거리 누적
        Vector3 pos = transform.position;
        float delta = Vector3.Distance(pos, _lastPos);
        _lastPos = pos;

        if (delta < minMoveDelta) return;

        _accumDistance += delta;

        // 2) 목표 반지름 계산 (거리 기반)
        float rawRadius = startRadius + _accumDistance * radiusPerMeter;
        rawRadius = Mathf.Min(rawRadius, maxRadius);

        // 3) 성장 곡선 적용 (선형이 기본이지만, 커브로 “초반 빨리/후반 느리게” 가능)
        float t = Mathf.InverseLerp(startRadius, maxRadius, rawRadius);
        float curvedT = Mathf.Clamp01(growthCurve.Evaluate(t));
        float targetRadius = Mathf.Lerp(startRadius, maxRadius, curvedT);

        Radius = targetRadius;

        // 4) 스케일 적용 (부드럽게)
        ApplyUniformScaleSmooth(Radius);
    }

    private void ApplyUniformScaleInstant(float radius)
    {
        // 기본 구( Sphere ) 기준: localScale=1일 때 반지름이 0.5m 라고 가정 (Unity 기본 Sphere)
        // 따라서 desiredDiameter = radius*2, localScale = desiredDiameter / 1.0  (기본 지름 1m)
        float diameter = radius * 2f;
        transform.localScale = Vector3.one * diameter;
    }

    private void ApplyUniformScaleSmooth(float radius)
    {
        float diameter = radius * 2f;
        Vector3 target = Vector3.one * diameter;
        transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-scaleSmooth * Time.deltaTime));
    }

    #region Public API
    public void ResetGrowth()
    {
        _accumDistance = 0f;
        Radius = Mathf.Clamp(startRadius, 0.0001f, maxRadius);
        _lastPos = transform.position;
        ApplyUniformScaleInstant(Radius);
    }

    #endregion
}
