using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using Unity.Mathematics;

public class CarController : MonoBehaviour
{
    [Header("Spline Ayarları")]
    public SplineContainer trackSpline;
    [HideInInspector] public float progress = 0f;

    [Header("Hareket Ayarları")]
    public float forwardSpeed = 20.0f;
    public float maxSpeed = 100.0f;
    public float acceleration = 0.5f;

    [Header("Şerit Ayarları")]
    public float laneDistance = 3.0f;
    public float laneChangeSmoothTime = 0.1f;
    public float groundOffset = 0.2f;

    [Header("Görsel Dönüş")]
    public float turnAngle = 15.0f;
    public float turnSpeed = 15.0f;

    [Header("Güçlendiriciler")]
    [HideInInspector] public bool isMagnetActive = false; 
    public float magnetDuration = 10f;  
    public float magnetRadius = 25f;    
    public float magnetPullSpeed = 50f; 

    // --- KAMERANIN OKUDUĞU 2 DEĞİŞKEN ---
    [HideInInspector] public Vector3 currentTrackForward;
    [HideInInspector] public float publicXOffset;

    private int currentLane = 1;
    private float currentXOffset = 0f;
    private float xOffsetVelocity = 0f;
    private float cachedSplineLength;

    // Kaza durumunu takip eden değişken
    private bool isDead = false;

    void Start()
    {
        Application.targetFrameRate = 60;

        if (trackSpline != null)
        {
            cachedSplineLength = trackSpline.CalculateLength();
        }
    }

    void Update()
    {
        // Eğer öldüysek hiçbir hesaplama yapma, araç olduğu yerde kalsın
        if (isDead || trackSpline == null) return;

        HandleSpeed();
        HandleInput();
        CalculateMovement();
    }

    void HandleSpeed()
    {
        if (forwardSpeed < maxSpeed) forwardSpeed += acceleration * Time.deltaTime;
    }

    void HandleInput()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
                currentLane = Mathf.Min(2, currentLane + 1);

            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
                currentLane = Mathf.Max(0, currentLane - 1);
        }
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

        // Değişkenleri güncelle (Kameranın çalışması için)
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
        // Daha önce ölmediysek ve bir engele çarptıysak
        if (!isDead && other.CompareTag("Obstacle"))
        {
            isDead = true; // Hareketi kilitle
            forwardSpeed = 0; // Hızı sıfırla

            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
        else if (!isDead && other.CompareTag("Coin"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddCoin();

            other.gameObject.SetActive(false);
        }
        // YENİ: Mıknatıs Toplama
        else if (!isDead && other.CompareTag("Magnet"))
        {
            StartCoroutine(MagnetRoutine()); // Mıknatıs süresini başlat
            other.gameObject.SetActive(false); // Mıknatısı gizle
        }
    }

    // YENİ: Mıknatıs Süresi
    private System.Collections.IEnumerator MagnetRoutine()
    {
        isMagnetActive = true; 
        yield return new WaitForSeconds(magnetDuration); 
        isMagnetActive = false; 
    }
}