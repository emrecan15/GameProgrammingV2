using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections; // Animasyonlar (Coroutine) için gerekli

public class UIManager : MonoBehaviour
{
    [Header("Oyun Ýçi Arayüz (HUD)")]
    public GameObject pauseButtonHUD;

    [Header("Paneller")]
    public GameObject pauseScreen;
    public GameObject gameOverScreen; // Figma'da Game Over çizince buraya koyacaksýn

    [Header("Game Over Yazýlarý")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI collectedCoinsText;
    public TextMeshProUGUI bestScoreText;

    [Header("Animasyon Ayarlarý")]
    public float countAnimationDuration = 1.0f; // Skorlarýn 0'dan yükeselme süresi

    private bool isPaused = false;
    private bool isGameOver = false;

    void Start()
    {
        if (pauseScreen != null) pauseScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (pauseButtonHUD != null) pauseButtonHUD.SetActive(true);
    }

    // --- PAUSE SÝSTEMÝ ---

    public void PauseGame()
    {
        if (isGameOver || isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseScreen != null) pauseScreen.SetActive(true);
        if (pauseButtonHUD != null) pauseButtonHUD.SetActive(false);
    }

    public void ResumeGame()
    {
        if (isGameOver) return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseScreen != null) pauseScreen.SetActive(false);
        if (pauseButtonHUD != null) pauseButtonHUD.SetActive(true);
    }

    // --- GAME OVER SÝSTEMÝ (GameManager Burayý Çaðýrýr) ---

    public void ShowGameOver(int finalScore, int collectedCoins)
    {
        if (isGameOver) return;
        isGameOver = true;

        // Oyun duraklatýlmýþsa Pause menüsünü kapat
        if (pauseScreen != null) pauseScreen.SetActive(false);
        if (pauseButtonHUD != null) pauseButtonHUD.SetActive(false);

        // Game Over ekranýný aç ve zamaný dondur
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
        Time.timeScale = 0f;

        // En Yüksek Skoru (Best Score) Kaydetme
        int currentBest = PlayerPrefs.GetInt("BestScore", 0);
        if (finalScore > currentBest)
        {
            PlayerPrefs.SetInt("BestScore", finalScore);
            PlayerPrefs.Save();
            currentBest = finalScore;
        }

        // Skorlarý Animasyonlu Þekilde Ekrana Yazdýr
        if (finalScoreText != null)
            StartCoroutine(AnimateNumber(finalScoreText, finalScore, ""));

        if (collectedCoinsText != null)
            StartCoroutine(AnimateNumber(collectedCoinsText, collectedCoins, ""));

        if (bestScoreText != null)
            bestScoreText.text = "" + currentBest.ToString();
    }

    // Sayýlarý 0'dan hedefe doðru hýzla saydýran animasyon fonksiyonu
    private IEnumerator AnimateNumber(TextMeshProUGUI textComponent, int targetValue, string prefix)
    {
        float elapsed = 0f;
        int startingValue = 0;

        while (elapsed < countAnimationDuration)
        {
            // Zaman donduðu için (timeScale=0) unscaledDeltaTime kullanýyoruz
            elapsed += Time.unscaledDeltaTime;
            float currentValue = Mathf.Lerp(startingValue, targetValue, elapsed / countAnimationDuration);
            textComponent.text = prefix + Mathf.RoundToInt(currentValue).ToString();
            yield return null;
        }

        textComponent.text = prefix + targetValue.ToString();
    }

    // --- GENEL BUTON FONKSÝYONLARI ---

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}