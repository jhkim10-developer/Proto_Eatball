using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterLife : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CharacterID characterID;
    [SerializeField] private BallSpawner ballSpawner;
    [SerializeField] private MonoBehaviour spawnPointProviderBehaviour; // SpawnPointProvider 드래그
    //private ISpawnPointProvider _spawnPoints;

    [Header("Respawn Rule")]
    [SerializeField] private float respawnDelay = 0.0f;      // 즉시 리스폰 원하면 0
    [SerializeField] private float invincibleTime = 1.5f;    // 무적 1.5초
    [SerializeField] private float ballRespawnAfterLost = 0.5f;

    public bool IsDead { get; private set; }
    public bool IsInvincible { get; private set; }

    private GameObject _ownedBall; // 현재 소유 공 (스폰 시 바인딩)

    private void Awake()
    {
        if (characterID == null) characterID = GetComponent<CharacterID>();
        //if (ballSpawner == null) ballSpawner = GetComponent<SnowBallSpawnerRuntime>();

        //_spawnPoints = spawnPointProviderBehaviour as ISpawnPointProvider;
        //if (_spawnPoints == null && spawnPointProviderBehaviour != null)
            //Debug.LogError("[CharacterLife] spawnPointProviderBehaviour must implement ISpawnPointProvider");
    }

    public void BindBall(GameObject ball)
    {
        _ownedBall = ball;
    }

    // Ball vs Character에서 호출될 사망
    public void Die(GameObject killer = null)
    {
        if (IsDead) return;
        if (IsInvincible) return;

        IsDead = true;

        // 캐릭터가 죽으면 내 공도 파괴
        //ballSpawner?.DespawnCurrentBallByOwnerDeath();

        // TODO: 사망 연출/점수/카메라 등
        gameObject.SetActive(false);

        StartCoroutine(CoRespawn());
    }

    // Ball vs Ball로 내 공만 사라졌을 때 호출됨
    public void OnBallLostByBallVsBall()
    {
        if (IsDead) return;
        //ballSpawner?.RequestRespawnAfter(ballRespawnAfterLost);
    }

    private IEnumerator CoRespawn()
    {
        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        // 리스폰
        RespawnAtRandomPoint();

        // 완전 초기화: 새 공 스폰 + 성장 초기화
        //ballSpawner?.SpawnNow();

        // 무적
        StartCoroutine(CoInvincible(invincibleTime));
    }

    private void RespawnAtRandomPoint()
    {
        // 활성화
        gameObject.SetActive(true);

        //if (_spawnPoints != null)
        //{
        //    transform.position = _spawnPoints.GetRandomPosition();
        //    transform.rotation = _spawnPoints.GetRandomRotation();
        //}

        // TODO: 이동/AI 상태 초기화(목표 리셋, NavMeshAgent warp 등)
        IsDead = false;
    }

    private IEnumerator CoInvincible(float t)
    {
        IsInvincible = true;
        // TODO: 무적 VFX/머티리얼/콜라이더 레이어 변경 등
        yield return new WaitForSeconds(t);
        IsInvincible = false;
    }
}
