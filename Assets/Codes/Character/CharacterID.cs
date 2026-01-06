using UnityEngine;

public class CharacterID : MonoBehaviour
{
    public int masterID => gameObject.GetInstanceID();
    public Transform Root => transform;

    [SerializeField] private int masterIDDebug;

    private void Start()
    {
        masterIDDebug = masterID;
    }
}
