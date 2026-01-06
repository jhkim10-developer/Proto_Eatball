using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallDestruction : MonoBehaviour
{
    [SerializeField] private BallID ballID;
    [SerializeField] private BallGrowth growth;

    private CharacterLife _ownerLife;
    private bool _destroyed;

    public int OwnerId => ballID != null ? ballID.OwnerID : 0;

    private void Awake()
    {
        if (ballID == null) ballID = GetComponent<BallID>();
        if (growth == null) growth = GetComponentInParent<BallGrowth>();
    }

    public void BindOwner(CharacterLife ownerLife)
    {
        _ownerLife = ownerLife;
    }

    public void DestroyByBallVsBall()
    {
        if (_destroyed) return;
        _destroyed = true;

        LeaveTrace();
        gameObject.SetActive(false);

        // 캐릭터는 살아있고 공만 잃음 → 0.5초 후 다시 굴릴 수 있게 요청
        _ownerLife?.OnBallLostByBallVsBall();
    }

    public void DestroyByOwnerDeath()
    {
        if (_destroyed) return;
        _destroyed = true;

        LeaveTrace();
        gameObject.SetActive(false);

        // 주인이 죽은 케이스는 재스폰 요청 없음(리스폰 시 완전 초기화에서 새 공 생성)
    }

    private void LeaveTrace()
    {
        // TODO: 나중에 눈 흔적 남기기 (SnowController/SnowPathDrawer 등과 연결)
        // 지금은 훅만.
    }
}
