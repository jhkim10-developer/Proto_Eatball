using UnityEngine;

public class BallID : MonoBehaviour
{
    public GameObject OwnerRoot { get; private set; }  // Player/AI 루트
    public int OwnerID { get; private set; }          // 빠른 비교용

    public void BindOwner(GameObject ownerRoot)
    {
        OwnerRoot = ownerRoot;
        OwnerID = ownerRoot != null ? ownerRoot.GetInstanceID() : 0;
    }


    [SerializeField] private int OwnerIDDebug;

    private void Start()
    {
        OwnerIDDebug = OwnerID;
    }
}
