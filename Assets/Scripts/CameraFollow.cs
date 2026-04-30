using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Mesafe Ayarlarý")]
    public Vector3 offset = new Vector3(0f, 5f, -10f);

    [Header("Yumuþatma Ayarlarý")]
    public float positionSmoothTime = 0.12f;
    public float rotationSmoothSpeed = 6f;

    private CarController carController;
    private Vector3 currentVelocity;
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

        Vector3 trackForward = carController.currentTrackForward;

        // Frame rate'den baðýmsýz yumuþatma için deltaTime'ý 1'den çýkar
        // Bu klasik Lerp frame rate baðýmlýlýðýný ortadan kaldýrýr
        float t = 1f - Mathf.Pow(1f - (rotationSmoothSpeed * 0.1f), Time.deltaTime * 60f);

        Quaternion targetRotation = Quaternion.LookRotation(trackForward, Vector3.up);
        smoothedRotation = Quaternion.Slerp(smoothedRotation, targetRotation, t);

        // Kamera pozisyonu: SmoothDamp yeterli, Slerp ile çakýþmasýn
        Vector3 desiredPosition = target.position + smoothedRotation * offset;
        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);

        // Bakýþ noktasý: arabanýn önüne odaklan, þerit titremelerini gizler
        Vector3 lookAt = target.position + trackForward * 6f;
        transform.rotation = Quaternion.LookRotation(lookAt - transform.position);
    }
}