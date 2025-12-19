using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController Controller;

    private Animator animator;
    private ThirdPersonCamera thirdPersonCamera; // 3인칭 카메라 참조

    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 10.0f; // 캐릭터 회전 속도

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void SetCamera(ThirdPersonCamera camera)
    {
        thirdPersonCamera = camera;
    }

    public void Move()
    {
        if (Controller != null)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            P_PlayerMovement playerMovement = default;
            playerMovement.player_id = LocalPlayerInfo.ID;
            playerMovement.rotation = Controller.transform.rotation;
            playerMovement.dx = horizontal;
            playerMovement.dy = vertical;

            if (horizontal != 0 || vertical != 0)
            {
                Client.TCP.SendPacket(E_PACKET.PLAYER_MOVEMENT, playerMovement);

                // 애니메이션
                if (animator != null)
                {
                    animator.SetFloat("Speed", 1f);
                }

                // 3인칭: 카메라 방향 기준으로 이동
                MoveCharacter(horizontal, vertical);
            }
            else
            {
                if (animator != null)
                {
                    animator.SetFloat("Speed", 0f);
                }
            }
        }
    }

    // 새로운 메서드: 카메라 방향 기준 이동
    private void MoveCharacter(float horizontal, float vertical)
    {
        if (thirdPersonCamera == null)
        {
            Debug.LogWarning("ThirdPersonCamera가 설정되지 않았습니다!");
            return;
        }

        // 카메라의 forward와 right 방향 가져오기
        Vector3 cameraForward = thirdPersonCamera.GetCameraForward();
        Vector3 cameraRight = thirdPersonCamera.GetCameraRight();

        // 이동 방향 계산
        Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            // 캐릭터를 이동 방향으로 회전
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            // 실제 이동 (로컬에서는 직접 이동, 서버에서 검증)
            Vector3 motion = moveDirection * moveSpeed * Time.deltaTime;
            Controller.Move(motion);
        }
    }

    // 서버에서 받은 이동 처리 (다른 플레이어용)
    public void Move(Vector3 motion)
    {
        Controller.Move(motion);

        // 이동 중 애니메이션
        if (animator != null && motion.magnitude > 0.01f)
        {
            animator.SetFloat("Speed", 1f);

            // 이동 방향으로 캐릭터 회전
            if (motion.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(motion);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
        else if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
    }
}