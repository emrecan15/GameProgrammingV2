using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Referanslar")]
    [HideInInspector] public CarController playerCar;
    public UIManager uiManager;
    public CameraFollow mainCamera;
    public ObstacleSpawner obstacleSpawner;

    [Header("Garaj Ayarlarý")]
    public GameObject[] carPrefabs;
    public Transform carSpawnPoint;

    [Header("Oyun Ýçi UI (HUD)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI coinText;

    [Header("Skor Ayarlarý")]
    public float scoreMultiplier = 5f;

    public float currentScore { get; private set; }
    public int totalCoins { get; private set; }

    private bool isGameActive = true;

    // UI'ý her frame deðil, bu saniye aralýðýnda güncelle
    private const float UI_UPDATE_INTERVAL = 0.1f;
    private float uiUpdateTimer = 0f;

    // Son gösterilen deðerleri sakla, deðiþmediyse text'e yazma (re-render engellenir)
    private int lastDisplayedScore = -1;
    private int lastDisplayedCoins = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        SpawnSelectedCar();
    }

    void Start()
    {
        Time.timeScale = 1f;
        ForceUpdateUI();

        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>();
    }

    void SpawnSelectedCar()
    {
        if (carPrefabs == null || carPrefabs.Length == 0 || carSpawnPoint == null)
        {
            Debug.LogWarning("GameManager: CarPrefabs veya CarSpawnPoint eksik!");
            return;
        }

        int index = PlayerPrefs.GetInt("SelectedCarIndex", 0);
        if (index >= carPrefabs.Length) index = 0;

        GameObject spawnedCar = Instantiate(carPrefabs[index], carSpawnPoint.position, carSpawnPoint.rotation);
        playerCar = spawnedCar.GetComponent<CarController>();

        if (mainCamera != null) mainCamera.target = spawnedCar.transform;
        if (obstacleSpawner != null) obstacleSpawner.playerCar = playerCar;
    }

    void Update()
    {
        if (!isGameActive || playerCar == null) return;

        currentScore += playerCar.forwardSpeed * scoreMultiplier * Time.deltaTime;

        // UI her frame deðil, 0.1 saniyede bir güncellenir.
        // Bu GC baskýsýný ve TextMeshPro re-render'ý büyük ölçüde azaltýr.
        uiUpdateTimer += Time.deltaTime;
        if (uiUpdateTimer >= UI_UPDATE_INTERVAL)
        {
            uiUpdateTimer = 0f;
            UpdateScoreUI();
        }
    }

    public void AddCoin()
    {
        totalCoins++;
        // Coin UI'ý anlýk güncellenir (coin toplama nadir, sorun deðil)
        UpdateCoinUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText == null) return;
        int score = Mathf.FloorToInt(currentScore);
        // Deðer deðiþmediyse text'e yazma — TextMeshPro'nun dirty/re-render döngüsünü engeller
        if (score == lastDisplayedScore) return;
        lastDisplayedScore = score;
        scoreText.text = "SCORE\n" + score;
    }

    void UpdateCoinUI()
    {
        if (coinText == null) return;
        if (totalCoins == lastDisplayedCoins) return;
        lastDisplayedCoins = totalCoins;
        coinText.text = "COINS\n" + totalCoins;
    }

    // Oyun baþýnda UI'ý zorla güncelle (lastDisplayed -1 olduðu için çalýþýr)
    void ForceUpdateUI()
    {
        lastDisplayedScore = -1;
        lastDisplayedCoins = -1;
        UpdateScoreUI();
        UpdateCoinUI();
    }

    public void GameOver()
    {
        if (!isGameActive) return;
        isGameActive = false;

        int finalScore = Mathf.FloorToInt(currentScore);

        int savedCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        PlayerPrefs.SetInt("TotalCoins", savedCoins + totalCoins);
        PlayerPrefs.Save();

        if (uiManager != null)
            uiManager.ShowGameOver(finalScore, totalCoins);
        else
            Time.timeScale = 0f;
    }
}