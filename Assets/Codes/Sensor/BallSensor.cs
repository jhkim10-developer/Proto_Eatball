using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public class BallSensor : MonoBehaviour, IDefeatable
{
    private BallID selfBallID;
    private BallGrowth selfGrowth;

    [Header("Rule")]
    [SerializeField] private float loseRatio = 1.05f; // 상대가 5% 이상 크면 내가 진다
    [SerializeField] private float tieEpsilon = 0.005f; // 거의 같으면 무시

    public bool IsDefeated { get; private set; }

    private void Awake()
    {
        if (selfBallID == null) selfBallID = GetComponent<BallID>();
        if (selfGrowth == null) selfGrowth = GetComponentInParent<BallGrowth>();
    }

    private void OnTriggerEnter(Collider other)
    {
        //자기 자신만 조작하도록 세팅하면 중복 해결됨. OOP 준수
        if (IsDefeated) return;

        // 상대 크기 제공자 찾기
        var otherGrowth = other.GetComponent<BallGrowth>();
        if (otherGrowth == null || selfGrowth == null) return;

        // 같은 주인(멀티볼 등) 무시
        var otherBallID = other.GetComponentInParent<BallID>();
        if (otherBallID != null && selfBallID != null && otherBallID.OwnerID == selfBallID.OwnerID)
            return;

        float myR = selfGrowth.Radius;
        float otherR = otherGrowth.Radius;

        // 동률(거의 같음) 무시
        if (Mathf.Abs(myR - otherR) <= tieEpsilon)
            return;

        // 내가 작으면 내가 진다(상대가 충분히 커야)
        if (otherR >= myR * loseRatio)
        {
            Defeat(other.gameObject);
        }

        // ==============================================================
        //// 1. Ball VS Character
        //// 캐릭터 들어오면
        //if (other.CompareTag("Character") || other.CompareTag("Player"))
        //{
        //    var charID = other.GetComponent<CharacterID>();
        //    if (charID != null && selfBallID != null && selfBallID.OwnerID == charID.masterID)
        //        return; //내 주인거라면 무시

        //    Debug.Log("[BallSensor] sensor trigger enter");
        //}

        //// 2. Ball VS Ball
        //if (other.CompareTag("Ball"))
        //{
        //    var otherBall = other.GetComponent<BallID>();
        //    if (otherBall != null && selfBallID != null && otherBall.OwnerID == selfBallID.OwnerID)
        //        return; // 같은 주인(혹시 멀티볼) 무시
        //    Debug.Log("[BallSensor] sensor trigger enter");
        //}
    }

    public void Defeat(GameObject instigator)
    {
        if (IsDefeated) return;
        IsDefeated = true;

        // 프로토타입: 꺼버리기
        gameObject.SetActive(false);

        Debug.Log($"[BallSensor] Defeated by {instigator.name}");
    }
}
