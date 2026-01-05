using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ClickToSpawnDecal : MonoBehaviour
{
    [Tooltip("Assign a Decal Projector prefab here (must have a DecalProjector component).")]
    public DecalProjector decalPrefab;

    [Tooltip("Which layers count as 'ground' to raycast against.")]
    public LayerMask groundMask = ~0;

    [Tooltip("Size (X,Y) of the decal. Z is projection depth and can stay small.")]
    public Vector2 decalSize = new Vector2(2f, 2f);
    public float projectionDepth = 0.1f;

    [Tooltip("Seconds before the spawned decal is destroyed.")]
    public float lifetime = 10f;

    Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        if (_cam == null)
            Debug.LogError("ClickToSpawnDecal: No main camera found.");
        if (decalPrefab == null)
            Debug.LogError("ClickToSpawnDecal: Please assign a DecalProjector prefab.");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TrySpawnDecalAtMouse();
    }

    void TrySpawnDecalAtMouse()
    {
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, Mathf.Infinity, groundMask))
        {
            // instantiate and orient decal
            var splat = Instantiate(decalPrefab,
                hit.point + hit.normal * 0.01f,
                Quaternion.LookRotation(-hit.normal),
                transform);

            // set size (X, Y) and small depth (Z)
            splat.size = new Vector3(decalSize.x, decalSize.y, projectionDepth);

            Destroy(splat.gameObject, lifetime);
        }
    }
}
