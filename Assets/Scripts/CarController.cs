using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using Unity.Mathematics;

public class CarController : MonoBehaviour
{
	[Header("Efektler (SFX)")]
	public AudioSource driftAudio;

	[Header("Spline Ayarları")]
	public SplineContainer trackSpline;
	[HideInInspector] public float progress = 0f;

	[Header("Hareket Ayarları")]
	public float forwardSpeed = 25.0f;
	public float maxSpeed = 100.0f;
	public float acceleration = 0.5f;

	[Header("Şerit Ayarları")]
	public float laneDistance = 3.0f;
	public float laneChangeSmoothTime = 0.1f;
	public float groundOffset = 0.2f;

	[Header("Görsel Dönüş")]
	public float turnAngle = 15.0f;
	public float turnSpeed = 15.0f;

	[Header("Güçlendiriciler (Mıknatıs)")]
	[HideInInspector] public bool isMagnetActive = false;
	public float magnetDuration = 10f;
	public float magnetRadius = 25f;
	public float magnetPullSpeed = 50f;

	[Header("Güçlendiriciler (Nitro)")]
	[HideInInspector] public bool isNitroActive = false;
	[HideInInspector] public bool isNitroEnding = false;
	public float nitroDuration = 5f;
	public float nitroSpeedMultiplier = 1.8f;

	[Header("Güçlendiriciler (2x Altın)")]
	[HideInInspector] public bool isDoubleCoinActive = false;
	public float doubleCoinDuration = 10f; 

	[Header("Güçlendiriciler (Kalkan)")]
	[HideInInspector] public bool isShieldActive = false;
	public float shieldDuration = 15f; 
	public GameObject shieldVisual; 

	[HideInInspector] public Vector3 currentTrackForward;
	[HideInInspector] public float publicXOffset;

	private int currentLane = 1;
	private float currentXOffset = 0f;
	private float xOffsetVelocity = 0f;
	private float cachedSplineLength;

	private bool isDead = false;
	private float baseForwardSpeed;

	private Coroutine magnetCoroutine;
	private Coroutine nitroCoroutine;
	private Coroutine doubleCoinCoroutine;
	private Coroutine shieldCoroutine;

	void Awake()
	{
		if (driftAudio == null) driftAudio = GetComponent<AudioSource>();

		if (trackSpline == null) trackSpline = FindFirstObjectByType<SplineContainer>();
		if (trackSpline != null) cachedSplineLength = trackSpline.CalculateLength();

		RoadManager rm = FindFirstObjectByType<RoadManager>();
		if (rm != null) rm.playerCar = this;

		ObstacleSpawner os = FindFirstObjectByType<ObstacleSpawner>();
		if (os != null) os.playerCar = this;

		CameraFollow cam = FindFirstObjectByType<CameraFollow>();
		if (cam != null) cam.target = this.transform;

		baseForwardSpeed = forwardSpeed;
		
		if (shieldVisual != null) shieldVisual.SetActive(false);
	}

	void Start()
	{
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
    }

	void Update()
	{
		if (isDead || trackSpline == null) return;

		HandleSpeed();
		HandleInput();
		CalculateMovement();
	}

	void HandleSpeed()
	{
		if (baseForwardSpeed < maxSpeed)
		{
			baseForwardSpeed += acceleration * Time.deltaTime;
		}

		if (isNitroActive)
		{
			float nitroTargetSpeed = maxSpeed * nitroSpeedMultiplier; 
			forwardSpeed = Mathf.MoveTowards(forwardSpeed, nitroTargetSpeed, 60f * Time.deltaTime);
		}
		else
		{
			if (forwardSpeed > baseForwardSpeed)
			{
				forwardSpeed = Mathf.MoveTowards(forwardSpeed, baseForwardSpeed, 80f * Time.deltaTime);
			}
			else
			{
				forwardSpeed = baseForwardSpeed;
			}
		}
	}

	void HandleInput()
	{
		if (Keyboard.current != null)
		{
			int previousLane = currentLane;

			if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
				currentLane = Mathf.Min(2, currentLane + 1);

			if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
				currentLane = Mathf.Max(0, currentLane - 1);

			if (currentLane != previousLane)
			{
				PlayDriftEffects();
			}
		}
	}

	private void PlayDriftEffects()
	{
		if (driftAudio != null) driftAudio.Play();
	}

	void CalculateMovement()
	{
		progress += (forwardSpeed * Time.deltaTime) / cachedSplineLength;
		progress = Mathf.Repeat(progress, 1f);

		float3 pos, forward, up;
		trackSpline.Evaluate(progress, out pos, out forward, out up);

		forward = math.normalize(forward);
		up = math.normalize(up);
		float3 right = math.cross(up, forward);

		currentTrackForward = (Vector3)forward;
		publicXOffset = currentXOffset;

		float targetXOffset = (currentLane - 1) * laneDistance;
		currentXOffset = Mathf.SmoothDamp(currentXOffset, targetXOffset, ref xOffsetVelocity, laneChangeSmoothTime);

		Vector3 finalPosition = (Vector3)pos + ((Vector3)right * currentXOffset);
		finalPosition += (Vector3)up * groundOffset;

		transform.position = finalPosition;

		float xDiff = targetXOffset - currentXOffset;
		float targetRotationY = xDiff * turnAngle;
		Quaternion baseRotation = Quaternion.LookRotation(forward, up);
		Quaternion turnRotation = Quaternion.Euler(0, targetRotationY, 0);
		transform.rotation = Quaternion.Lerp(transform.rotation, baseRotation * turnRotation, turnSpeed * Time.deltaTime);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (isDead) return;

		if (other.CompareTag("Obstacle"))
		{
			bool isBrakingFromNitro = forwardSpeed > (baseForwardSpeed + 5f);

			if (isNitroActive || isBrakingFromNitro)
			{
				other.gameObject.SetActive(false);
			}
			else if (isShieldActive)
			{
				isShieldActive = false;
				if (shieldVisual != null) shieldVisual.SetActive(false); 
				if (shieldCoroutine != null) StopCoroutine(shieldCoroutine); 
				other.gameObject.SetActive(false); 
			}
			else
			{
				isDead = true;
				forwardSpeed = 0;
				if (GameManager.Instance != null) GameManager.Instance.GameOver();
			}
		}
		else if (other.CompareTag("Coin"))
		{
			if (GameManager.Instance != null)
			{
				GameManager.Instance.AddCoin();
				if (isDoubleCoinActive) GameManager.Instance.AddCoin();
			}
			other.gameObject.SetActive(false);
		}
		else if (other.CompareTag("Magnet"))
		{
			if (magnetCoroutine != null) StopCoroutine(magnetCoroutine);
			magnetCoroutine = StartCoroutine(MagnetRoutine());
			other.gameObject.SetActive(false);
		}
		else if (other.CompareTag("Nitro"))
		{
			if (nitroCoroutine != null) StopCoroutine(nitroCoroutine);
			nitroCoroutine = StartCoroutine(NitroRoutine());
			other.gameObject.SetActive(false);
		}
		else if (other.CompareTag("DoubleCoin"))
		{
			if (doubleCoinCoroutine != null) StopCoroutine(doubleCoinCoroutine);
			doubleCoinCoroutine = StartCoroutine(DoubleCoinRoutine());
			other.gameObject.SetActive(false);
		}
		else if (other.CompareTag("Shield"))
		{
			if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
			shieldCoroutine = StartCoroutine(ShieldRoutine());
			other.gameObject.SetActive(false);
		}
	}

	private System.Collections.IEnumerator MagnetRoutine()
	{
		isMagnetActive = true;
		yield return new WaitForSeconds(magnetDuration);
		isMagnetActive = false;
	}

	private System.Collections.IEnumerator NitroRoutine()
	{
		isNitroActive = true;
		isNitroEnding = false;

		GameObject[] activeObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
		foreach (GameObject obs in activeObstacles)
		{
			obs.SetActive(false);
		}

		float timer = nitroDuration;
		while (timer > 0)
		{
			timer -= Time.deltaTime;
			if (timer <= 2.0f && !isNitroEnding) isNitroEnding = true;
			yield return null;
		}

		isNitroActive = false;
		isNitroEnding = false;
	}

	private System.Collections.IEnumerator DoubleCoinRoutine()
	{
		isDoubleCoinActive = true;
		yield return new WaitForSeconds(doubleCoinDuration);
		isDoubleCoinActive = false;
	}

	// YENİDEN YAZILAN KALKAN FONKSİYONU
	private System.Collections.IEnumerator ShieldRoutine()
	{
		isShieldActive = true;
		if (shieldVisual != null) shieldVisual.SetActive(true);

		// Yanıp sönme kodunu al
		BlinkVisual blinker = shieldVisual.GetComponent<BlinkVisual>();

		float timer = shieldDuration;
		bool blinkingStarted = false;

		while (timer > 0)
		{
			timer -= Time.deltaTime;

			// Son 3 saniye kala yanıp sönmeyi başlat
			if (timer <= 3.0f && !blinkingStarted)
			{
				blinkingStarted = true;
				if (blinker != null) blinker.StartBlinking();
			}

			yield return null;
		}

		// Süre bittiğinde kalkanı kapat ve yanıp sönmeyi durdur
		isShieldActive = false;
		if (blinker != null) blinker.StopBlinking();
		if (shieldVisual != null) shieldVisual.SetActive(false);
	}
}