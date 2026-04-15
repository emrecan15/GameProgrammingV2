using UnityEngine;

public class CoinBehavior : MonoBehaviour
{
    private Transform playerTransform;
    private CarController carController;
    private bool isFlying = false;

    void OnEnable()
    {
        // Havuzdan (Pool) her yeni altın çıktığında uçuş durumunu sıfırla
        isFlying = false;

        // Oyuncuyu bul
        if (playerTransform == null)
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

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // 1. MIKNATIS KONTROLÜ (Sadece mesafeye bakıyoruz, Z kısıtlamasını kaldırdık)
        if (carController.isMagnetActive && !isFlying)
        {
            // Eğer altın, mıknatısın çekim alanına girdiyse uçuşu başlat
            if (distance <= carController.magnetRadius)
            {
                isFlying = true;
            }
        }

        // 2. UÇUŞ VE TOPLANMA MANTIĞI
        if (isFlying)
        {
            // Altının uçma hızı, arabanın o anki hızından HER ZAMAN daha hızlı olmalı ki onu yakalayabilsin!
            float currentPullSpeed = Mathf.Max(carController.magnetPullSpeed, carController.forwardSpeed * 2.5f);
            
            // Altını arabaya doğru uçur
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, currentPullSpeed * Time.deltaTime);

            // 3. KESİN TOPLAMA (Tunnelling Önlemi)
            // Eğer altın arabaya 2.5 birimden fazla yaklaştıysa, fizik motorunun çarpışmayı 
            // algılamasını beklemeden (garanti olsun diye) altını toplanmış say!
            if (distance < 2.5f)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddCoin(); // Skoru/Altını artır
                    
                    // YENİ: 2x Altın (Double Coin) gücü aktifse bir altın daha ver!
                    if (carController.isDoubleCoinActive)
                    {
                        GameManager.Instance.AddCoin(); 
                    }
                }
                
                gameObject.SetActive(false); // Altını sahneden gizle (Havuza yolla)
            }
        }
    }
}