using System.Threading;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIBrainNavmeshWander : MonoBehaviour
{
    [Header("Wander")]
    [SerializeField] private float roamRadius = 12f;
    [SerializeField] private float repathInterval = 1.5f;
    [SerializeField] private float arriveDistance = 1.0f;
    private NavMeshAgent _agent;
    private float _nextRepathTime;

    [Header("Flee")]
    [SerializeField] private float fleeDistance = 8f;
    private Transform _threat;
    private float _fleeUntil;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.autoBraking = true;
    }

    private void OnEnable()
    {
        PickNewDestination();
    }

    private void Update()
    {
        //도망
        if (_threat != null && Time.time < _fleeUntil)
        {
            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + repathInterval;
                PickFleeDestination();
            }
            return;
        }

        _threat = null;

        //방황
        if (Time.time < _nextRepathTime) return;
        _nextRepathTime = Time.time + repathInterval;

        if (!_agent.hasPath || _agent.remainingDistance <= Mathf.Max(arriveDistance, _agent.stoppingDistance))
        {
            PickNewDestination();
        }
    }

    private void PickNewDestination()
    {
        Vector3 random = Random.insideUnitSphere * roamRadius;
        random.y = 0f;

        Vector3 target = transform.position + random;

        if (NavMesh.SamplePosition(target, out var hit, roamRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }

    // 디버그용: 씬에서 roamRadius를 볼 수 있게
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, roamRadius);
    }

    public void FleeFrom(Transform threat, float duration)
    {
        _threat = threat;
        _fleeUntil = Time.time + duration;
        PickFleeDestination();
    }

    private void PickFleeDestination()
    {
        if (_threat == null) return;

        Vector3 away = (transform.position - _threat.position);
        away.y = 0f;

        if (away.sqrMagnitude < 0.01f)
            away = Random.insideUnitSphere;

        Vector3 target = transform.position + away.normalized * fleeDistance;

        if (NavMesh.SamplePosition(target, out var hit, fleeDistance, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }
}
