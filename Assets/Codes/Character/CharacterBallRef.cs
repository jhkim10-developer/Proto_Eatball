using UnityEngine;

public sealed class CharacterBallRef : MonoBehaviour
{
    [SerializeField] private BallGrowth ballGrowth;
    public IBallSizeProvider Size => ballGrowth;

    public void Bind(BallGrowth growth) => ballGrowth = growth;
}
