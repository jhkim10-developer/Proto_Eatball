using UnityEngine;

public class BallSensor : MonoBehaviour
{
    private BallID _selfBallID;
    private BallGrowth _selfGrowth;
    private IDefeatable _selfDefeatable;

    [Header("Rule")]
    [SerializeField] private float tieEpsilon = 0.005f; // 거의 같으면 무시

    private void Awake()
    {
        _selfBallID = GetComponent<BallID>();
        _selfGrowth = GetComponent<BallGrowth>();
        _selfDefeatable = GetComponent<IDefeatable>(); // BallLifeCycle
    }

    private void OnTriggerEnter(Collider other)
    {
        // ==== 자기 자신만 조작하도록 세팅하면 중복 해결됨. OOP 준수 ====
        // 이미 죽은 공이면 무시
        if (_selfDefeatable != null && _selfDefeatable.IsDefeated) return;

        // 1) Ball vs Character (or Player)
        if (other.CompareTag("Character") || other.CompareTag("Player"))
        {
            var charID = other.GetComponent<CharacterID>();
            if (_selfBallID != null && charID != null && _selfBallID.OwnerID == charID.masterID)
                return; // 내 주인이면 무시

            // 캐릭터 무적/사망 상태면 처리 스킵(권장)
            var characterLife = other.GetComponent<CharacterLifeCycle>();
            if (characterLife != null && (characterLife.IsDefeated || characterLife.IsInvulnerable))
                return;

            // 1) 캐릭터와 딸린 공 파괴
            other.GetComponent<IDefeatable>()?.Defeat(gameObject);

            return;
        }

        // 2) Ball vs Ball
        if (!other.CompareTag("Ball")) return;

        // 같은 주인 무시
        var otherBallID = other.GetComponent<BallID>();
        if (otherBallID != null && _selfBallID != null && otherBallID.OwnerID == _selfBallID.OwnerID)
            return;

        var otherGrowth = other.GetComponent<BallGrowth>();
        if (_selfGrowth == null || otherGrowth == null) return;

        float myR = _selfGrowth.Radius;
        float otherR = otherGrowth.Radius;

        if (Mathf.Abs(myR - otherR) <= tieEpsilon) return;

        if (myR > otherR)
        {
            other.GetComponent<IDefeatable>()?.Defeat(gameObject);
        }
        else
        {
            _selfDefeatable?.Defeat(other.gameObject);
        }
    }
}
