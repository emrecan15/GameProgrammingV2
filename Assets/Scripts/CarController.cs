using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class CarController : MonoBehaviour
{
    
    [Header("Efektler (SFX)")]
    public AudioSource driftAudio;
    public AudioSource sfxSource;
    public AudioClip powerUpPickupSound;
    public AudioClip shieldBreakSound;
    public AudioClip speedBumpSound;
    public float speedBumpVolume = 1f;
    public AudioClip coinPickupSound;
    public float coinPickupVolume = 0.5f;
    public AudioClip crashSound;
    public float crashVolume = 5f;

    [Header("Araç Sesleri")]
    public AudioSource engineAudio;
    public AudioClip hornSound;

    public float minEnginePitch = 0.85f;
    public float maxEnginePitch = 1.35f;
    public float minEngineVolume = 0.20f;
    public float maxEngineVolume = 0.45f;

    [Header("Spline Ayarları")]
    public SplineContainer trackSpline;
    [HideInInspector] public float progress;

    [Header("Hareket Ayarları")]
    public float forwardSpeed = 25f;
    public float maxSpeed = 100f;
    public float acceleration = 0.5f;

    [Header("Şerit Ayarları")]
    public float laneDistance = 3f;
    public float laneChangeSmoothTime = 0.1f;
    public float groundOffset = 0.2f;

    [Header("Görsel Dönüş")]
    public float turnAngle = 15f;
    public float turnSpeed = 15f;

    [Header("Güçlendirici: Mıknatıs")]
    [HideInInspector] public bool isMagnetActive;
    public float magnetDuration = 10f;
    public float magnetRadius = 25f;
    public float magnetPullSpeed = 50f;

    [Header("Güçlendirici: Nitro")]
    [HideInInspector] public bool isNitroActive;
    [HideInInspector] public bool isNitroEnding;
    public float nitroDuration = 5f;
    public float nitroSpeedMultiplier = 1.8f;

    [Header("Güçlendirici: 2x Altın")]
    [HideInInspector] public bool isDoubleCoinActive;
    public float doubleCoinDuration = 10f;

    [Header("Güçlendirici: Kalkan")]
    [HideInInspector] public bool isShieldActive;
    public float shieldDuration = 15f;
    public GameObject shieldVisual;

    [HideInInspector] public Vector3 currentTrackForward;
    [HideInInspector] public float publicXOffset;

    private int currentLane;
    private float currentXOffset;
    private float xOffsetVelocity;
    private float cachedSplineLength;
    private float baseForwardSpeed;
    private bool isDead;

    private float initialSpeed; 

    private float totalDistanceTraveled;

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

        currentLane = 1;

        initialSpeed = forwardSpeed; 
        baseForwardSpeed = forwardSpeed;

        if (shieldVisual != null) shieldVisual.SetActive(false);
    }

    void Start()
    {
        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;

        totalDistanceTraveled = 0f;

        if (engineAudio != null)
        {
            engineAudio.loop = true;
            engineAudio.Play();
        }
    }

    void Update()
    {
        if (isDead || trackSpline == null) return;

        HandleSpeed();
        HandleInput();
        CalculateMovement();
        UpdateEngineSound();


        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (sfxSource != null && hornSound != null)
            {
                sfxSource.PlayOneShot(hornSound);
            }
        }

        if (PowerUpUIManager.Instance != null)
        {
            PowerUpUIManager.Instance.UpdateDashboard(totalDistanceTraveled, forwardSpeed);
        }
    }

    void HandleSpeed()
    {
        if (baseForwardSpeed < maxSpeed)
            baseForwardSpeed += acceleration * Time.deltaTime;

        if (isNitroActive)
        {
            float nitroTarget = maxSpeed * nitroSpeedMultiplier;
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, nitroTarget, 60f * Time.deltaTime);
        }
        else if (forwardSpeed > baseForwardSpeed)
        {
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, baseForwardSpeed, 80f * Time.deltaTime);
        }
        else
        {
            forwardSpeed = baseForwardSpeed;
        }
    }

    void HandleInput()
    {
        if (Keyboard.current == null) return;

        int prev = currentLane;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            currentLane = Mathf.Min(2, currentLane + 1);

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            currentLane = Mathf.Max(0, currentLane - 1);

        if (currentLane != prev && driftAudio != null)
            driftAudio.Play();
    }

    void CalculateMovement()
    {
        float moveDistance = forwardSpeed * Time.deltaTime;
        totalDistanceTraveled += moveDistance;

        progress += moveDistance / cachedSplineLength; 
        progress = Mathf.Repeat(progress, 1f);

        trackSpline.Evaluate(progress, out float3 pos, out float3 forward, out float3 up);
        forward = math.normalize(forward);
        up = math.normalize(up);
        float3 right = math.cross(up, forward);

        currentTrackForward = (Vector3)forward;
        publicXOffset = currentXOffset;

        float targetXOffset = (currentLane - 1) * laneDistance;
        currentXOffset = Mathf.SmoothDamp(currentXOffset, targetXOffset, ref xOffsetVelocity, laneChangeSmoothTime);

        transform.position = (Vector3)pos + (Vector3)right * currentXOffset + (Vector3)up * groundOffset;

        float xDiff = targetXOffset - currentXOffset;
        Quaternion baseRot = Quaternion.LookRotation(forward, up);
        Quaternion turnRot = Quaternion.Euler(0, xDiff * turnAngle, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, baseRot * turnRot, turnSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("Obstacle"))
        {
            bool atNitroSpeed = forwardSpeed > (baseForwardSpeed + 5f);

            if (isNitroActive || atNitroSpeed)
            {
                other.gameObject.SetActive(false);
            }
            else if (isShieldActive)
            {
                isShieldActive = false;
                
                if (sfxSource != null && shieldBreakSound != null) sfxSource.PlayOneShot(shieldBreakSound);

                if (shieldVisual != null) shieldVisual.SetActive(false);
                if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);

                if (PowerUpUIManager.Instance != null) PowerUpUIManager.Instance.HidePowerUp("Shield");

                other.gameObject.SetActive(false);
            }
            else
            {
                isDead = true;
                forwardSpeed = 0f;

                if (engineAudio != null)
                {
                    engineAudio.Stop();
                }

                if (sfxSource != null && crashSound != null)
                {
                    sfxSource.PlayOneShot(crashSound, crashVolume);
                }

                if (magnetCoroutine != null) StopCoroutine(magnetCoroutine);
                if (nitroCoroutine != null) StopCoroutine(nitroCoroutine);
                if (doubleCoinCoroutine != null) StopCoroutine(doubleCoinCoroutine);
                if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);

                if (PowerUpUIManager.Instance != null) PowerUpUIManager.Instance.HideAll();

                if (GameManager.Instance != null) GameManager.Instance.GameOver();
            }
        }
        else if (other.CompareTag("SpeedBump"))
        {
            if (isNitroActive) 
            {
                other.gameObject.SetActive(false);
            }
            else
            {
                /* set currentSpeed to initialSpeed
                 
                baseForwardSpeed = initialSpeed; 
                forwardSpeed = initialSpeed;
                */

                
                forwardSpeed *= 0.8f;

                baseForwardSpeed = forwardSpeed;

                forwardSpeed = Mathf.Max(forwardSpeed, initialSpeed);
                baseForwardSpeed = Mathf.Max(baseForwardSpeed, initialSpeed);


                if (sfxSource != null && speedBumpSound != null) 
                {
                    sfxSource.PlayOneShot(speedBumpSound,speedBumpVolume);
                }

                other.gameObject.SetActive(false);
            }
        }
        else if (other.CompareTag("Coin"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin();

                if (isDoubleCoinActive)
                    GameManager.Instance.AddCoin();
            }

            // Coin sesi
            if (sfxSource != null && coinPickupSound != null)
            {
                sfxSource.PlayOneShot(coinPickupSound, coinPickupVolume);
            }

            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("Magnet"))
        {
            PlayPowerUpSound(); 
            if (magnetCoroutine != null) StopCoroutine(magnetCoroutine);
            magnetCoroutine = StartCoroutine(MagnetRoutine());
            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("Nitro"))
        {
            PlayPowerUpSound(); 
            if (nitroCoroutine != null) StopCoroutine(nitroCoroutine);
            nitroCoroutine = StartCoroutine(NitroRoutine());
            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("DoubleCoin"))
        {
            PlayPowerUpSound(); 
            if (doubleCoinCoroutine != null) StopCoroutine(doubleCoinCoroutine);
            doubleCoinCoroutine = StartCoroutine(DoubleCoinRoutine());
            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("Shield"))
        {
            PlayPowerUpSound(); 
            if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
            shieldCoroutine = StartCoroutine(ShieldRoutine());
            other.gameObject.SetActive(false);
        }
    }

    private void PlayPowerUpSound()
    {
        if (sfxSource != null && powerUpPickupSound != null)
        {
            sfxSource.PlayOneShot(powerUpPickupSound);
        }
    }

    private System.Collections.IEnumerator MagnetRoutine()
    {
        isMagnetActive = true;
        if (PowerUpUIManager.Instance != null) PowerUpUIManager.Instance.ShowPowerUp("Magnet");

        float timer = magnetDuration;
        while (timer > 0f)
        {
            if (PowerUpUIManager.Instance != null)
                PowerUpUIManager.Instance.UpdateTimer("Magnet", Mathf.CeilToInt(timer));

            timer -= Time.deltaTime;
            yield return null;
        }

        isMagnetActive = false;
        if (PowerUpUIManager.Instance != null) PowerUpUIManager.Instance.HidePowerUp("Magnet");
    }

    private System.Collections.IEnumerator NitroRoutine()
    {
        isNitroActive = true;
        isNitroEnding = false;
        if (PowerUpUIManager.Instance != null) PowerUpUIManager.Instance.ShowPowerUp("Nitro");

        // Engelleri temizle
        foreach (GameObject obs in GameObject.FindGameObjectsWithTag("Obstacle"))
            obs.SetActive(false);

        // YENİ: Kasisleri de temizle
        foreach (GameObject bump in GameObject.FindGameObjectsWithTag("SpeedBump"))
            bump.SetActive(false);

        float timer = nitroDuration;
        while (timer > 0f)
        {
            if (PowerUpUIManager.Instance != null)
                PowerUpUIManager.Instance.UpdateTimer("Nitro", Mathf.CeilToInt(timer));

            timer -= Time.deltaTime;
            if (timer <= 2f && !isNitroEnding) isNitroEnding = true;
            yield return null;
        }

        isNitroActive = false;
        isNitroEnding = false;
        if (PowerUpUIManager.Instance != null) PowerUpUIManager.Instance.HidePowerUp("Nitro");
    }

    private System.Collections.IEnumerator DoubleCoinRoutine()
    {
        isDoubleCoinActive = true;
        if (PowerUpUIManager.Instance != null) PowerUpUIManager.Instance.ShowPowerUp("DoubleCoin");

        float timer = doubleCoinDuration;
        while (timer > 0f)
        {
            if (PowerUpUIManager.Instance != null)
                PowerUpUIManager.Instance.UpdateTimer("DoubleCoin", Mathf.CeilToInt(timer));

            timer -= Time.deltaTime;
            yield return null;
        }

        isDoubleCoinActive = false;
        if (PowerUpUIManager.Instance != null) PowerUpUIManager.Instance.HidePowerUp("DoubleCoin");
    }

    private System.Collections.IEnumerator ShieldRoutine()
    {
        isShieldActive = true;
        if (shieldVisual != null) shieldVisual.SetActive(true);
        if (PowerUpUIManager.Instance != null) PowerUpUIManager.Instance.ShowPowerUp("Shield");

        BlinkVisual blinker = shieldVisual != null ? shieldVisual.GetComponent<BlinkVisual>() : null;
        bool blinkStarted = false;
        float timer = shieldDuration;

        while (timer > 0f)
        {
            if (PowerUpUIManager.Instance != null)
                PowerUpUIManager.Instance.UpdateTimer("Shield", Mathf.CeilToInt(timer));

            timer -= Time.deltaTime;

            if (timer <= 3f && !blinkStarted)
            {
                blinkStarted = true;
                if (blinker != null) blinker.StartBlinking();
            }

            yield return null;
        }

        if (isShieldActive)
        {
            if (sfxSource != null && shieldBreakSound != null) sfxSource.PlayOneShot(shieldBreakSound);
        }

        isShieldActive = false;
        if (blinker != null) blinker.StopBlinking();
        if (shieldVisual != null) shieldVisual.SetActive(false);
        if (PowerUpUIManager.Instance != null) PowerUpUIManager.Instance.HidePowerUp("Shield");
    }

    void UpdateEngineSound()
    {
        if (engineAudio == null) return;

        float speedPercent = Mathf.InverseLerp(initialSpeed, maxSpeed, forwardSpeed);

        engineAudio.pitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, speedPercent);
        engineAudio.volume = Mathf.Lerp(minEngineVolume, maxEngineVolume, speedPercent);
    }
}