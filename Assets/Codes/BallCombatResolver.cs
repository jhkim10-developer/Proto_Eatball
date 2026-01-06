using UnityEngine;

public class BallCombatResolver : MonoBehaviour
{
    public static BallCombatResolver Instance { get; private set; }

    [Header("Rule")]
    [SerializeField] private float killRatio = 1.05f; // 5% 더 커야 잡아먹음(튜닝값)

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void TryResolve(GameObject a, GameObject b)
    {
        if (a == null || b == null) return;

        // 중복 판정 방지: 항상 InstanceID 작은 쪽만 판정한다
        if (a.GetInstanceID() > b.GetInstanceID())
            return;

        // A 또는 B에서 "볼의 크기"를 뽑는다 (SnowBallGrowth 사용)
        var aSize = a.GetComponent<BallGrowth>();
        var bSize = b.GetComponent<BallGrowth>();

        if (aSize == null || bSize == null) return;

        float ar = aSize.Radius;
        float br = bSize.Radius;

        if (ar >= br * killRatio)
        {
            Kill(b, a);
        }
        else if (br >= ar * killRatio)
        {
            Kill(a, b);
        }
        // else: 비슷하면 아무 일 없음
    }

    private void Kill(GameObject deadSide, GameObject killerSide)
    {
        var deadRoot = deadSide.GetComponentInParent<BallGrowth>()?.gameObject;
        if (deadRoot == null) return;

        deadRoot.SetActive(false);

        Debug.Log($"[BallCombatResolver] {killerSide.name} killed {deadRoot.name}");
    }
}
