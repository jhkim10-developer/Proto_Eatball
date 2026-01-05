using UnityEngine;

public class CamFollow : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0, 8, -8);
    [SerializeField] float smoothTime = 0.15f;

    [Header("Zoom (Orthographic)")]
    [SerializeField] private SnowBallGrowth growth;
    [SerializeField] private float baseOrthoSize = 7f;
    [SerializeField] private float radiusToSize = 0.6f;
    [SerializeField] private float zoomSmoothTime = 0.25f;
    [SerializeField] private float minSize = 7f;
    [SerializeField] private float maxSize = 15f;

    Vector3 velocity;
    private float zoomVelocity;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = baseOrthoSize;
    }

    void LateUpdate()
    {
        if (!target) return;

        //위치 따라오기
        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );

        // 2) SnowBall 성장에 따른 줌 아웃
        UpdateZoom();
    }

    private void UpdateZoom()
    {
        if (!growth) return;

        float desiredSize =
            baseOrthoSize + growth.Radius * radiusToSize;

        desiredSize = Mathf.Clamp(desiredSize, minSize, maxSize);

        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize,
            desiredSize,
            ref zoomVelocity,
            zoomSmoothTime
        );
    }

    /// <summary>
    /// SnowBall이 런타임에 생성될 때 연결하기 위한 함수 (선택)
    /// </summary>
    public void BindGrowth(SnowBallGrowth growth)
    {
        this.growth = growth;
    }
}
