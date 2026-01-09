using UnityEngine;

public class FixedTopDownCamera : MonoBehaviour
{
    [Header("Takip edilecek obje")]
    public Transform target;

    [Header("Ayarlar")]
    public float rotateSpeed = 5f;
    public float scrollSensitivity = 250f;
    public float offsetYRange = 6f;

    private Vector3 initialOffset;    // Başlangıç offset
    private float yaw;                // Mevcut yaw
    private float pitch;              // Başlangıç pitch (x rotasyon)
    private float offsetYDelta = 0f;  // Scroll ile Y değişimi
    private float distanceToTarget;   // Gerçek 3D mesafe

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Camera target atanmadı!");
            enabled = false;
            return;
        }

        // Kameranın hedefe göre olan başlangıç pozisyon farkı
        initialOffset = transform.position - target.position;

        // Sahnedeki kamera açısını al
        Vector3 angles = transform.eulerAngles;
        pitch = angles.x;
        yaw = angles.y;

        // Gerçek 3D mesafe
        distanceToTarget = initialOffset.magnitude;
    }

    void LateUpdate()
    {
        // Sağ tıkla döndürme
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            yaw += mouseX * rotateSpeed;
        }

        // Scroll ile yükseklik ayarı
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            offsetYDelta -= scroll * Time.deltaTime * scrollSensitivity;
            offsetYDelta = Mathf.Clamp(offsetYDelta, -offsetYRange, offsetYRange);
        }

        // Yeni rotasyon
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Güncellenmiş mesafe (yükseklik dahil)
        float currentDistance = distanceToTarget + offsetYDelta;

        // Kameranın yeni pozisyonu = hedefin etrafında dönen nokta
        Vector3 offset = rotation * new Vector3(0f, 0f, -currentDistance);
        Vector3 desiredPosition = target.position + offset;

        // Kamerayı konumlandır
        transform.position = desiredPosition;

        // Her zaman karaktere bak
        transform.LookAt(target.position);
    }
}
