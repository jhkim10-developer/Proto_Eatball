using UnityEngine;

public class CamFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0, 8, -8);
    [SerializeField] float smoothTime = 0.15f;

    Vector3 velocity;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );
    }
}
