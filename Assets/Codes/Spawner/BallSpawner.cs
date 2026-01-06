using System.Threading;
using UnityEngine;

[DisallowMultipleComponent]
public class BallSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform ballAnchor;
    [SerializeField] private GameObject snowBallPrefab;

    [Header("Spawn Rule")]
    [SerializeField] private bool aiSpawnOnEnable = true; // AI는 스폰 즉시 공 생성
    [SerializeField] private bool aiSpawnOnlyOnce = true; // AI는 공을 단 한번만 생성

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
        if (isPlayer)
        {
            TrySpawn();
        }
    }

    public void TrySpawn()
    {
        if (aiSpawnOnlyOnce && _spawned) return;
        if (snowBallPrefab == null || ballAnchor == null)
        {
            Debug.LogWarning("[BallSpawner] Missing refs.");
            return;
        }

        // 1) 처음부터 anchor 위치/회전으로 생성 (0,0,0 생성 방지)
        GameObject ball = Instantiate(snowBallPrefab, ballAnchor.position, ballAnchor.rotation);
        var growth = ball.GetComponent<BallGrowth>();

        if (isPlayer)
        {
            FindFirstObjectByType<CamFollow>()?.BindGrowth(growth);            
        }

        // 스노우볼 바인딩 -> 각각의 캐릭터에 필요 (플레이어든 AI든)
        CharacterBallRef ballRef = GetComponent<CharacterBallRef>();
        ballRef.Bind(growth);

        // 2) Follow bind (owner/anchor)
        var follow = ball.GetComponent<SnowBallFollowAnchorMotor>();
        if (follow != null)
            follow.Bind(transform, ballAnchor);

        // 고유 아이디 바인딩
        var ballID = ball.GetComponent<BallID>();
        if(ballID != null)
        {
            ballID.BindOwner(gameObject); //볼 스포너는 캐릭터에 붙어있잖아.
        }
        else
        {
            Debug.LogError("[BallSpawner] Cant' Find BallID Component");
        }

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
