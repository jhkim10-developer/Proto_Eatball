using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    [SerializeField] private RectTransform bgRT;
    [SerializeField] private RectTransform leverRT;
    [SerializeField, Range(10f, 150f)] private float leverRange = 80f;
    [SerializeField] private GameObject guide;

    [Header("Canvas Injection")]
    [SerializeField] private Canvas targetCanvas;              // 외부에서 주입 가능
    [SerializeField] private RectTransform canvasRootRT;       // targetCanvas의 root RectTransform 캐시
    [SerializeField] private Camera uiCamera;                  // ScreenSpace-Camera / WorldSpace일 때 필요

    [Header("Input")]
    [SerializeField] private bool useMagnitude = true;         // true면 살짝 밀면 약하게, 끝까지 밀면 강하게

    public event Action<Vector2> OnInputVectorEvent;
    public event Action OnInitialTouchEvent;

    private Vector2 inputVector;       // -1~1
    private Vector2 pressLocalPos;     // 캔버스 로컬 좌표
    private bool initialTouch;
    private bool isTouchLock;
    private bool isPressed;
    private float idleTimer;

    private void Awake()
    {
        // 인스펙터로 주입 안 됐으면 자기 상위에서라도 찾기(안전장치)
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas != null)
        {
            canvasRootRT = targetCanvas.GetComponent<RectTransform>();
            uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
        }
    }

    /// <summary>
    /// 외부에서 명시적으로 Canvas를 주입하고 싶을 때 호출.
    /// </summary>
    public void SetCanvas(Canvas canvas)
    {
        targetCanvas = canvas;
        canvasRootRT = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
    }

    private void Start()
    {
        ActivateJoyStick(true);
    }

    public void Initialize(Vector2 normalizedAnchor) // 예: (0.5, 0.3)
    {
        guide.SetActive(true);
        if (canvasRootRT != null)
        {
            // Canvas 크기 기준으로 초기 위치 배치
            var size = canvasRootRT.rect.size;
            bgRT.anchoredPosition = new Vector2(size.x * normalizedAnchor.x, size.y * normalizedAnchor.y);
        }
        bgRT.gameObject.SetActive(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isTouchLock) return;
        if (canvasRootRT == null) return;

        if (!initialTouch)
        {
            OnInitialTouchEvent?.Invoke();
            initialTouch = true;
        }

        isPressed = true;
        idleTimer = 0f;

        // 스크린 좌표 -> Canvas 로컬 좌표
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRootRT, eventData.position, uiCamera, out var localPos))
        {
            pressLocalPos = localPos;
            bgRT.anchoredPosition = localPos;
            bgRT.gameObject.SetActive(true);
            guide.SetActive(false);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed) return;

        isPressed = false;
        SendInput(Vector2.zero);

        bgRT.gameObject.SetActive(false);
        leverRT.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isPressed) return;
        ControlJoystickLever(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isPressed) return;
        ControlJoystickLever(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 필요 시 구현
    }

    private void ControlJoystickLever(PointerEventData eventData)
    {
        if (canvasRootRT == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRootRT, eventData.position, uiCamera, out var localPos))
            return;

        var inputDir = localPos - pressLocalPos; // 캔버스 로컬에서의 이동량
        var clampedDir = inputDir.magnitude < leverRange ? inputDir : inputDir.normalized * leverRange;

        leverRT.anchoredPosition = clampedDir;
        inputVector = clampedDir / leverRange; // -1~1, magnitude 포함
    }

    private void FixedUpdate()
    {
        if (!isPressed) return;

        // 기존 코드는 normalized로 "항상 최대 입력"이었음.
        // 선택적으로 magnitude 유지 가능.
        var v = useMagnitude ? inputVector : (inputVector.sqrMagnitude > 0 ? inputVector.normalized : Vector2.zero);
        SendInput(v);
    }

    private void Update()
    {
        if (bgRT.gameObject.activeInHierarchy || isTouchLock) return;
        if (isPressed) return;

        idleTimer += Time.deltaTime;
        if (idleTimer > 10f)
        {
            idleTimer = 0f;
            ActivateJoyStick(true);
        }
    }

    public void ActivateJoyStick(bool isActive)
    {
        isTouchLock = !isActive;

        if (isActive)
        {
            // 기본 위치(원하면 Initialize로 외부에서 잡아도 됨)
            if (canvasRootRT != null)
            {
                var size = canvasRootRT.rect.size;
                bgRT.anchoredPosition = new Vector2(size.x * 0.5f, size.y * 0.3f);
            }
        }

        bgRT.gameObject.SetActive(isActive);
        guide.SetActive(isActive);
    }

    public void ControlOff()
    {
        // null 이벤트데이터 안 넘기도록 내부 리셋로 처리
        isPressed = false;
        SendInput(Vector2.zero);
        bgRT.gameObject.SetActive(false);
        leverRT.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;
    }

    private void SendInput(Vector2 v)
    {
        OnInputVectorEvent?.Invoke(v);
    }
}
