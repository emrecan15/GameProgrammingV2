using UnityEngine;

// Bu script SADECE mıknatıs aktifken coin'i arabaya doğru çeker.
// Toplama işi CarController.OnTriggerEnter tarafından yapılır.
public class CoinBehavior : MonoBehaviour
{
    private static Transform playerTransform;
    private static CarController carController;

    private bool isFlying;

    void OnEnable()
    {
        isFlying = false;

        if (playerTransform == null || carController == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                carController = playerObj.GetComponent<CarController>();
            }
        }
    }

    void Update()
    {
        if (carController == null || playerTransform == null) return;
        if (!carController.isMagnetActive)
        {
            isFlying = false;
            return;
        }

        // sqrMagnitude kullan: distance hesabından çok daha ucuz (karekök yok)
        float sqrDist = (transform.position - playerTransform.position).sqrMagnitude;
        float sqrMagRadius = carController.magnetRadius * carController.magnetRadius;

        if (!isFlying && sqrDist <= sqrMagRadius)
            isFlying = true;

        if (isFlying)
        {
            float pullSpeed = Mathf.Max(carController.magnetPullSpeed, carController.forwardSpeed * 2.5f);
            transform.position = Vector3.MoveTowards(
                transform.position, playerTransform.position, pullSpeed * Time.deltaTime);
        }
    }
}