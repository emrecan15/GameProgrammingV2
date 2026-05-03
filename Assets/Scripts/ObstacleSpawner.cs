using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Havuz Ayarları (Engeller)")]
    public GameObject[] obstaclePrefabs;
    public int poolSizePerPrefab = 5;
    public Vector3 obstacleRotationOffset;
    public float obstacleHeightOffset = 0f;

    [Header("Havuz Ayarları (Altın)")]
    public GameObject coinPrefab;
    public int coinPoolSize = 30;
    public Vector3 coinRotationOffset;
    public float coinHeightOffset = 0.5f;
    [Range(0f, 100f)] public float coinSpawnChance = 60f;

    [Header("Havuz Ayarları (Mıknatıs)")]
    public GameObject magnetPrefab;
    public int magnetPoolSize = 1;
    public Vector3 magnetRotationOffset;
    public float magnetHeightOffset = 0.5f;
    [Range(0f, 100f)] public float magnetSpawnChance = 25f;

    [Header("Havuz Ayarları (Nitro)")]
    public GameObject nitroPrefab;
    public int nitroPoolSize = 1;
    public Vector3 nitroRotationOffset;
    public float nitroHeightOffset = 0.5f;
    [Range(0f, 100f)] public float nitroSpawnChance = 25f;

    [Header("Havuz Ayarları (2x Altın)")]
    public GameObject doubleCoinPrefab;
    public int doubleCoinPoolSize = 1;
    public Vector3 doubleCoinRotationOffset;
    public float doubleCoinHeightOffset = 0.5f;
    [Range(0f, 100f)] public float doubleCoinSpawnChance = 25f;

    [Header("Havuz Ayarları (Kalkan)")]
    public GameObject shieldPrefab;
    public int shieldPoolSize = 1;
    public Vector3 shieldRotationOffset;
    public float shieldHeightOffset = 0.5f;
    [Range(0f, 100f)] public float shieldSpawnChance = 25f;

    [Header("Doğma Ayarları")]
    public CarController playerCar;
    public float spawnDistanceAhead = 60f;
    public float laneDistance = 3.5f;

    [Header("Zorluk Ayarları")]
    public float spawnDistanceInterval = 25f;
    public int minSpawnsBetweenPowerUps = 4;

    private SplineContainer spline;
    private float cachedSplineLength;

    private List<List<GameObject>> obstaclePools;
    private List<GameObject> coinPool;
    private List<GameObject> magnetPool;
    private List<GameObject> nitroPool;
    private List<GameObject> doubleCoinPool;
    private List<GameObject> shieldPool;

    private float lastCarProgress;
    private int splineLoopCount;
    private float absoluteCarDistance;
    private float lastSpawnDist;
    private int currentPowerUpCooldown;

    private int cleanupFrameInterval = 10;
    private int cleanupFrameCounter = 0;

    private struct ActiveObject { public GameObject obj; }
    private List<ActiveObject> activeSpawns = new List<ActiveObject>();

    void Start()
    {
        if (playerCar == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerCar = playerObj.GetComponent<CarController>();
        }

        if (playerCar != null && playerCar.trackSpline != null)
        {
            spline = playerCar.trackSpline;
            cachedSplineLength = spline.CalculateLength();
            lastCarProgress = playerCar.progress;
            absoluteCarDistance = lastCarProgress * cachedSplineLength;
            lastSpawnDist = absoluteCarDistance;
        }

        obstaclePools = new List<List<GameObject>>();
        if (obstaclePrefabs != null)
            foreach (GameObject prefab in obstaclePrefabs)
                obstaclePools.Add(CreatePool(prefab, poolSizePerPrefab));

        coinPool = CreatePool(coinPrefab, coinPoolSize);
        magnetPool = CreatePool(magnetPrefab, magnetPoolSize);
        nitroPool = CreatePool(nitroPrefab, nitroPoolSize);
        doubleCoinPool = CreatePool(doubleCoinPrefab, doubleCoinPoolSize);
        shieldPool = CreatePool(shieldPrefab, shieldPoolSize);
    }

    List<GameObject> CreatePool(GameObject prefab, int size)
    {
        List<GameObject> pool = new List<GameObject>();
        if (prefab == null) return pool;
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Add(obj);
        }
        return pool;
    }

    void Update()
    {
        if (spline == null || playerCar == null) return;

        float currentProgress = playerCar.progress;

        if (lastCarProgress > 0.8f && currentProgress < 0.2f)
            splineLoopCount++;

        lastCarProgress = currentProgress;
        absoluteCarDistance = (splineLoopCount + currentProgress) * cachedSplineLength;

        if (absoluteCarDistance - lastSpawnDist >= spawnDistanceInterval)
        {
            SpawnWave();
            lastSpawnDist = absoluteCarDistance;
        }

        cleanupFrameCounter++;
        if (cleanupFrameCounter >= cleanupFrameInterval)
        {
            cleanupFrameCounter = 0;
            CleanupOldSpawns();
        }
    }

    void CleanupOldSpawns()
    {
        if (playerCar == null) return;
        Vector3 carPos = playerCar.transform.position;
        Vector3 carForward = playerCar.currentTrackForward;

        for (int i = activeSpawns.Count - 1; i >= 0; i--)
        {
            ActiveObject entry = activeSpawns[i];

            if (entry.obj == null || !entry.obj.activeInHierarchy)
            {
                activeSpawns.RemoveAt(i);
                continue;
            }

            Vector3 toObject = entry.obj.transform.position - carPos;
            if (Vector3.Dot(carForward, toObject) < -15f)
            {
                entry.obj.SetActive(false);
                activeSpawns.RemoveAt(i);
            }
        }
    }

    void SpawnWave()
    {
        float targetSpawnDist = absoluteCarDistance + spawnDistanceAhead;
        float spawnProgress = (targetSpawnDist % cachedSplineLength) / cachedSplineLength;

        spline.Evaluate(spawnProgress, out float3 pos, out float3 forward, out float3 up);
        forward = math.normalize(forward);
        up = math.normalize(up);
        float3 right = math.cross(up, forward);

        Quaternion baseRotation = Quaternion.LookRotation(forward, up);
        List<int> usedLanes = new List<int>();

        // ── POWER-UP ──────────────────────────────────────────────────────────
        bool anyActive = playerCar.isMagnetActive || playerCar.isNitroActive
                       || playerCar.isDoubleCoinActive || playerCar.isShieldActive;

        if (!anyActive)
        {
            if (currentPowerUpCooldown > 0)
            {
                currentPowerUpCooldown--;
            }
            else
            {
                float total = magnetSpawnChance + nitroSpawnChance + doubleCoinSpawnChance + shieldSpawnChance;
                float roll = UnityEngine.Random.Range(0f, total);
                int pLane = UnityEngine.Random.Range(0, 3);
                GameObject spawnedPowerUp = null;

                if (roll < magnetSpawnChance)
                    spawnedPowerUp = PlaceObject(magnetPool, pLane, baseRotation, magnetRotationOffset, pos, right, up, magnetHeightOffset);
                else if (roll < magnetSpawnChance + nitroSpawnChance)
                    spawnedPowerUp = PlaceObject(nitroPool, pLane, baseRotation, nitroRotationOffset, pos, right, up, nitroHeightOffset);
                else if (roll < magnetSpawnChance + nitroSpawnChance + doubleCoinSpawnChance)
                    spawnedPowerUp = PlaceObject(doubleCoinPool, pLane, baseRotation, doubleCoinRotationOffset, pos, right, up, doubleCoinHeightOffset);
                else
                    spawnedPowerUp = PlaceObject(shieldPool, pLane, baseRotation, shieldRotationOffset, pos, right, up, shieldHeightOffset);

                if (spawnedPowerUp != null)
                {
                    usedLanes.Add(pLane);
                    currentPowerUpCooldown = minSpawnsBetweenPowerUps;
                }
            }
        }

        // ── ENGEL ─────────────────────────────────────────────────────────────
        // DÜZELTME: Nitro bitiyorsa (isNitroEnding) engeller yeniden doğmaya başlasın.
        bool spawnObstacle = (!playerCar.isNitroActive || playerCar.isNitroEnding) && obstaclePools.Count > 0;

        if (spawnObstacle)
        {
            List<int> obstacleLanes = new List<int> { 0, 1, 2 };
            foreach (int l in usedLanes) obstacleLanes.Remove(l);
            if (obstacleLanes.Count == 0) obstacleLanes = new List<int> { 0, 1, 2 };

            int lane = obstacleLanes[UnityEngine.Random.Range(0, obstacleLanes.Count)];
            int poolIndex = UnityEngine.Random.Range(0, obstaclePools.Count);
            
            GameObject spawnedObstacle = PlaceObject(obstaclePools[poolIndex], lane, baseRotation, obstacleRotationOffset, pos, right, up, obstacleHeightOffset);
            
            // EĞER NİTRO BİTİYORSA YANIP SÖNDÜR
            if (spawnedObstacle != null && playerCar.isNitroEnding)
            {
                BlinkEffect blinker = spawnedObstacle.GetComponent<BlinkEffect>();
                if (blinker == null) blinker = spawnedObstacle.AddComponent<BlinkEffect>(); 
                blinker.StartBlinking(2.0f); 
            }

            if (!usedLanes.Contains(lane)) usedLanes.Add(lane);
        }

        // ── COIN ──────────────────────────────────────────────────────────────
        if (coinPool.Count > 0 && UnityEngine.Random.Range(0f, 100f) <= coinSpawnChance)
        {
            List<int> coinLanes = new List<int> { 0, 1, 2 };
            foreach (int l in usedLanes) coinLanes.Remove(l);

            if (coinLanes.Count > 0)
            {
                int lane = coinLanes[UnityEngine.Random.Range(0, coinLanes.Count)];
                PlaceObject(coinPool, lane, baseRotation, coinRotationOffset, pos, right, up, coinHeightOffset);
            }
        }
    }

    // ARTIK BOOL DEĞİL, GAMEOBJECT DÖNDÜRÜYOR (Efekt Ekleyebilmek İçin)
    GameObject PlaceObject(List<GameObject> pool, int lane, Quaternion baseRot, Vector3 rotOffset,
                     float3 splinePos, float3 right, float3 up, float heightOffset)
    {
        GameObject obj = GetPooled(pool);
        if (obj == null) return null;

        float xPos = (lane - 1) * laneDistance;
        Vector3 finalPos = (Vector3)splinePos + (Vector3)right * xPos + (Vector3)up * heightOffset;

        obj.transform.position = finalPos;
        obj.transform.rotation = baseRot * Quaternion.Euler(rotOffset);
        obj.SetActive(true);

        activeSpawns.Add(new ActiveObject { obj = obj });
        return obj;
    }

    GameObject GetPooled(List<GameObject> pool)
    {
        foreach (GameObject obj in pool)
            if (obj != null && !obj.activeInHierarchy) return obj;
        return null;
    }
}

// YARDIMCI YANIP SÖNME KODU
public class BlinkEffect : MonoBehaviour
{
    private float blinkDuration;
    private float timer;
    private MeshRenderer[] renderers;

    public void StartBlinking(float duration)
    {
        blinkDuration = duration;
        timer = 0;
        renderers = GetComponentsInChildren<MeshRenderer>();
    }

    void Update()
    {
        if (timer < blinkDuration)
        {
            timer += Time.deltaTime;
            bool isVisible = Mathf.PingPong(Time.time * 15f, 1f) > 0.5f;
            foreach(MeshRenderer mr in renderers) mr.enabled = isVisible;
            if (timer >= blinkDuration) foreach(MeshRenderer mr in renderers) mr.enabled = true;
        }
    }

    // DÜZELTİLEN KISIM BURASI
    void OnDisable()
    {
        if (renderers != null) 
        {
            foreach(MeshRenderer mr in renderers) mr.enabled = true;
        }
        
        // Obje ekrandan çıkıp havuza döndüğünde sayacı dolu gösteriyoruz.
        // Böylece ileride normal bir engel olarak doğduğunda yarım kalan yanıp sönmeye devam etmeyecek!
        timer = blinkDuration; 
    }
}