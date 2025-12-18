using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;                    // 플레이어 Transform
    public float distance = 5.0f;               // 플레이어와 카메라 사이 거리
    public float height = 2.0f;                 // 카메라 높이 오프셋
    public float smoothSpeed = 10.0f;           // 카메라 이동 부드러움 정도

    [Header("Mouse Settings")]
    public float mouseSensitivity = 500f;       // 마우스 감도
    public float minVerticalAngle = -40f;       // 최소 수직 각도
    public float maxVerticalAngle = 80f;        // 최대 수직 각도

    [Header("Camera Collision")]
    public bool checkCollision = true;          // 카메라 충돌 체크
    public LayerMask collisionLayers = -1;      // 충돌 레이어
    public float collisionPadding = 0.3f;       // 충돌 시 여백

    private float currentX = 0f;                // 현재 수평 회전 각도
    private float currentY = 20f;               // 현재 수직 회전 각도
    private float currentDistance;              // 현재 거리 (충돌 시 변경됨)

    void Start()
    {
        // 마우스 커서 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentDistance = distance;

        if (target == null)
        {
            Debug.LogError("ThirdPersonCamera: target이 설정되지 않았습니다!");
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ESC로 마우스 잠금/해제
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked)
                ? CursorLockMode.None
                : CursorLockMode.Locked;
            Cursor.visible = (Cursor.lockState == CursorLockMode.None);
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // 마우스 입력
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            // 회전 각도 계산
            currentX += mouseX;
            currentY -= mouseY;
            currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
        }

        // 카메라 위치 계산
        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        // 목표 위치 (플레이어 위치 + 높이 오프셋)
        Vector3 targetPosition = target.position + Vector3.up * height;

        // 회전 계산
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // 카메라가 있어야 할 이상적인 위치
        Vector3 desiredPosition = targetPosition - (rotation * Vector3.forward * distance);

        // 충돌 체크
        if (checkCollision)
        {
            Vector3 direction = desiredPosition - targetPosition;
            float targetDistance = direction.magnitude;

            // Raycast로 카메라와 플레이어 사이에 장애물이 있는지 확인
            if (Physics.Raycast(targetPosition, direction.normalized, out RaycastHit hit, targetDistance, collisionLayers))
            {
                // 장애물이 있으면 카메라를 당김
                currentDistance = Mathf.Lerp(currentDistance, hit.distance - collisionPadding, Time.deltaTime * smoothSpeed);
            }
            else
            {
                // 장애물이 없으면 원래 거리로 복구
                currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime * smoothSpeed);
            }

            // 충돌을 고려한 위치 재계산
            desiredPosition = targetPosition - (rotation * Vector3.forward * currentDistance);
        }

        // 카메라 위치 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);

        // 카메라가 항상 플레이어를 바라보도록
        transform.LookAt(targetPosition);
    }

    // 외부에서 현재 카메라 방향을 가져올 수 있도록
    public Vector3 GetCameraForward()
    {
        // Y축 회전만 사용 (캐릭터 이동용)
        return Quaternion.Euler(0, currentX, 0) * Vector3.forward;
    }

    public Vector3 GetCameraRight()
    {
        return Quaternion.Euler(0, currentX, 0) * Vector3.right;
    }
}