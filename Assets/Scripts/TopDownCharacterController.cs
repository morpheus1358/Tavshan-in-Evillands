using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class TopDownCharacterController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f,Jumpforce;
    private string currentAnimation = "";
    private bool isJumping = false;
    private bool isFocusing = false; // örnek flag
    [Header("Mouse ile Dönüş")]
    public Camera mainCamera;
    public LayerMask groundLayer;
    public enum LookMode
    {
        Mouse,
        MoveDirection,
        Enemy
    }

    public LookMode lookMode = LookMode.MoveDirection;
    private Rigidbody rb;
    private Vector3 moveInput;
    private Animator animator;
  
    public enum CharacterAnimationState
    {
        Idle,
        Run,
        Jump,
        Attack
    }
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        // WASD input
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // Kameraya göre hareket yönü
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 desiredDirection = (camForward * moveZ + camRight * moveX).normalized;
        moveInput = desiredDirection;

        // Eğer focus modundaysa hareket animasyonu oynatma
        if (!isFocusing)
        {
            if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
            {
                StartCoroutine(HandleJumpAnimation());
            }
            else if (!isJumping)
            {
                if (moveInput != Vector3.zero)
                    PlayAnimationState(CharacterAnimationState.Run);
                else
                    PlayAnimationState(CharacterAnimationState.Idle);
            }
        }

        // Dönüş kontrolü
        if (lookMode == LookMode.Mouse)
            RotateTowardsMouse();
        else if (lookMode == LookMode.MoveDirection)
            RotateTowardsMoveDirection();
    }
    private IEnumerator HandleJumpAnimation()
    {
        isJumping = true;

        // Animasyonu oynat
        PlayAnimationState(CharacterAnimationState.Jump);

        // Yukarı doğru fiziksel zıplama uygula
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // eski y hızını sıfırla
        rb.AddForce(Vector3.up * Jumpforce, ForceMode.Impulse); // yukarı zıplat

        // Animasyon süresi kadar bekle
        yield return new WaitForSeconds(0.5f); // Jump animasyon süresine göre ayarla

        // Harekete göre Idle veya Run animasyonuna geç
        if (moveInput != Vector3.zero)
            PlayAnimationState(CharacterAnimationState.Run);
        else
            PlayAnimationState(CharacterAnimationState.Idle);

        isJumping = false;
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void RotateTowardsMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 500f, groundLayer))
        {
            Vector3 lookDirection = hit.point - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude < 0.01f) return;

            lookDirection.Normalize();
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            float angleDiff = Vector3.SignedAngle(forward, lookDirection, Vector3.up);
            float angleThreshold = 5f;
            if (Mathf.Abs(angleDiff) < angleThreshold) return;

            float maxRotation = rotationSpeed * Time.deltaTime;
            float clampedAngle = Mathf.Clamp(angleDiff, -maxRotation, maxRotation);
            transform.Rotate(0f, clampedAngle, 0f);
        }
    }

    void RotateTowardsMoveDirection()
    {
        if (moveInput == Vector3.zero)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(moveInput, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
    private void PlayAnimationState(CharacterAnimationState state)
    {
        string animName = state.ToString();

        if (currentAnimation == animName)
            return;

        animator.CrossFade(animName, 0.1f);
        currentAnimation = animName;
    }
}
