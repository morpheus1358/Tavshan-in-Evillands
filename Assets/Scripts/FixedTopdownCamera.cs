using UnityEngine;

public class FixedTopDownCamera : MonoBehaviour
{
    [Header("Takip edilecek obje")]
    public Transform target;

    [Header("Ayarlar")]
    public float rotateSpeed = 5f;
    public float scrollSensitivity = 250f;
    public float offsetYRange = 25f;

    [Header("Pitch Açısı Sınırı")]
    public float minPitch = 10f;
    public float maxPitch = 80f;

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
        // target -> düşman yönü
        Vector3 dir = lockOnTarget.position - target.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        // hedef yaw
        float targetYaw = Quaternion.LookRotation(dir.normalized, Vector3.up).eulerAngles.y;

        // yaw'ı yumuşak döndür
        yaw = Mathf.LerpAngle(yaw, targetYaw, lockOnRotateSpeed * Time.deltaTime);

        // pitch sabit istersen
        if (useFixedLockOnPitch)
        {
            pitch = Mathf.Lerp(pitch, lockOnPitch, lockOnRotateSpeed * Time.deltaTime);
        }
        else
        {
            // sabit değilse yine clamp içinde bırak
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
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
