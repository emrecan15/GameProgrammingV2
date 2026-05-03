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

    // ARTIK PUBLIC: Böylece araba kaza yaptýðýnda dýþarýdan çaðrýlýp her þeyi kapatabilecek
    public void HideAll()
    {
        if (magnetPanel) magnetPanel.SetActive(false);
        if (nitroPanel) nitroPanel.SetActive(false);
        if (doubleCoinPanel) doubleCoinPanel.SetActive(false);
        if (shieldPanel) shieldPanel.SetActive(false);
    }
}