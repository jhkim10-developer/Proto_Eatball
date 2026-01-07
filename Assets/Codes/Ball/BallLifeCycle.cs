using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallLifeCycle : MonoBehaviour, IDefeatable
{
    [Header("Debris")]
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private float debrisLifeTime = 4f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1f;

    public bool IsDefeated { get; private set; }

    private BallID _ballID;

    private void Awake()
    {
        _ballID = GetComponent<BallID>();
    }

    public void Defeat(GameObject instigator)
    {
        if (IsDefeated) return;
        IsDefeated = true;

        SpawnDebris();

        // 오너에게 1초 후 재스폰 요청
        var owner = _ballID != null ? _ballID.Owner : null;
        if (owner != null && owner.TryGetComponent<BallSpawner>(out var spawner))
        {
            spawner.NotifyBallDestroyed(respawnDelay, instigator);
        }

        // 일단은 비활성 (풀링하면 여기만 바꾸면 됨)
        gameObject.SetActive(false);
    }

    private void SpawnDebris()
    {
        if (!debrisPrefab) return;

        var debris = Instantiate(debrisPrefab, transform.position, transform.rotation);
        Destroy(debris, debrisLifeTime);
    }
}
