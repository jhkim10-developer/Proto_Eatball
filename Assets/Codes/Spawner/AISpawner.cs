using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AISpawner : MonoBehaviour
{
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

    private void Start()
    {
        SpawnAll();
    }

    public void SpawnAll()
    {
        if (aiPrefab == null)
        {
            Debug.LogWarning("[AISpawner] Missing aiPrefab.");
            return;
        }

        if (count <= 0)
        {
            Debug.LogWarning("[AISpawner] count <= 0");
            return;
        }

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("[AISpawner] Missing spawnPoints.");
            return;
        }

        PrepareUnusedIndices();

        if (_unusedIndices.Count == 0)
        {
            Debug.LogWarning("[AISpawner] No valid (non-null) spawnPoints.");
            return;
        }

        int spawnCount = Mathf.Min(count, _unusedIndices.Count);
        if (count > _unusedIndices.Count)
        {
            Debug.LogWarning($"[AISpawner] Requested {count} bots, but only {_unusedIndices.Count} spawn points available. " +
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
        // 1) 중복 없이 SpawnPoint 하나 선택
        int pick = Random.Range(0, _unusedIndices.Count);
        int idx = _unusedIndices[pick];
        _unusedIndices.RemoveAt(pick);

        Vector3 basePos = spawnPoints[idx].position;

        // 2) 패턴 방지용 jitter
        Vector2 jitter = Random.insideUnitCircle * spawnJitter;
        Vector3 candidate = basePos + new Vector3(jitter.x, 0f, jitter.y);

        // 3) NavMesh 위로 보정
        if (NavMesh.SamplePosition(candidate, out var hit, sampleRadius, NavMesh.AllAreas))
            return hit.position;

        // 실패 시 포인트 그대로 사용
        return basePos;
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
