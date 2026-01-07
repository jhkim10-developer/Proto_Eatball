using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GlobalSpawner : MonoBehaviour
{
    public static GlobalSpawner Instance { get; private set; }

    [Header("Spawn")]
    [SerializeField] private GameObject aiPrefab;
    [SerializeField] private int count = 7;

    [Header("Spawn Points")]
    [Tooltip("AI가 소환될 위치들 (중복 없이 사용됨)")]
    [SerializeField] private List<Transform> spawnPoints = new();

    [Tooltip("SpawnPoint에서 살짝 흔들기 (패턴 방지)")]
    [SerializeField] private float spawnJitter = 1.5f;

    [Tooltip("NavMesh.SamplePosition 검색 반경")]
    [SerializeField] private float sampleRadius = 2.0f;

    [Header("AI Tuning (Random Range)")]
    [SerializeField] private Vector2 aiSpeedRange = new Vector2(2.8f, 3.4f);
    [SerializeField] private Vector2 aiAccelerationRange = new Vector2(10f, 16f);
    [SerializeField] private Vector2 aiAngularSpeedRange = new Vector2(300f, 600f);

    private readonly List<int> _unusedIndices = new();

    private readonly List<int> _deck = new();
    private int _deckCursor = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        BuildDeck();
        SpawnAllAIBot();        
    }

    public void SpawnAllAIBot()
    {
        if (aiPrefab == null)
        {
            Debug.LogWarning("[GlobalSpawner] Missing aiPrefab.");
            return;
        }

        if (count <= 0)
        {
            Debug.LogWarning("[GlobalSpawner] count <= 0");
            return;
        }

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("[GlobalSpawner] Missing spawnPoints.");
            return;
        }

        PrepareUnusedIndices();

        if (_unusedIndices.Count == 0)
        {
            Debug.LogWarning("[GlobalSpawner] No valid (non-null) spawnPoints.");
            return;
        }

        int spawnCount = Mathf.Min(count, _unusedIndices.Count);
        if (count > _unusedIndices.Count)
        {
            Debug.LogWarning($"[GlobalSpawner] Requested {count} bots, but only {_unusedIndices.Count} spawn points available. " +
                             $"Spawning {spawnCount} bots.");
        }

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 pos = GetSpawnPosition();
            SpawnOne(pos, i);
        }
    }

    private void PrepareUnusedIndices()
    {
        _unusedIndices.Clear();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
                _unusedIndices.Add(i);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
            return transform.position;

        // 혹시 덱에 null이 섞여있을 수 있으니 몇 번 재시도
        for (int attempt = 0; attempt < 10; attempt++)
        {
            int idx = NextSpawnIndex();
            var sp = spawnPoints[idx];
            if (sp == null) continue;

            Vector3 basePos = sp.position;

            Vector2 jitter = Random.insideUnitCircle * spawnJitter;
            Vector3 candidate = basePos + new Vector3(jitter.x, 0f, jitter.y);

            if (NavMesh.SamplePosition(candidate, out var hit, sampleRadius, NavMesh.AllAreas))
                return hit.position;

            return basePos;
        }

        // 최후 fallback
        return transform.position;
    }

    private void SpawnOne(Vector3 position, int index)
    {
        GameObject bot = Instantiate(aiPrefab, position, Quaternion.identity);
        bot.name = $"AI_{index:00}";

        // NavMeshAgent 튜닝 자동 적용
        var agent = bot.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = Random.Range(aiSpeedRange.x, aiSpeedRange.y);
            agent.acceleration = Random.Range(aiAccelerationRange.x, aiAccelerationRange.y);
            agent.angularSpeed = Random.Range(aiAngularSpeedRange.x, aiAngularSpeedRange.y);
        }

        // Wander/Brain 파라미터 자동 바인딩(필요하면)
        // 예: AIBrainNavmeshWander의 roamRadius를 spawnRadius와 맞춘다거나
        var wander = bot.GetComponent<AIBrainNavmeshWander>();
        if (wander != null)
        {
            // wander가 public setter가 없으면 인스펙터 값 그대로 사용해도 OK
            // 필요 시 AIBrainNavmeshWander에 SetRoamRadius() 같은 API를 추가 추천
        }

        // SnowBall 자동 스폰
        var snowBallSpawner = bot.GetComponent<BallSpawner>();
        if (snowBallSpawner != null)
        {
            snowBallSpawner.TrySpawn();
        }

        bot.name = $"AI_{index:00}";
    }

    public void RequestRespawn(CharacterLifeCycle target, float respawnDelay, float invulnDuration)
    {
        if (target == null) return;
        StartCoroutine(CoRespawnCharacter(target, respawnDelay, invulnDuration));
    }

    private IEnumerator CoRespawnCharacter(CharacterLifeCycle target, float delay, float invulnDuration)
    {
        yield return new WaitForSeconds(delay);

        if (target == null) yield break; // 파괴됐으면 종료

        // 1) 스폰 위치 다시 뽑기
        Vector3 pos = GetSpawnPosition();

        // 2) 위치/에이전트 리셋(비활성 상태에서도 Transform 변경은 가능)
        target.transform.position = pos;
        target.transform.rotation = Quaternion.identity;

        // 3) 활성화
        target.gameObject.SetActive(true);

        // 4) 상태 리셋 + 무적 부여 (API로 분리 추천)
        target.ResetForRespawn(invulnDuration);

        // 5) 공 스폰 (원하면 즉시)
        target.GetComponent<BallSpawner>()?.NotifyOwnerRespawned(spawnImmediately: true);
    }

    private void BuildDeck()
    {
        _deck.Clear();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
                _deck.Add(i);
        }

        Shuffle(_deck);
        _deckCursor = 0;
    }

    private int NextSpawnIndex()
    {        
        if (_deck.Count == 0)
            BuildDeck();

        if (_deckCursor >= _deck.Count)
        {
            Shuffle(_deck);
            _deckCursor = 0;
        }

        return _deck[_deckCursor++];
    }

    private static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] == null) continue;
            Gizmos.DrawWireSphere(spawnPoints[i].position, 0.3f);
        }
    }
#endif
}
