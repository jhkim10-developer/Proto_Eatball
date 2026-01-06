using UnityEngine;

public class CharacterSensor : MonoBehaviour
{
    private CharacterID selfID; // 루트에 붙은 ID 연결(또는 Awake에서 GetComponentInParent)
    [SerializeField] private CharacterBallRef myBallRef;

    [Header("Rule")]
    [SerializeField] private float loseRatio = 1.05f;
    [SerializeField] private float tieEpsilon = 0.001f;

    public bool IsDefeated { get; private set; }

    private void Awake()
    {
        if (selfID == null) selfID = GetComponent<CharacterID>();
        if (myBallRef == null) myBallRef = GetComponent<CharacterBallRef>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsDefeated) return;
        if (myBallRef == null || myBallRef.Size == null) return;

        // 상대가 Ball이면 내 공이면 무시
        if (other.CompareTag("Ball"))
        {
            var ballID = other.GetComponent<BallID>();
            if (ballID != null && ballID.OwnerID == selfID.masterID)
                return;

            Debug.Log("[CharacterSensor] Ball trigger enter");
        }

        // 상대 공 크기
        var otherGrowth = other.GetComponent<BallGrowth>();
        if (otherGrowth == null) return;

        float myR = myBallRef.Size.Radius;
        float otherR = otherGrowth.Radius;

        if (Mathf.Abs(myR - otherR) <= tieEpsilon)
            return;

        if (otherR >= myR * loseRatio)
        {
            Defeat(other.gameObject);
        }

        //// 1. Character VS Character
        //if (other.CompareTag("Character"))
        //{
        //    Debug.Log("[CharacterSensor] Character trigger enter");
        //}

        //// 2. Character VS Player
        //if (other.CompareTag("Player"))
        //{
        //    Debug.Log("[CharacterSensor] Player trigger enter");
        //}
    }

    public void Defeat(GameObject instigator)
    {
        if (IsDefeated) return;
        IsDefeated = true;

        gameObject.SetActive(false); // 또는 AI disable, Player면 GameOver 등
        Debug.Log($"[CharacterSensor] Defeated by {instigator.name}");
    }
}
