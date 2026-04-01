using UnityEngine;
using TMPro; // textmash lib

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	[Header("Referanslar")]
	public Transform playerTransform;
	public TextMeshProUGUI scoreText; 
	public TextMeshProUGUI coinText;  

	[Header("Skor Bilgileri")]
	public float currentScore;
	public int totalCoins;

	private bool isGameActive = true;
	private float startZPos;

	private void Awake()
	{
		if (Instance == null) Instance = this;
		else Destroy(gameObject);
	}

	void Start()
	{
		Time.timeScale = 1f;

		if (playerTransform != null)
		{
			startZPos = playerTransform.position.z;
		}

		// default coin text = 0 when the game starts
		UpdateCoinUI();
	}

	void Update()
	{
		if (isGameActive && playerTransform != null)
		{
			
			float distanceScore = playerTransform.position.z - startZPos;

			
			currentScore = distanceScore + (totalCoins * 10);

			
			if (scoreText != null)
			{
				scoreText.text = "Score: " + Mathf.FloorToInt(currentScore).ToString();
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
			coinText.text = "Coins: " + totalCoins.ToString();
		}
	}

	public void GameOver()
	{
		isGameActive = false;
		Debug.Log("ENGELE ÇARPTIN! Final Skor: " + Mathf.FloorToInt(currentScore));
		Time.timeScale = 0f;
	}
}