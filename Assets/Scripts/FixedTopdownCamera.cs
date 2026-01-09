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
    public float minPitch = 10f;  // En fazla aşağı bakış
    public float maxPitch = 80f;  // En fazla yukarı bakış

    private Vector3 initialOffset;    // Başlangıç offset
    private float yaw;                // Mevcut yaw
    private float pitch;              // Mevcut pitch (x rotasyon)
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
        // Sağ tıkla döndürme (yaw ve pitch)
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * rotateSpeed;

            // Yukarı-aşağı bakış (ters eksen isteyenler için -mouseY)
            pitch -= mouseY * rotateSpeed;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // Scroll ile yükseklik/zoom ayarı
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
