using UnityEngine;
using TMPro;

public class PowerUpUIManager : MonoBehaviour
{
    public static PowerUpUIManager Instance;

    public GameObject magnetPanel;
    public TextMeshProUGUI magnetText;

    public GameObject nitroPanel;
    public TextMeshProUGUI nitroText;

    public GameObject doubleCoinPanel;
    public TextMeshProUGUI doubleCoinText;

    public GameObject shieldPanel;
    public TextMeshProUGUI shieldText;

    [Header("Araç Göstergeleri (Dashboard)")]
    public GameObject distancePanel;
    public GameObject speedPanel;

    public TextMeshProUGUI distanceText; // KM için
    public TextMeshProUGUI speedText;    // KM/H için

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HideAll();
    }

    public void ShowPowerUp(string type)
    {
        switch (type)
        {
            case "Magnet": if (magnetPanel) magnetPanel.SetActive(true); break;
            case "Nitro": if (nitroPanel) nitroPanel.SetActive(true); break;
            case "DoubleCoin": if (doubleCoinPanel) doubleCoinPanel.SetActive(true); break;
            case "Shield": if (shieldPanel) shieldPanel.SetActive(true); break;
        }
    }

    public void HidePowerUp(string type)
    {
        switch (type)
        {
            case "Magnet": if (magnetPanel) magnetPanel.SetActive(false); break;
            case "Nitro": if (nitroPanel) nitroPanel.SetActive(false); break;
            case "DoubleCoin": if (doubleCoinPanel) doubleCoinPanel.SetActive(false); break;
            case "Shield": if (shieldPanel) shieldPanel.SetActive(false); break;
        }
    }

    public void UpdateTimer(string type, int secondsLeft)
    {
        switch (type)
        {
            case "Magnet": if (magnetText) magnetText.text = secondsLeft.ToString() + "s"; break;
            case "Nitro": if (nitroText) nitroText.text = secondsLeft.ToString() + "s"; break;
            case "DoubleCoin": if (doubleCoinText) doubleCoinText.text = secondsLeft.ToString() + "s"; break;
            case "Shield": if (shieldText) shieldText.text = secondsLeft.ToString() + "s"; break;
        }
    }

    // YENÝ: KM ve KM/H hesaplayýp UI'a yazdýran fonksiyon
    public void UpdateDashboard(float totalDistanceMeters, float currentSpeed)
    {
        // Kaza sonrasý yeniden baþlarken veya oyun ilk açýldýðýnda paneller kapalýysa otomatik aç
        if (distancePanel != null && !distancePanel.activeSelf) distancePanel.SetActive(true);
        if (speedPanel != null && !speedPanel.activeSelf) speedPanel.SetActive(true);

        if (distanceText != null)
        {
            // Metreyi KM'ye çevirip virgülden sonra 1 basamak gösteriyoruz (Örn: 1.2 KM)
            float km = totalDistanceMeters / 1000f;
            distanceText.text = km.ToString("F1");
        }

        if (speedText != null)
        {
            // Unity'nin hýzýný (m/s) gerçekçi KM/H birimine çeviriyoruz (x 3.6)
            int kmh = Mathf.RoundToInt(currentSpeed * 2.7f);
            speedText.text = kmh.ToString();
        }
    }

    // ARTIK PUBLIC: Böylece araba kaza yaptýðýnda dýþarýdan çaðrýlýp her þeyi kapatabilecek
    public void HideAll()
    {
        if (magnetPanel) magnetPanel.SetActive(false);
        if (nitroPanel) nitroPanel.SetActive(false);
        if (doubleCoinPanel) doubleCoinPanel.SetActive(false);
        if (shieldPanel) shieldPanel.SetActive(false);

        // Kaza anýnda Dashboard panelleri de kapanýyor
        if (distancePanel) distancePanel.SetActive(false);
        if (speedPanel) speedPanel.SetActive(false);
    }
}