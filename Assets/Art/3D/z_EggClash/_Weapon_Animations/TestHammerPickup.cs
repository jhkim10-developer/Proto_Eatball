using UnityEngine;

public class TestWeaponSystem : MonoBehaviour
{
    [System.Serializable]
    public class WeaponConfig
    {
        [Tooltip("무기 이름")]
        public string name = "Weapon";
        
        [Tooltip("무기 프리팹")]
        public GameObject prefab;
        
        [Tooltip("왼손 무기 프리팹 (비워두면 오른손만 사용)")]
        public GameObject leftHandPrefab;
        
        [Tooltip("상반신 애니메이션 레이어 (예: Hammer_Upper)")]
        public string upperLayer = "";
        
        [Tooltip("하반신 애니메이션 레이어 (예: LowerBody)")]
        public string lowerLayer = "";
    }
    
    [Header("Movement")]
    [Tooltip("걷기 속도 (Shift를 누르지 않았을 때)")]
    public float walkSpeed = 2f;
    
    [Tooltip("달리기 속도 (Shift를 눌렀을 때)")]
    public float runSpeed = 5f;
    
    [Range(0.01f, 0.3f)]
    [Tooltip("회전 부드러움 (낮을수록 빠르게 회전, 0.05=즉각반응, 0.15=느린회전)")]
    public float rotationSmoothTime = 0.1f;
    
    [Range(0.05f, 0.5f)]
    [Tooltip("가속 시간 (움직임 시작/속도 증가할 때, 낮을수록 빠르게 가속)")]
    public float accelerationTime = 0.2f;
    
    [Range(0.5f, 10f)]
    [Tooltip("프레임당 최대 속도 변화량 (낮을수록 부드럽게 변화, 높을수록 즉각 반응)")]
    public float maxSpeedChangeRate = 5f;
    
    [Header("References")]
    [Tooltip("캐릭터의 Animator 컴포넌트")]
    public Animator animator;
    
    [Tooltip("오른손 무기 본 (Weapon_R, Weapon_Hammer_01 등)")]
    public Transform weaponRTransform;
    
    [Tooltip("왼손 무기 본 (Weapon_L 등)")]
    public Transform weaponLTransform;
    
    [Header("Weapons")]
    [Tooltip("사용 가능한 무기 목록 (1,2,3,4 키로 선택)")]
    public WeaponConfig[] weaponList;
    
    [Tooltip("게임 시작 시 장착할 무기 번호 (-1=빈손, 0=첫번째 무기)")]
    public int startWeaponIndex = -1;
    
    private Rigidbody rb;
    private GameObject equippedWeaponObj;
    private GameObject equippedLeftWeaponObj;
    private bool hasWeapon = false;
    private int activeWeaponIndex = -1;
    
    private int activeUpperLayer = -1;
    private int activeLowerLayer = -1;
    
    private bool useRigidbodyMovement = false;
    private Vector3 moveDirection;
    private bool isMoving;
    private float currentMoveSpeed;
    private float moveSpeedVelocity;
    private float currentRotationVelocity;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            useRigidbodyMovement = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (weaponRTransform == null)
        {
            string[] possibleNames = { "Weapon_Hammer_01", "Weapon_R", "weapon_r", "WeaponR", "R Hand" };
            
            foreach (string boneName in possibleNames)
            {
                weaponRTransform = FindBone(transform, boneName);
                if (weaponRTransform != null)
                {
                    Debug.Log($"✓ 오른손 본 '{boneName}' 찾음");
                    break;
                }
            }
        }
        
        if (weaponLTransform == null)
        {
            string[] possibleNames = { "Weapon_L", "weapon_l", "WeaponL", "L Hand" };
            
            foreach (string boneName in possibleNames)
            {
                weaponLTransform = FindBone(transform, boneName);
                if (weaponLTransform != null)
                {
                    Debug.Log($"✓ 왼손 본 '{boneName}' 찾음");
                    break;
                }
            }
        }
        
        if (startWeaponIndex >= 0 && startWeaponIndex < weaponList.Length)
        {
            SwitchWeapon(startWeaponIndex);
        }
    }
    
    void Update()
    {
        HandleMovement();
        HandleInput();
        HandleAttack();
    }
    
    void FixedUpdate()
    {
        if (useRigidbodyMovement && rb != null)
        {
            if (isMoving)
            {
                rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
            }
            else
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }
    }
    
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        Vector3 dir = new Vector3(h, 0f, v).normalized;
        float targetMoveSpeed = 0f;
        
        if (dir.magnitude >= 0.1f)
        {
            isMoving = true;
            
            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref currentRotationVelocity, rotationSmoothTime);
            
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float speed = isRunning ? runSpeed : walkSpeed;
            moveDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * speed;
            
            targetMoveSpeed = isRunning ? 1f : 0.5f;
            animator.SetBool("IsRun", isRunning);
        }
        else
        {
            isMoving = false;
            moveDirection = Vector3.zero;
            targetMoveSpeed = 0f;
            animator.SetBool("IsRun", false);
            
            currentMoveSpeed = 0f;
            moveSpeedVelocity = 0f;
            animator.SetFloat("MoveSpeed", 0f);
            return;
        }
        
        float smoothTime = accelerationTime;
        float newSpeed = Mathf.SmoothDamp(currentMoveSpeed, targetMoveSpeed, ref moveSpeedVelocity, smoothTime);
        
        float maxChange = maxSpeedChangeRate * Time.deltaTime;
        newSpeed = Mathf.Clamp(newSpeed, currentMoveSpeed - maxChange, currentMoveSpeed + maxChange);
        
        currentMoveSpeed = Mathf.Clamp(newSpeed, 0f, 1f);
        animator.SetFloat("MoveSpeed", currentMoveSpeed);
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && weaponList.Length > 0)
            SwitchWeapon(0);
        
        if (Input.GetKeyDown(KeyCode.Alpha2) && weaponList.Length > 1)
            SwitchWeapon(1);
        
        if (Input.GetKeyDown(KeyCode.Alpha3) && weaponList.Length > 2)
            SwitchWeapon(2);
        
        if (Input.GetKeyDown(KeyCode.Alpha4) && weaponList.Length > 3)
            SwitchWeapon(3);
        
        if (Input.GetKeyDown(KeyCode.E) && hasWeapon)
            DropWeapon();
    }
    
    void HandleAttack()
    {
        if (hasWeapon && Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("Attack");
        }
    }
    
    void SwitchWeapon(int index)
    {
        if (index < 0 || index >= weaponList.Length)
            return;
        
        if (hasWeapon && activeWeaponIndex == index)
            return;
        
        WeaponConfig config = weaponList[index];
        
        // ✅ 수정: 오른손과 왼손 모두 없을 때만 에러
        if (config.prefab == null && config.leftHandPrefab == null)
        {
            Debug.LogError($"무기 {index} 프리팹 없음!");
            return;
        }
        
        // 기존 무기 제거
        if (equippedWeaponObj != null)
            Destroy(equippedWeaponObj);
        
        if (equippedLeftWeaponObj != null)
            Destroy(equippedLeftWeaponObj);
        
        DisableLayers();
        
        // ✅ 수정: 오른손 프리팹이 있을 때만 장착
        if (config.prefab != null && weaponRTransform != null)
        {
            equippedWeaponObj = Instantiate(config.prefab, weaponRTransform);
            equippedWeaponObj.transform.localPosition = Vector3.zero;
            equippedWeaponObj.transform.localRotation = Quaternion.identity;
            equippedWeaponObj.transform.localScale = Vector3.one;
            
            Rigidbody weaponRb = equippedWeaponObj.GetComponent<Rigidbody>();
            if (weaponRb != null) Destroy(weaponRb);
            
            Collider weaponCol = equippedWeaponObj.GetComponent<Collider>();
            if (weaponCol != null) weaponCol.enabled = false;
        }
        else if (config.prefab == null)
        {
            Debug.Log("ℹ️ 오른손 프리팹이 없습니다 (왼손만 사용)");
        }
        
        // 왼손 무기 장착 (있는 경우)
        if (config.leftHandPrefab != null && weaponLTransform != null)
        {
            equippedLeftWeaponObj = Instantiate(config.leftHandPrefab, weaponLTransform);
            equippedLeftWeaponObj.transform.localPosition = Vector3.zero;
            equippedLeftWeaponObj.transform.localRotation = Quaternion.identity;
            equippedLeftWeaponObj.transform.localScale = Vector3.one;
            
            Rigidbody leftWeaponRb = equippedLeftWeaponObj.GetComponent<Rigidbody>();
            if (leftWeaponRb != null) Destroy(leftWeaponRb);
            
            Collider leftWeaponCol = equippedLeftWeaponObj.GetComponent<Collider>();
            if (leftWeaponCol != null) leftWeaponCol.enabled = false;
            
            if (config.prefab != null)
                Debug.Log($"✅ {config.name} 양손 장착");
            else
                Debug.Log($"✅ {config.name} 왼손 장착");
        }
        
        activeWeaponIndex = index;
        hasWeapon = true;
        
        EnableLayers(config);
    }
    
    void EnableLayers(WeaponConfig config)
    {
        if (!string.IsNullOrEmpty(config.upperLayer))
        {
            activeUpperLayer = animator.GetLayerIndex(config.upperLayer);
            if (activeUpperLayer != -1)
                animator.SetLayerWeight(activeUpperLayer, 1f);
        }
        
        if (!string.IsNullOrEmpty(config.lowerLayer))
        {
            activeLowerLayer = animator.GetLayerIndex(config.lowerLayer);
            if (activeLowerLayer != -1)
                animator.SetLayerWeight(activeLowerLayer, 1f);
        }
    }
    
    void DisableLayers()
    {
        if (activeUpperLayer != -1)
        {
            animator.SetLayerWeight(activeUpperLayer, 0f);
            activeUpperLayer = -1;
        }
        
        if (activeLowerLayer != -1)
        {
            animator.SetLayerWeight(activeLowerLayer, 0f);
            activeLowerLayer = -1;
        }
    }
    
    void DropWeapon()
    {
        if (equippedWeaponObj != null)
            Destroy(equippedWeaponObj);
        
        if (equippedLeftWeaponObj != null)
            Destroy(equippedLeftWeaponObj);
        
        DisableLayers();
        
        hasWeapon = false;
        activeWeaponIndex = -1;
        
        Debug.Log("✅ 무기 버림");
    }
    
    Transform FindBone(Transform parent, string boneName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == boneName)
                return child;
            
            Transform result = FindBone(child, boneName);
            if (result != null)
                return result;
        }
        return null;
    }
}