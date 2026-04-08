using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using Unity.Mathematics;

public class CarController : MonoBehaviour
{
    [Header("Spline Ayarlarý")]
    public SplineContainer trackSpline;
    [HideInInspector] public float progress = 0f;

    [Header("Hareket Ayarlarý")]
    public float forwardSpeed = 20.0f;
    public float maxSpeed = 100.0f;
    public float acceleration = 0.5f;

    [Header("Þerit Ayarlarý")]
    public float laneDistance = 3.0f;
    public float laneChangeSmoothTime = 0.1f;
    public float groundOffset = 0.2f;

    [Header("Görsel Dönüþ")]
    public float turnAngle = 15.0f;
    public float turnSpeed = 15.0f;

    // --- KAMERANIN OKUDUÐU 2 DEÐÝÞKEN ---
    [HideInInspector] public Vector3 currentTrackForward;
    [HideInInspector] public float publicXOffset;

    private int currentLane = 1;
    private float currentXOffset = 0f;
    private float xOffsetVelocity = 0f;
    private float cachedSplineLength;

    // Kaza durumunu takip eden deðiþken
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
        // Eðer öldüysek hiçbir hesaplama yapma, araç olduðu yerde kalsýn
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

        // Deðiþkenleri güncelle (Kameranýn çalýþmasý için)
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
        // Daha önce ölmediysek ve bir engele çarptýysak
        if (!isDead && other.CompareTag("Obstacle"))
        {
            isDead = true; // Hareketi kilitle
            forwardSpeed = 0; // Hýzý sýfýrla

            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
        else if (!isDead && other.CompareTag("Coin"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddCoin();

            other.gameObject.SetActive(false);
        }
    }
}