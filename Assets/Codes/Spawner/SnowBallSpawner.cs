using System.Threading;
using UnityEngine;

[DisallowMultipleComponent]
public class SnowBallSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform ballAnchor;
    [SerializeField] private GameObject snowBallPrefab;

    [Header("Spawn Rule")]
    [SerializeField] private bool spawnOnEnable = true; // AI는 스폰 즉시 공 생성 추천
    [SerializeField] private bool spawnOnlyOnce = true;

    [Header("Spawn Fix")]
    [Tooltip("NavMeshAgent가 첫 Update에서 위치를 확정하는 경우를 대비해, 다음 FixedUpdate에 한 번 더 스냅합니다.")]
    [SerializeField] private bool snapOnNextFixedUpdate = true;

    public bool isPlayer = false;

    private bool _spawned;

    private void Awake()
    {
        if (ballAnchor == null)
            ballAnchor = transform.Find("BallAnchor");
    }

    private void OnEnable()
    {
        //AI일 경우에는 AISpawner에서 공 생성, Player의 경우에는 여기에서 생성
        //if (spawnOnEnable) TrySpawn(); 
        if (isPlayer) TrySpawn();
    }

    public void TrySpawn()
    {
        if (spawnOnlyOnce && _spawned) return;
        if (snowBallPrefab == null || ballAnchor == null)
        {
            Debug.LogWarning("[SnowBallSpawner] Missing refs.");
            return;
        }

        // 1) 처음부터 anchor 위치/회전으로 생성 (0,0,0 생성 방지)
        GameObject ball = Instantiate(snowBallPrefab, ballAnchor.position, ballAnchor.rotation);

        if(isPlayer)
        {
            var growth = ball.GetComponent<SnowBallGrowth>();
            FindFirstObjectByType<CamFollow>()?.BindGrowth(growth);
        }

        // 2) Follow bind (owner/anchor)
        var follow = ball.GetComponent<SnowBallFollowAnchorMotor>();
        if (follow != null)
            follow.Bind(transform, ballAnchor);

        // 3) Rigidbody가 있으면 즉시 위치/속도 안정화
        if (ball.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.position = ballAnchor.position;
            rb.rotation = ballAnchor.rotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep(); // 첫 프레임 튐 방지(원치 않으면 제거)
        }
        else
        {
            ball.transform.SetPositionAndRotation(ballAnchor.position, ballAnchor.rotation);
        }

        // 4) (선택) 다음 FixedUpdate에 다시 한 번 스냅 (AI 스폰 직후 안정화용)
        if (snapOnNextFixedUpdate)
            StartCoroutine(SnapNextFixed(ball, ballAnchor));

        _spawned = true;
    }

    private System.Collections.IEnumerator SnapNextFixed(GameObject ball, Transform anchor)
    {
        // 다음 FixedUpdate까지 대기
        yield return new WaitForFixedUpdate();

        if (ball == null || anchor == null) yield break;

        if (ball.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.position = anchor.position;
            rb.rotation = anchor.rotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            ball.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }
    }
}
