using UnityEngine;

public class CharacterSensor : MonoBehaviour
{
    private CharacterID selfID; // 루트에 붙은 ID 연결(또는 Awake에서 GetComponentInParent)

    private void Awake()
    {
        if (selfID == null)
            selfID = GetComponent<CharacterID>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            var ballID = other.GetComponent<BallID>();
            if (ballID != null && ballID.OwnerID == selfID.masterID)
                return; // 내 공이면 무시

            Debug.Log("[CharacterSensor] Ball trigger enter");
        }

        // 1. Character VS Character
        if (other.CompareTag("Character"))
        {
            Debug.Log("[CharacterSensor] Character trigger enter");
        }

        // 2. Character VS Player
        if (other.CompareTag("Player"))
        {
            Debug.Log("[CharacterSensor] Player trigger enter");
        }
    }
}
