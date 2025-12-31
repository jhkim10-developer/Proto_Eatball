using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Joystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] RectTransform bgRT; //조이스틱 배경 원.
    [SerializeField] RectTransform leverRT; //조이스틱 레버(손잡이), 드래그 방향으로 움직임.
    [SerializeField, Range(10f, 150f)] float leverRange; // 레버가 베이스 중심에서 움직일 수 있는 최대 거리(픽셀).
    [SerializeField] GameObject guide; //여기 터치해서 움직이세요 같은 가이드 UI

    Vector2 inputVector;
    public event Action<Vector2> OnInputVectorEvent;
    public event Action OnInitialTouchEvent;

    private bool initialTouch;
    Vector2 screenSize;
    Vector2 pressPos;

    float time = 0;
    private bool isTouchLock;
    private bool isPressed;

    private void Start()
    {
        Rect rect = transform.GetComponent<RectTransform>().rect;
        screenSize = new Vector2(rect.width, rect.height);
        ActivateJoyStick(true);
    }

    public void Initialize()
    {
        guide.SetActive(true);
        bgRT.anchoredPosition = new Vector2(screenSize.x * 0.5f, screenSize.y * 0.3f);
        bgRT.gameObject.SetActive(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isTouchLock) return;

        if (!initialTouch)
        {
            OnInitialTouchEvent?.Invoke();
            initialTouch = true;
        }

        isPressed = true;
        pressPos = eventData.position;

        Vector2 viewPos = Camera.main.ScreenToViewportPoint(pressPos);
        bgRT.anchoredPosition = screenSize * viewPos;
        bgRT.gameObject.SetActive(true);

        guide.SetActive(false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed) return;

        isPressed = false;
        OnInputVectorEvent?.Invoke(Vector2.zero);

        time = 0;
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
        // 필요시 구현
    }

    public void ControlJoystickLever(PointerEventData eventData)
    {
        var inputDir = eventData.position - pressPos;
        var clampedDir = inputDir.magnitude < leverRange ? inputDir : inputDir.normalized * leverRange;
        leverRT.anchoredPosition = clampedDir;
        inputVector = clampedDir / leverRange;
    }

    private void FixedUpdate()
    {
        if (isPressed)
        {
            OnInputVectorEvent?.Invoke(inputVector.normalized);
        }
    }

    private void Update()
    {
        if (bgRT.gameObject.activeInHierarchy || isTouchLock) return;

        if (!isPressed)
        {
            time += Time.deltaTime;
            if (time > 10)
            {
                time = 0;
                ActivateJoyStick(true);
            }
        }
    }

    public void ActivateJoyStick(bool isActive)
    {
        isTouchLock = !isActive;
        bgRT.anchoredPosition = new Vector2(screenSize.x * 0.5f, screenSize.y * 0.3f);
        bgRT.gameObject.SetActive(isActive);
        guide.SetActive(isActive);
    }

    public void ControllOff()
    {
        OnEndDrag(null);
        OnPointerUp(null);
    }
}
