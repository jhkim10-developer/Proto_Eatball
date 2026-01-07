using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class CharacterLifeCycle : MonoBehaviour, IDefeatable
{
    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1f;
    [SerializeField] private float invulnDuration = 1f;

    [Header("Debris")]
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private float debrisLifeTime = 4f;

    public bool IsDefeated { get; private set; }
    public bool IsInvulnerable { get; private set; }

    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public void Defeat(GameObject instigator)
    {
        if (IsDefeated || IsInvulnerable) return;
        IsDefeated = true;

        GetComponent<BallSpawner>()?.NotifyOwnerDefeated();
        GetComponent<CharacterBallRef>()?.DestroyMyBall(gameObject);

        SpawnDebris();
        GlobalSpawner.Instance.RequestRespawn(this, respawnDelay: 1.5f, invulnDuration: 1.5f);
        SetAlive(false);
    }

    private void SetAlive(bool alive)
    {
        this.gameObject.SetActive(alive);
    }

    public void ResetForRespawn(float invulnDuration)
    {
        IsDefeated = false;
        IsInvulnerable = true;

        // NavMeshAgent/Animator/Collider 등 다시 켜기 (네 프로젝트 구성에 맞게)
        // 예: _agent.enabled = true; _agent.Warp(transform.position);

        GlobalSpawner.Instance.StartCoroutine(CoInvuln(invulnDuration));
    }

    private IEnumerator CoInvuln(float t)
    {
        yield return new WaitForSeconds(t);
        IsInvulnerable = false;
    }

    private void SpawnDebris()
    {
        if (!debrisPrefab) return;
        var debris = Instantiate(debrisPrefab, transform.position, transform.rotation);
        Destroy(debris, debrisLifeTime);
    }
}
