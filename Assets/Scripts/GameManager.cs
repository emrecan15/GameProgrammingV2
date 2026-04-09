using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Referanslar (Otomatik Atanýr)")]
    // playerCar artýk Inspector'dan sürüklenmeyecek, kod otomatik atayacak.
    [HideInInspector] public CarController playerCar;
    public UIManager uiManager;
    public CameraFollow mainCamera;
    public ObstacleSpawner obstacleSpawner;

    [Header("Garaj (Market) Ayarlarý")]
    public GameObject[] carPrefabs; // Marketten alýnan arabalarýn PREFAB listesi
    public Transform carSpawnPoint; // Arabanýn doðacaðý baþlangýç noktasý (Boþ Obje)

    [Header("Oyun Ýçi UI (HUD)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI coinText;

    [Header("Skor Bilgileri")]
    public float currentScore;
    public int totalCoins;

    [Header("Subway Surfers Ayarlarý")]
    public float scoreMultiplier = 5f;

    private bool isGameActive = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // YENÝ: Oyun baþlarken marketten seçilen arabayý yarat
        SpawnSelectedCar();
    }

    void Start()
    {
        Time.timeScale = 1f;
        UpdateCoinUI();

        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>();
    }

    private void SpawnSelectedCar()
    {
        // Eðer prefab listesi doluysa ve doðma noktasý (SpawnPoint) belirlendiyse
        if (carPrefabs != null && carPrefabs.Length > 0 && carSpawnPoint != null)
        {
            // Cihazýn hafýzasýndan seçili arabanýn indexini (sýrasýný) al (Örn: 0, 1 veya 2)
            int selectedCarIndex = PlayerPrefs.GetInt("SelectedCarIndex", 0);

            // Güvenlik: Eðer kayýtlý index, bizim araba listemizden büyükse çökmemesi için 0. arabayý seç
            if (selectedCarIndex >= carPrefabs.Length) selectedCarIndex = 0;

            // Arabayý SpawnPoint noktasýnda doður (Instantiate)
            GameObject spawnedCar = Instantiate(carPrefabs[selectedCarIndex], carSpawnPoint.position, carSpawnPoint.rotation);
            playerCar = spawnedCar.GetComponent<CarController>();

            // Doðurulan bu yeni arabayý sisteme tanýt:
            // 1. Kamerayý yeni doðan arabaya kilitle
            if (mainCamera != null) mainCamera.target = spawnedCar.transform;

            // 2. Engel üreticiye yeni doðan arabayý bildir (Engeller/Altýnlar arabanýn önüne çýksýn)
            if (obstacleSpawner != null) obstacleSpawner.playerCar = playerCar;
        }
        else
        {
            Debug.LogWarning("GameManager'da CarPrefabs veya CarSpawnPoint eksik!");
        }
    }

    void Update()
    {
        if (isGameActive && playerCar != null)
        {
            currentScore += (playerCar.forwardSpeed * scoreMultiplier) * Time.deltaTime;

            if (scoreText != null)
            {
                scoreText.text = "SCORE\n" + Mathf.FloorToInt(currentScore).ToString();
            }
        }
    }

    public void AddCoin()
    {
        totalCoins++;
        UpdateCoinUI();
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = "COINS\n" + totalCoins.ToString();
        }
    }

    public void GameOver()
    {
        if (!isGameActive) return;

        isGameActive = false;
        Debug.Log("ENGELE ÇARPTIN! Final Skor: " + Mathf.FloorToInt(currentScore));

        // Oyun bittiðinde toplanan altýnlarý cihaza kaydet (Market için)
        int savedCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        PlayerPrefs.SetInt("TotalCoins", savedCoins + totalCoins);
        PlayerPrefs.Save();

        if (uiManager != null)
        {
            uiManager.ShowGameOver(Mathf.FloorToInt(currentScore), totalCoins);
        }
        else
        {
            Time.timeScale = 0f;
        }
    }
}