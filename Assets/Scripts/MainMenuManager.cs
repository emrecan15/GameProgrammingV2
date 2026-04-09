using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Yazýlarý (UI) kontrol etmek için ekledik

public class MainMenuManager : MonoBehaviour
{
    [Header("Settings")]
    public string gameSceneName = "SampleScene";

    [Header("Paneller")]
    public GameObject shopScreen; // Hiyerarþideki ShopScreen objesini buraya baðlayacaðýz

    [Header("Ýstatistik Yazýlarý (Kutular)")]
    public TextMeshProUGUI bestScoreText;
    public TextMeshProUGUI totalCoinsText;
    public TextMeshProUGUI gamesPlayedText;

    void Start()
    {
        // Ana menü açýldýðýnda istatistikleri kutulara yazdýr
        LoadStats();
    }

    private void LoadStats()
    {
        // Cihazýn hafýzasýndaki (kasa) verileri çek
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        int gamesPlayed = PlayerPrefs.GetInt("GamesPlayed", 0);

        // Eðer kutular baðlandýysa içlerine deðerleri yazdýr
        if (bestScoreText != null) bestScoreText.text = bestScore.ToString();
        if (totalCoinsText != null) totalCoinsText.text = totalCoins.ToString();
        if (gamesPlayedText != null) gamesPlayedText.text = gamesPlayed.ToString();
    }

    // --- MARKET GEÇÝÞ FONKSÝYONLARI ---

    public void OpenShop()
    {
        // Shop paneline görünür yap
        if (shopScreen != null) shopScreen.SetActive(true);
    }

    public void CloseShop()
    {
        // Shop panelini gizle
        if (shopScreen != null) shopScreen.SetActive(false);

        // ÇOK ÖNEMLÝ: Marketten ana menüye dönünce kalan altýn miktarýný ekranda güncelle!
        LoadStats();
    }

    public void PlayGame()
    {
        // OYUNA BAÞLA butonuna basýldýðýnda oynanma sayýsýný 1 artýrýp kaydet
        int currentGamesPlayed = PlayerPrefs.GetInt("GamesPlayed", 0);
        PlayerPrefs.SetInt("GamesPlayed", currentGamesPlayed + 1);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void LoadSceneByIndex(int index)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(index);
    }
}