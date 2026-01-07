using UnityEngine;

public sealed class CharacterBallRef : MonoBehaviour
{
    [SerializeField] private BallLifeCycle ball; // 또는 MonoBehaviour로 두고 IDefeatable 캐스팅

    public BallLifeCycle Ball => ball;

    public void Bind(BallLifeCycle ballLife)
    {
        ball = ballLife;
    }

    public void Clear()
    {
        ball = null;
    }

    // 캐릭터가 자기 공을 파괴하라고 명령하는 API
    public void DestroyMyBall(GameObject instigator)
    {
        ball?.Defeat(instigator);
    }
}
