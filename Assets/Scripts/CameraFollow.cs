using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Takip Edilecek Obje")]
    public Transform target;

    private CarController carController;

    [Header("Mesafe Ayarlarý")]
    // X:0, Y:5, Z:-10 -> Daha yüksek ve daha geriden profesyonel bir bakýþ açýsý saðlar.
    public Vector3 offset = new Vector3(0f, 5f, -10f);

    [Header("Yumuþatma Ayarlarý")]
    public float positionSmoothTime = 0.12f; // Virajlarda yolu hissetmek için biraz artýrdýk
    public float rotationSmoothSpeed = 6f;

    private Vector3 currentVelocity = Vector3.zero;
    private Quaternion smoothedRotation;

    void Start()
    {
        if (target != null)
        {
            carController = target.GetComponent<CarController>();
            smoothedRotation = target.rotation;
        }
    }

    void LateUpdate()
    {
        if (target == null || carController == null) return;

        // Araba scriptinden hazýr hesaplanmýþ yol yönünü alýyoruz
        Vector3 trackForward = carController.currentTrackForward;

        // Yolun virajýna göre kameranýn durmasý gereken rotasyonu belirle
        Quaternion trackRotation = Quaternion.LookRotation(trackForward, Vector3.up);
        smoothedRotation = Quaternion.Slerp(smoothedRotation, trackRotation, rotationSmoothSpeed * Time.deltaTime);

        // --- KONUM HESAPLAMA ---
        Vector3 desiredPosition = target.position + (smoothedRotation * offset);

        // SmoothDamp kullanýmý sarsýntýlarý önler ve yüksek hýzlarda kamerayý dengeler
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);

        // --- BAKIÞ MANTIÐI ---
        // Arabanýn þeritteki ufak titremelerini görmemek için 6 birim önüne odaklanýyoruz
        Vector3 lookAtPoint = target.position + trackForward * 6f;
        Quaternion finalRotation = Quaternion.LookRotation(lookAtPoint - transform.position);

        transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}