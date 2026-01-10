using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TopDownCharacterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float jumpForce = 6f;

    [Header("Refs")]
    public Camera mainCamera;
    public LayerMask groundLayer;
    public GameObject SwordActive, NoActiveSword;

    [Header("Combat Timings")]
    public float slash1Duration = 0.6f;   // Slash süresi
    public float slash2Duration = 0.7f;   // Slash2 süresi
    public float comboWindow = 0.8f;      // Slash1 sonrası Slash2 için süre
    public float focusKeepTime = 1.2f;    // Son vuruştan sonra focus'ta kalma

    public enum LookMode { Mouse, MoveDirection, Enemy }
    public LookMode lookMode = LookMode.MoveDirection;

    Rigidbody rb;
    Animator animator;

    Vector3 moveInput;
    string currentAnimation = "";

    bool isJumping = false;
    bool isAttacking = false;

    bool isInFocus = false;
    float focusTimer = 0f;

    // Combo
    int comboStep = 0;            // 0: Slash, 1: Slash2 hazır
    float comboTimer = 0f;        // Slash2 penceresi
    bool queuedSlash2 = false;    // Slash1 sırasında tık gelirse kuyruğa al

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        if (mainCamera == null) mainCamera = Camera.main;

        SetSwordVisual(false);
    }

    void Update()
    {
        ReadMoveInput();

        // Combo penceresi sayacı (Slash1 bittikten sonra çalışır)
        if (comboStep == 1 && !isAttacking)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                comboStep = 0;
                queuedSlash2 = false;
            }
        }

        // Focus timer (attack yokken akar)
        if (isInFocus && !isAttacking)
        {
            focusTimer -= Time.deltaTime;
            if (focusTimer <= 0f)
                ExitFocus();
        }

        // Attack input
        if (Input.GetMouseButtonDown(0))
            TryAttack();

        // Jump (attack yokken)
        if (!isAttacking && Input.GetKeyDown(KeyCode.Space) && !isJumping)
            StartCoroutine(JumpRoutine());

        // Locomotion (attack/jump yokken)
        if (!isAttacking && !isJumping)
            UpdateLocomotionAnimation();

        // Rotate (istersen attack sırasında da dönebilsin)
        if (lookMode == LookMode.Mouse) RotateTowardsMouse();
        else if (lookMode == LookMode.MoveDirection) RotateTowardsMoveDirection();
    }

    void FixedUpdate()
    {
        // Vuruş anında HAREKET YOK
        if (isAttacking) return;

        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void ReadMoveInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;

        camForward.y = 0f; camRight.y = 0f;
        camForward.Normalize(); camRight.Normalize();

        moveInput = (camForward * moveZ + camRight * moveX).normalized;
    }

    void TryAttack()
    {
        // Attack sırasında 2. vuruşu kuyrukla
        if (isAttacking)
        {
            // Slash1 oynarken gelen tık -> Slash2 kuyruğa
            if (comboStep == 0)
                queuedSlash2 = true;

            return;
        }

        // Her vuruşta focus aç/süre tazele
        EnterFocusForAWhile();

        // Eğer Slash2 penceresindeysek direkt Slash2
        if (comboStep == 1 && comboTimer > 0f)
        {
            StopCoroutine(nameof(AttackRoutine));
            StartCoroutine(AttackRoutine("Slash2", slash2Duration, endsCombo: true));
        }
        else
        {
            // Slash1
            StopCoroutine(nameof(AttackRoutine));
            StartCoroutine(AttackRoutine("Slash", slash1Duration, endsCombo: false));
        }
    }

    IEnumerator AttackRoutine(string animName, float duration, bool endsCombo)
    {
        isAttacking = true;

        Play(animName);

        yield return new WaitForSeconds(duration);

        isAttacking = false;

        if (!endsCombo)
        {
            // Slash1 bitti -> Slash2 penceresi aç
            comboStep = 1;
            comboTimer = comboWindow;

            // Slash1 sırasında tıklanmışsa Slash2'yi hemen bas
            if (queuedSlash2)
            {
                queuedSlash2 = false;
                EnterFocusForAWhile();
                StopCoroutine(nameof(AttackRoutine));
                StartCoroutine(AttackRoutine("Slash2", slash2Duration, endsCombo: true));
                yield break;
            }
        }
        else
        {
            // Slash2 bitti -> combo sıfır
            comboStep = 0;
            comboTimer = 0f;
            queuedSlash2 = false;
        }

        if (!isJumping)
            UpdateLocomotionAnimation();
    }

    IEnumerator JumpRoutine()
    {
        isJumping = true;

        Play(isInFocus ? "jumpFocus" : "Jump");

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        yield return new WaitForSeconds(1.0f);

        isJumping = false;

        if (!isAttacking)
            UpdateLocomotionAnimation();
    }

    void UpdateLocomotionAnimation()
    {
        if (moveInput == Vector3.zero)
        {
            Play(isInFocus ? "IdleFocus" : "Idle");
            return;
        }

        if (!isInFocus)
        {
            Play("Run");
            return;
        }

        Vector3 localMove = transform.InverseTransformDirection(moveInput);

        if (localMove.z < -0.2f) Play("BackwardRunFocus");
        else if (localMove.x > 0.35f) Play("RightRunFocus");
        else if (localMove.x < -0.35f) Play("LeftRunFocus");
        else Play("RunFocus");
    }

    void EnterFocusForAWhile()
    {
        isInFocus = true;
        focusTimer = focusKeepTime; // her vuruşta yenilenir
        SetSwordVisual(true);
    }

    void ExitFocus()
    {
        isInFocus = false;
        focusTimer = 0f;
        SetSwordVisual(false);

        if (!isAttacking && !isJumping)
            UpdateLocomotionAnimation();
    }

    void SetSwordVisual(bool swordOn)
    {
        if (SwordActive) SwordActive.SetActive(swordOn);
        if (NoActiveSword) NoActiveSword.SetActive(!swordOn);
    }

    void Play(string animName)
    {
        if (currentAnimation == animName) return;
        animator.CrossFade(animName, 0.1f);
        currentAnimation = animName;
    }

    void RotateTowardsMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            Vector3 dir = hit.point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    void RotateTowardsMoveDirection()
    {
        if (moveInput == Vector3.zero) return;
        Quaternion targetRot = Quaternion.LookRotation(moveInput, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }
}
