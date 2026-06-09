using UnityEngine;
using UnityEngine.AI;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 20f;
    public float turnSpeed = 5f;
    public Animator animator;

    Rigidbody rb;
    bool isGrounded;
    bool jumpRequested;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        // 💡 恢復你原本最穩定的跳躍按鍵偵測
        if (isGrounded && (Input.GetKeyDown(KeyCode.Space) || ArduinoSerialPOC.GetButtonDown("JUMP")))
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(h, 0f, v);
        Vector3 moveDirection = inputDirection.normalized;

        if (Camera.main != null)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            moveDirection = (cameraForward * inputDirection.z + cameraRight * inputDirection.x).normalized;
        }

        Vector3 move = moveDirection * moveSpeed;

        // ─── 繼續使用 NavMesh 限制水平範圍，防止穿牆 ───
        Vector3 targetPosition = rb.position + new Vector3(move.x, 0f, move.z) * Time.fixedDeltaTime;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            targetPosition.x = hit.position.x;
            targetPosition.z = hit.position.z;
        }

        Vector3 requiredHorizontalVelocity = (targetPosition - rb.position) / Time.fixedDeltaTime;

        // 獲取當前的 Y 軸速度（保留真實的物理重力）
        float currentVelocityY = rb.linearVelocity.y;

        // ─── 💡 關鍵修正：完美還原 AddForce 的物理手感 ───
        if (jumpRequested)
        {
            // 物理公式：速度變化量 = 力道 / 質量。這樣寫就不管質量設多少，跳躍高度都會正確！
            currentVelocityY += (jumpForce / rb.mass);

            animator.SetTrigger("Jump");
            Debug.Log("角色成功跳躍！");
            jumpRequested = false;
        }

        // 一次性套用水平與垂直速度
        rb.linearVelocity = new Vector3(requiredHorizontalVelocity.x, currentVelocityY, requiredHorizontalVelocity.z);
        // ──────────────────────────────────────────

        if (moveDirection.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        }

        if (moveDirection.magnitude > 0)
        {
            animator.SetBool("IsWalking", true);
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }
    }

    // 💡 把你原本最穩定的地板偵測加回來，避免高度判定失誤！
    void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}