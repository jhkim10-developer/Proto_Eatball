using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// 실제 충돌이 나는 모든 자식 Collider 에 붙어서
/// 부모의 CrackSpawner.HandleCollision 로 이벤트를 포워드해 줍니다.
/// </summary>
public class CollisionProxy : MonoBehaviour
{
    [HideInInspector] public CrackSpawner spawner;

    void OnCollisionEnter(Collision col)
    {
        // 부모 스포너로 그대로 뿌려 줍니다.
        spawner?.HandleCollision(col);
    }
}

/// <summary>
/// Player 루트에 붙여 두는 메인 스크립트.
/// 충돌이 일어나면 Base_Body 메시(local)에 데칼을 찍습니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CrackSpawner : MonoBehaviour
{
    [Header("Decal Settings")]
    public DecalProjector crackDecalPrefab; // ▶ 프로젝트 뷰의 DecalProjector Prefab
    public Transform       decalParent;     // ▶ Base_Body(스킨드메시) Transform
    public int             maxCracks     = 6;
    public float           decalSize     = 1.2f;
    public float           decalDepth    = 0.15f; // Projection Depth
    public float           decalLifetime = 5f;

    [Header("Collision Settings")]
    public string collisionTag = "Player"; // Player 루트 태그
    public float  minForce     = 1f;       // 최소 충돌 강도

    // 생성된 데칼을 FIFO로 보관
    private Queue<DecalProjector> _decals = new Queue<DecalProjector>();

    void Awake()
    {
        // 이 GameObject(루트) 하위 모든 Collider 에 Proxy 붙이기
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            if (col.isTrigger) continue;
            var proxy = col.gameObject.AddComponent<CollisionProxy>();
            proxy.spawner = this;
        }
    }

    /// <summary>
    /// CollisionProxy 에서 호출됩니다.
    /// </summary>
    public void HandleCollision(Collision col)
    {
        // 1) 충돌한 콜라이더가 속한 최상위 루트가 “Player” 태그인지 체크
        var root = col.collider.transform.root;
        if (!root.CompareTag(collisionTag)) return;

        // 2) 충돌 세기 체크
        if (col.relativeVelocity.magnitude < minForce) return;

        // 3) 충돌 지점 가져와서 데칼 찍기
        var contact = col.contacts[0];
        SpawnCrack(contact.point, contact.normal);
    }

    void SpawnCrack(Vector3 worldPos, Vector3 normal)
    {
        // 최대 개수 초과 시, 오래된 애부터 지우기
        if (_decals.Count >= maxCracks)
        {
            var old = _decals.Dequeue();
            if (old) Destroy(old.gameObject);
        }

        // 월드→로컬 변환
        Vector3 localPos    = decalParent.InverseTransformPoint(worldPos);
        Vector3 localNormal = decalParent.InverseTransformDirection(normal);

        // Base_Body 자식으로 Instatiate → 함께 움직임
        var decal = Instantiate(crackDecalPrefab, decalParent);
        decal.transform.localPosition = localPos + localNormal * 0.01f;
        decal.transform.localRotation = Quaternion.LookRotation(-localNormal);
        decal.size = new Vector3(decalSize, decalSize, decalDepth);

        _decals.Enqueue(decal);
        Destroy(decal.gameObject, decalLifetime);
    }
}
