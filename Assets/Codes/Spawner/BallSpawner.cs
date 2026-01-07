using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BallSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform ballAnchor;
    [SerializeField] private GameObject snowBallPrefab;

    [Header("Spawn Rule")]
    [SerializeField] private bool aiSpawnOnEnable = true; // AI는 스폰 즉시 공 생성

    [Header("Spawn Fix")]
    [Tooltip("NavMeshAgent가 첫 Update에서 위치를 확정하는 경우를 대비해, 다음 FixedUpdate에 한 번 더 스냅합니다.")]
    [SerializeField] private bool snapOnNextFixedUpdate = true;

    public bool isPlayer = false;

    private bool _spawned;

    private CharacterLifeCycle _ownerLife;

    private Coroutine _respawnCo;

    private void Awake()
    {
        if (ballAnchor == null)
            ballAnchor = transform.Find("BallAnchor");

        _ownerLife = GetComponent<CharacterLifeCycle>(); 
    }

    private void OnEnable()
    {
        //AI일 경우에는 AISpawner에서 공 생성, Player의 경우에는 여기에서 생성
        if (isPlayer)
        {
            TrySpawn();
        }
    }
    private bool CanSpawnNow()
    {
        // CharacterLifeCycle이 없으면 “스폰 불가능”
        if (_ownerLife == null) return false;

        // 캐릭터가 죽었거나 무적 리스폰 대기 등 “죽음 상태”면 스폰 금지
        return !_ownerLife.IsDefeated;
        // 필요하면: && gameObject.activeInHierarchy 같은 것도 추가 가능
    }

    public void TrySpawn()
    {
        if (_spawned) return;

        if (!CanSpawnNow()) return;

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
        var life = ball.GetComponent<BallLifeCycle>();
        GetComponent<CharacterBallRef>()?.Bind(life);

        // 2) Follow bind (owner/anchor)
        var follow = ball.GetComponent<BallFollowAnchorMotor>();
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

    private IEnumerator SnapNextFixed(GameObject ball, Transform anchor)
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

    public void NotifyBallDestroyed(float delay, GameObject instigator)
    {
        _spawned = false; // 다시 스폰 가능하게

        if (!CanSpawnNow()) return;

        // AI 도망 트리거
        if (!isPlayer)
        {
            var threat = instigator != null ? instigator.transform : null;
            if (threat != null && TryGetComponent<AIBrainNavmeshWander>(out var brain))
            {
                brain.FleeFrom(threat, delay); // 일단 "공 다시 나오기 전까지" 도망
            }
        }

        if (_respawnCo != null) StopCoroutine(_respawnCo);
        _respawnCo = StartCoroutine(CoRespawn(delay));
    }

    private IEnumerator CoRespawn(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!CanSpawnNow()) yield break;

        TrySpawn();
    }

    // 캐릭터가 죽을 때 호출해서 예약 코루틴까지 끊기
    public void NotifyOwnerDefeated()
    {
        _spawned = false;
        if (_respawnCo != null)
        {
            StopCoroutine(_respawnCo);
            _respawnCo = null;
        }
    }

    // 캐릭터 리스폰 직후 호출하면 바로 공을 굴리게 할 수도 있음
    public void NotifyOwnerRespawned(bool spawnImmediately)
    {
        _spawned = false;
        if (spawnImmediately) TrySpawn();
    }
}
