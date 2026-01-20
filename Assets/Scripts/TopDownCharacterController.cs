using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TopDownCharacterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float jumpForce = 6f;

    [Header("Roll")]
    public KeyCode rollKey = KeyCode.LeftShift;
    public float rollDuration = 0.55f;
    public float rollSpeed = 9f;
    public float rollCooldown = 0.6f;
    public string rollAnimName = "Roll"; // <-- Animator state name


    [Header("Refs")]
    public Camera mainCamera;
    public LayerMask groundLayer;
    public GameObject SwordActive, NoActiveSword;

    [Header("Camera Shake")]
    public CameraShake cameraShake;
    public float rollShakeDuration = 0.18f;
    public float rollShakeStrength = 0.15f;

    [Header("Camera Lock-On")]
    public FixedTopDownCamera cameraLock;

    [Header("Lock-On (TAG + Radius)")]
    public string enemyTag = "Enemy";
    public float lockOnRadius = 12f;
    public KeyCode lockOnKey = KeyCode.E;
    public bool autoUnlockIfOutOfRange = true;

    public bool isLockedOn = false;
    public Transform lockedTarget;

    [Header("Combat Timings")]
    public float slash1Duration = 0.6f;
    public float slash2Duration = 0.7f;
    public float comboWindow = 0.8f;
    public float focusKeepTime = 1.2f;

    public enum LookMode { Mouse, MoveDirection, Enemy }
    public LookMode lookMode = LookMode.MoveDirection;

    Rigidbody rb;
    Animator animator;

    Vector3 moveInput;
    string currentAnimation = "";

    bool isJumping = false;
    bool isAttacking = false;

    bool isRolling = false;
    float rollCooldownTimer = 0f;


    bool isInFocus = false;
    float focusTimer = 0f;

    // Combo
    int comboStep = 0;            // 0: Slash, 1: Slash2 hazır
    float comboTimer = 0f;        // Slash2 penceresi
    bool queuedSlash2 = false;    // Slash1 sırasında tık gelirse kuyruğa al

    // Yakındaki enemy listesi (tag ile filtre)
    readonly List<Transform> nearbyEnemies = new();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        if (mainCamera == null) mainCamera = Camera.main;

        SetSwordVisual(false);

        if (cameraShake == null && mainCamera != null)
            cameraShake = mainCamera.GetComponent<CameraShake>();

    }

    void Update()
    {
        ReadMoveInput();

        // E ile Lock-on toggle
        if (Input.GetKeyDown(lockOnKey))
        {
            if (isLockedOn) Unlock();
            else LockOnNearestEnemyByTag();
        }

        // Lock-on açıkken karakteri hedefe çevir (sadece Y rotasyon)
        if (isLockedOn && lockedTarget != null && !isRolling)
            RotateYawTowardsTarget(lockedTarget);


        // Hedef uzaklaştıysa bırak
        if (isLockedOn && autoUnlockIfOutOfRange)
        {
            if (lockedTarget == null ||
                (lockedTarget.position - transform.position).sqrMagnitude > lockOnRadius * lockOnRadius)
            {
                Unlock();
            }
        }

        // Combo penceresi
        if (comboStep == 1 && !isAttacking)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                comboStep = 0;
                queuedSlash2 = false;
            }
        }

        // Focus timer: lock-on yokken süre dolunca focus kapanabilir
        if (isInFocus && !isAttacking && !isLockedOn)
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
        if (!isAttacking && !isJumping && !isRolling)
            UpdateLocomotionAnimation();

        // Rotate: lock-on kapalıyken serbest dönüş
        if (!isLockedOn)
        {
            if (lookMode == LookMode.Mouse) RotateTowardsMouse();
            else if (lookMode == LookMode.MoveDirection) RotateTowardsMoveDirection();
        }

        // Roll
        // Roll cooldown timer
        if (rollCooldownTimer > 0f)
            rollCooldownTimer -= Time.deltaTime;

        // Roll input
        if (Input.GetKeyDown(rollKey))
            TryRoll();
    }

    void FixedUpdate()
    {
        // Saldırı VEYA Roll anında hareket yok
        if (isAttacking || isRolling) return;

        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void ReadMoveInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        moveInput = (camForward * moveZ + camRight * moveX).normalized;
    }

    // -------------------- LOCK ON (TAG) --------------------

    void LockOnNearestEnemyByTag()
    {
        CollectNearbyEnemiesByTag();

        if (nearbyEnemies.Count == 0)
            return;

        Transform nearest = GetNearest(nearbyEnemies);
        if (nearest == null)
            return;

        LockOn(nearest);
    }

    void CollectNearbyEnemiesByTag()
    {
        nearbyEnemies.Clear();

        // Yarıçap içinde tüm collider’ları al
        Collider[] hits = Physics.OverlapSphere(transform.position, lockOnRadius);

        if (hits == null || hits.Length == 0) return;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            // Tag filtre
            if (!hits[i].CompareTag(enemyTag)) continue;

            // Aynı enemy’de birden fazla collider varsa duplicate olabilir
            Transform t = hits[i].transform;
            if (!nearbyEnemies.Contains(t))
                nearbyEnemies.Add(t);
        }
    }

    Transform GetNearest(List<Transform> list)
    {
        Transform best = null;
        float bestDist = float.MaxValue;
        Vector3 myPos = transform.position;

        for (int i = 0; i < list.Count; i++)
        {
            Transform t = list[i];
            if (t == null) continue;

            float d = (t.position - myPos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }
        return best;
    }

    public void LockOn(Transform target)
    {
        if (target == null) return;

        lockedTarget = target;
        isLockedOn = true;

        // Lock-on olunca focus kesin açık kalsın
        EnterFocusForAWhile();

        // Kameraya hedefi yolla
        if (cameraLock != null)
            cameraLock.SetLockOnTarget(lockedTarget);
    }

    public void Unlock()
    {
        isLockedOn = false;
        lockedTarget = null;

        if (cameraLock != null)
            cameraLock.ClearLockOn();

        // Lock-on bittiğinde focus hemen kapanmasın; süreye bırak
        focusTimer = focusKeepTime;
    }

    void RotateYawTowardsTarget(Transform target)
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f; // sadece yaw
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    // -------------------- COMBAT / COMBO --------------------

    void TryAttack()
    {
        // Attack sırasında 2. vuruşu kuyrukla
        if (isAttacking)
        {
            if (comboStep == 0)
                queuedSlash2 = true;
            return;
        }

        // Her vuruşta focus aç/süre tazele
        EnterFocusForAWhile();

        if (comboStep == 1 && comboTimer > 0f)
        {
            StopCoroutine(nameof(AttackRoutine));
            StartCoroutine(AttackRoutine("Slash2", slash2Duration, endsCombo: true));
        }
        else
        {
            StopCoroutine(nameof(AttackRoutine));
            StartCoroutine(AttackRoutine("Slash", slash1Duration, endsCombo: false));
        }
    }

    IEnumerator AttackRoutine(string animName, float duration, bool endsCombo)
    {
        // Attack lock
        isAttacking = true;

        // Souls hissi: input hareketi yok + kayma yok
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        rb.angularVelocity = Vector3.zero;

        Play(animName);

        yield return new WaitForSeconds(duration);

        // Attack bitti
        isAttacking = false;

        if (!endsCombo)
        {
            // Slash1 bitti -> Slash2 penceresi
            comboStep = 1;
            comboTimer = comboWindow;

            // Slash1 sırasında tıklandıysa Slash2'ye zincirle (aynı coroutine içinde)
            if (queuedSlash2)
            {
                queuedSlash2 = false;
                EnterFocusForAWhile();

                // Slash2'yi direkt burada çalıştır
                yield return StartCoroutine(AttackRoutine("Slash2", slash2Duration, endsCombo: true));
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

    // -------------------- JUMP --------------------

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


    void TryRoll()
    {
        if (isRolling) return;
        if (isAttacking || isJumping) return;
        if (rollCooldownTimer > 0f) return;

        StartCoroutine(RollRoutine());
    }

    // -------------------- ROLL --------------------
    IEnumerator RollRoutine()
    {
        isRolling = true;
        rollCooldownTimer = rollCooldown;

        Vector3 rollDir = transform.forward;
        rollDir.y = 0f;
        rollDir.Normalize();

        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        rb.angularVelocity = Vector3.zero;

        Play("Roll");
        if (cameraShake != null)
            cameraShake.Shake(rollShakeDuration, rollShakeStrength);


        float t = 0f;
        while (t < rollDuration)
        {
            rb.MovePosition(rb.position + rollDir * rollSpeed * Time.fixedDeltaTime);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isRolling = false;

        if (!isAttacking && !isJumping)
            UpdateLocomotionAnimation();
    }



    // -------------------- LOCOMOTION --------------------

    void UpdateLocomotionAnimation()
    {
        // Focus kapalıysa: düz animler
        if (!isInFocus)
        {
            Play(moveInput == Vector3.zero ? "Idle" : "Run");
            return;
        }

        // Focus açık + LockOn kapalıysa: SADECE RunFocus/IdleFocus
        if (!isLockedOn)
        {
            Play(moveInput == Vector3.zero ? "IdleFocus" : "RunFocus");
            return;
        }

        // Focus açık + LockOn açıksa: yönlü focus koşular
        if (moveInput == Vector3.zero)
        {
            Play("IdleFocus");
            return;
        }

        Vector3 localMove = transform.InverseTransformDirection(moveInput);

        if (localMove.z < -0.2f) Play("BackwardRunFocus");
        else if (localMove.x > 0.5f) Play("RightRunFocus");
        else if (localMove.x < -0.5f) Play("LeftRunFocus");
        else Play("RunFocus");
    }

    void EnterFocusForAWhile()
    {
        isInFocus = true;
        focusTimer = focusKeepTime;
        SetSwordVisual(true);
    }

    void ExitFocus()
    {
        // lock-on varsa focus kapanmasın (senin istediğin “full focus”)
        if (isLockedOn) return;

        isInFocus = false;
        focusTimer = 0f;
        SetSwordVisual(false);

        if (!isAttacking && !isJumping)
            UpdateLocomotionAnimation();
    }

    // -------------------- VISUAL / ANIM --------------------

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

    // -------------------- FREE ROTATION --------------------

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

    // -------------------- DEBUG --------------------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockOnRadius);
    }
}
