using UnityEngine;

public class SnowPathDrawer : MonoBehaviour
{
    public ComputeShader snowComputeShader;
    public RenderTexture snowRT;

    private string snowImageProperty = "snowImage";
    private string colorValueProperty = "colorValueToAdd";
    private string resolutionProperty = "resolution";
    private string positionXProperty = "positionX";
    private string positionYProperty = "positionY";
    private string spotSizeProperty = "spotSize";

    private string drawSpotKernel = "DrawSpot";

    private Vector2Int position = new Vector2Int(256, 256);
    public float spotSize = 5f;

    private SnowController snowController;
    private GameObject[] snowControllerObjs;

    // ====== growth 관련 코드 ======
    [SerializeField] private SnowBallGrowth growth;
    [SerializeField] private float spotRadiusToBrush = 1f; // 튜닝값: 월드 반지름 -> RT 브러시로 변환 스케일
    [SerializeField] private float minSpotSize = 0.1f;
    [SerializeField] private float maxSpotSize = 20f;

    private void Awake()
    {
        snowControllerObjs = GameObject.FindGameObjectsWithTag("SnowGround");
    }

    private void FixedUpdate()
    {
        // 0) 성장값 → 브러시 크기 변환
        if (growth != null)
        {
            float desired = growth.Radius * spotRadiusToBrush;
            spotSize = Mathf.Clamp(desired, minSpotSize, maxSpotSize);
        }

        // 1) 근처 SnowGround에만 찍기
        for (int i = 0; i < snowControllerObjs.Length; i++)
        {
            if (Vector3.Distance(snowControllerObjs[i].transform.position, transform.position) > spotSize * 5f) 
                continue;

            snowController = snowControllerObjs[i].GetComponent<SnowController>();
            snowRT = snowController.snowRT;
            //snowComputeShader = snowController.snowComputeShader;
            GetPosition();
            DrawSpot();
        }
    }

    void GetPosition()
    {
        float scaleX = snowController.transform.localScale.x;
        float scaleY = snowController.transform.localScale.z;

        float snowPosX = snowController.transform.position.x;
        float snowPosY = snowController.transform.position.z;

        int posX = snowRT.width / 2 - (int) (((transform.position.x - snowPosX) * snowRT.width / 2) / scaleX);
        int posY = snowRT.width / 2 - (int)(((transform.position.z - snowPosY) * snowRT.height / 2) / scaleY);
        position = new Vector2Int(posX, posY);
    }

    void DrawSpot()
    {
        if (snowRT == null) return;
        if (snowComputeShader == null) return;

        int kernel_Handle = snowComputeShader.FindKernel(drawSpotKernel);
        snowComputeShader.SetTexture(kernel_Handle, snowImageProperty, snowRT);
        snowComputeShader.SetFloat(colorValueProperty, 0);
        snowComputeShader.SetFloat(resolutionProperty, snowRT.width);
        snowComputeShader.SetFloat(positionXProperty, position.x);
        snowComputeShader.SetFloat(positionYProperty, position.y);
        snowComputeShader.SetFloat(spotSizeProperty, spotSize);
        snowComputeShader.Dispatch(kernel_Handle, snowRT.width / 8, snowRT.height / 8, 1);
    }
}
