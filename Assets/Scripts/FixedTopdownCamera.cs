using UnityEngine;

public class FixedTopDownCamera : MonoBehaviour
{
    [Header("Takip edilecek obje")]
    public Transform target;

    [Header("Camera Shake")]
    public float shakeStrength = 0.15f;
    float shakeTime = 0f;
    float shakeDuration = 0f;
    float currentShakeStrength = 0f;

    [Header("Ayarlar")]
    public float rotateSpeed = 5f;
    public float scrollSensitivity = 250f;
    public float offsetYRange = 25f;

    [Header("Pitch Açısı Sınırı")]
    public float minPitch = 10f;
    public float maxPitch = 80f;
    [Header("Lock-On Pitch Control")]
    public bool allowPitchInLockOn = true;
    public float lockOnPitchSensitivity = 2.5f;
    public float lockOnPitchCenter = 35f;      // lock-on varsayılan pitch
    public float lockOnPitchRange = 20f;       // +/- kaç derece oynayabilsin
    [Header("Lock-On")]
    public bool lockOnActive = false;
    public Transform lockOnTarget;            // düşman
    public float lockOnRotateSpeed = 10f;     // lock-on'da kameranın dönme hızı
    public float lockOnPitch = 35f;           // lock-on'da sabit pitch istersen
    public bool useFixedLockOnPitch = false;  // true olursa pitch sabitlenir

    private Vector3 initialOffset;
    private float yaw;
    private float pitch;
    private float offsetYDelta = 0f;
    private float distanceToTarget;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Camera target atanmadı!");
            enabled = false;
            return;
        }

        initialOffset = transform.position - target.position;

        Vector3 angles = transform.eulerAngles;
        pitch = angles.x;
        yaw = angles.y;

        distanceToTarget = initialOffset.magnitude;
    }

    void LateUpdate()
    {
        // Scroll her zaman çalışsın (lock-on'da da zoom olsun)
        HandleScroll();

        if (!lockOnActive || lockOnTarget == null)
        {
            // NORMAL MOD: sağ tıkla kamera kontrolü
            HandleFreeLook();
        }
        else
        {
            // LOCK-ON MOD: kamera düşmana bakacak şekilde yaw/pitch ayarla
            HandleLockOnLook();
        }

        // Kamera pozisyonunu aynı sistemle hesapla
        ApplyCameraTransform();
    }

    void HandleFreeLook()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * rotateSpeed;

            pitch -= mouseY * rotateSpeed;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    void HandleLockOnLook()
    {
        // target -> düşman yönü (yaw için)
        Vector3 dir = lockOnTarget.position - target.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        // ✅ YAW: sağa-sola kilit -> düşmana döner
        float targetYaw = Quaternion.LookRotation(dir.normalized, Vector3.up).eulerAngles.y;
        yaw = Mathf.LerpAngle(yaw, targetYaw, lockOnRotateSpeed * Time.deltaTime);

        // ✅ PITCH: sadece sağ tık basılıyken yukarı-aşağı oynat
        if (allowPitchInLockOn && Input.GetMouseButton(1))
        {
            float mouseY = Input.GetAxis("Mouse Y");

            pitch -= mouseY * lockOnPitchSensitivity;

            float minLockPitch = lockOnPitchCenter - lockOnPitchRange;
            float maxLockPitch = lockOnPitchCenter + lockOnPitchRange;

            pitch = Mathf.Clamp(
                pitch,
                Mathf.Max(minPitch, minLockPitch),
                Mathf.Min(maxPitch, maxLockPitch)
            );
        }
        else
        {
            // Sağ tık yokken: istersen merkeze yumuşak dönsün
            pitch = Mathf.Lerp(pitch, lockOnPitchCenter, lockOnRotateSpeed * Time.deltaTime);
        }
    }

    public void Shake(float duration, float strength)
    {
        shakeDuration = duration;
        shakeTime = duration;
        currentShakeStrength = strength;
    }

    void HandleScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            offsetYDelta -= scroll * Time.deltaTime * scrollSensitivity;
            offsetYDelta = Mathf.Clamp(offsetYDelta, -offsetYRange, offsetYRange);
        }
    }

    void ApplyCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        float currentDistance = distanceToTarget + offsetYDelta;

        Vector3 offset = rotation * new Vector3(0f, 0f, -currentDistance);
        Vector3 desiredPosition = target.position + offset;

        // --- SHAKE (applied after camera math, so it won't be overwritten) ---
        if (shakeTime > 0f)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * currentShakeStrength;
            shakeOffset.z = 0f; // don't mess zoom too much
            desiredPosition += shakeOffset;

            shakeTime -= Time.deltaTime;
            if (shakeTime <= 0f)
                currentShakeStrength = 0f;
        }

        transform.position = desiredPosition;

        // Lock-on'da ister karaktere ister düşmana bak:
        if (lockOnActive && lockOnTarget != null)
        {
            // İstersen ortalama noktaya bak (daha sinematik)
            Vector3 mid = (target.position + lockOnTarget.position) * 0.5f;
            transform.LookAt(mid);
        }
        else
        {
            transform.LookAt(target.position);
        }
    }

    // --- DIŞARIDAN ÇAĞRILACAK API ---
    public void SetLockOnTarget(Transform enemy)
    {
        lockOnTarget = enemy;
        lockOnActive = (enemy != null);
    }

    public void ClearLockOn()
    {
        lockOnTarget = null;
        lockOnActive = false;
    }
}
