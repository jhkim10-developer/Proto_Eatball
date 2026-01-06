using UnityEngine;

public interface IDefeatable
{
    void Defeat(GameObject instigator);
    bool IsDefeated { get; }
}
