using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class CarData
{
    public string carName;
    public int price;
    public Sprite carIcon;
}

[System.Serializable]
public class CarCardUI
{
    public GameObject cardRoot;
    public Image carImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button actionButton;
    public TextMeshProUGUI buttonText;

    [Header("Görsel Efektler")]
    public GameObject lockedOverlay;
    public GameObject selectedBorder;
    public GameObject coinIcon; // Altýn ikonu (Free yazarken gizlemek için)
}

public class ShopManager : MonoBehaviour
{
    [Header("Araç Veritabaný")]
    public CarData[] allCars;

    [Header("Arayüz (UI) Kartlarý")]
    public CarCardUI[] carCards;

    [Header("Genel Arayüz")]
    public TextMeshProUGUI totalCoinsText;
    public GameObject nextPageButton;
    public GameObject prevPageButton;

    [Header("Tasarým (Renkler)")]
    public Color buyButtonColor = new Color(1f, 0.7f, 0f);    // Sarý/Turuncu
    public Color useButtonColor = new Color(0f, 0.4f, 1f);    // Mavi
    public Color selectedButtonColor = new Color(0f, 0.8f, 0.4f); // Yeþil

    private int totalCoins;
    private int currentPage = 0;
    private const int CARS_PER_PAGE = 6;

    void Start()
    {
        PlayerPrefs.SetInt("CarUnlocked_0", 1);
        LoadShop();
    }

    public void LoadShop()
    {
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        if (totalCoinsText != null) totalCoinsText.text = totalCoins.ToString();
        UpdatePage();
    }

    public void NextPage()
    {
        if ((currentPage + 1) * CARS_PER_PAGE < allCars.Length)
        {
            currentPage++;
            UpdatePage();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
    }

    private void UpdatePage()
    {
        int selectedIndex = PlayerPrefs.GetInt("SelectedCarIndex", 0);

        if (prevPageButton != null) prevPageButton.SetActive(currentPage > 0);
        if (nextPageButton != null) nextPageButton.SetActive((currentPage + 1) * CARS_PER_PAGE < allCars.Length);

        for (int i = 0; i < carCards.Length; i++)
        {
            int actualCarIndex = (currentPage * CARS_PER_PAGE) + i;

            if (actualCarIndex < allCars.Length)
            {
                carCards[i].cardRoot.SetActive(true);
                CarData car = allCars[actualCarIndex];
                CarCardUI card = carCards[i];

                card.nameText.text = car.carName;
                card.carImage.sprite = car.carIcon;

                bool isUnlocked = PlayerPrefs.GetInt("CarUnlocked_" + actualCarIndex, 0) == 1;

                if (isUnlocked)
                {
                    // ARABA AÇIK (Kilit katmanýný gizle)
                    if (card.lockedOverlay != null) card.lockedOverlay.SetActive(false);

                    if (actualCarIndex == selectedIndex)
                    {
                        // DURUM: KULLANILIYOR
                        card.priceText.text = "FREE";
                        card.priceText.color = selectedButtonColor; // Yazýyý yeþil yap
                        if (card.coinIcon != null) card.coinIcon.SetActive(false); // Altýný gizle

                        card.buttonText.text = "SELECTED";
                        card.actionButton.interactable = false;
                        card.actionButton.GetComponent<Image>().color = selectedButtonColor; // Buton Yeþil

                        if (card.selectedBorder != null) card.selectedBorder.SetActive(true); // Yeþil çerçeve aç
                    }
                    else
                    {
                        // DURUM: AÇIK AMA KULLANILMIYOR
                        card.priceText.text = "OWNED";
                        card.priceText.color = Color.black;
                        if (card.coinIcon != null) card.coinIcon.SetActive(false);

                        card.buttonText.text = "USE";
                        card.actionButton.interactable = true;
                        card.actionButton.GetComponent<Image>().color = useButtonColor; // Buton Mavi

                        if (card.selectedBorder != null) card.selectedBorder.SetActive(false); // Yeþil çerçeve kapat
                    }
                }
                else
                {
                    // DURUM: ARABA KÝLÝTLÝ
                    card.priceText.text = car.price.ToString();
                    card.priceText.color = Color.black;
                    if (card.coinIcon != null) card.coinIcon.SetActive(true);

                    card.buttonText.text = "BUY";
                    card.actionButton.GetComponent<Image>().color = buyButtonColor; // Buton Sarý

                    if (card.lockedOverlay != null) card.lockedOverlay.SetActive(true); // Kilit katmaný aç
                    if (card.selectedBorder != null) card.selectedBorder.SetActive(false);

                    card.actionButton.interactable = (totalCoins >= car.price);
                }
            }
            else
            {
                carCards[i].cardRoot.SetActive(false);
            }
        }
    }

    public void OnCardButtonClicked(int slotIndex)
    {
        int actualCarIndex = (currentPage * CARS_PER_PAGE) + slotIndex;
        if (actualCarIndex >= allCars.Length) return;

        bool isUnlocked = PlayerPrefs.GetInt("CarUnlocked_" + actualCarIndex, 0) == 1;

        if (isUnlocked)
        {
            PlayerPrefs.SetInt("SelectedCarIndex", actualCarIndex);
            PlayerPrefs.Save();
        }
        else
        {
            CarData car = allCars[actualCarIndex];
            if (totalCoins >= car.price)
            {
                totalCoins -= car.price;
                PlayerPrefs.SetInt("TotalCoins", totalCoins);
                PlayerPrefs.SetInt("CarUnlocked_" + actualCarIndex, 1);
                PlayerPrefs.SetInt("SelectedCarIndex", actualCarIndex);
                PlayerPrefs.Save();

                if (totalCoinsText != null) totalCoinsText.text = totalCoins.ToString();
            }
        }
        UpdatePage();
    }
}