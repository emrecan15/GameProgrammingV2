using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines; 
using Unity.Mathematics;   

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Havuz Ayarları (Engeller)")]
    public GameObject[] obstaclePrefabs;
    public int poolSizePerPrefab = 5;
    public Vector3 obstacleRotationOffset = new Vector3(0, 0, 0);
    private List<List<GameObject>> poolList;

    [Header("Havuz Ayarları (Altın)")]
    public GameObject coinPrefab;
    public int coinPoolSize = 30; // Sayı azaldığı için havuzu da küçülttük
    public Vector3 coinRotationOffset = new Vector3(0, 0, 0);
    private List<GameObject> coinPool;
    [Range(0f, 100f)] public float coinSpawnChance = 60f;

    [Header("Havuz Ayarları (Mıknatıs)")]
    public GameObject magnetPrefab;
    public int magnetPoolSize = 3;
    public Vector3 magnetRotationOffset = new Vector3(0, 0, 0);
    private List<GameObject> magnetPool;
    [Range(0f, 100f)] public float magnetSpawnChance = 5f;

    [Header("Havuz Ayarları (Nitro)")]
    public GameObject nitroPrefab;
    public int nitroPoolSize = 3;
    public Vector3 nitroRotationOffset = new Vector3(0, 0, 0);
    private List<GameObject> nitroPool;
    [Range(0f, 100f)] public float nitroSpawnChance = 5f;

    [Header("Havuz Ayarları (2x Altın)")]
    public GameObject doubleCoinPrefab; 
    public int doubleCoinPoolSize = 3;
    public Vector3 doubleCoinRotationOffset = new Vector3(0, 0, 0);
    private List<GameObject> doubleCoinPool;
    [Range(0f, 100f)] public float doubleCoinSpawnChance = 5f;

    [Header("Havuz Ayarları (Kalkan)")]
    public GameObject shieldPrefab; 
    public int shieldPoolSize = 3;
    public Vector3 shieldRotationOffset = new Vector3(0, 0, 0);
    private List<GameObject> shieldPool;
    [Range(0f, 100f)] public float shieldSpawnChance = 5f;

    [Header("Doğma Ayarları (Spline)")]
    public CarController playerCar;
    public float spawnDistanceAhead = 60f; 
    public float laneDistance = 3.5f;

    [Header("Zorluk Ayarları")]
    public float spawnDistanceInterval = 25f;
    public int minSpawnsBetweenPowerUps = 10; 
    private int currentPowerUpCooldown = 0;

    private float lastSpawnProgressDist;
    private SplineContainer spline;
    private float cachedSplineLength; 

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
            lastSpawnProgressDist = playerCar.progress * cachedSplineLength;
        }

        poolList = new List<List<GameObject>>();
        if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
        {
            for (int i = 0; i < obstaclePrefabs.Length; i++)
            {
                if (obstaclePrefabs[i] != null) poolList.Add(CreatePool(obstaclePrefabs[i], poolSizePerPrefab));
            }
        }

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
        float currentDist = playerCar.progress * cachedSplineLength;
        if (currentDist - lastSpawnProgressDist >= spawnDistanceInterval)
        {
            SpawnObstacleAndItems();
            lastSpawnProgressDist = currentDist;
        }
    }

    void SpawnObstacleAndItems()
    {
        float targetSpawnDist = (playerCar.progress * cachedSplineLength) + spawnDistanceAhead;
        float spawnProgress = (targetSpawnDist % cachedSplineLength) / cachedSplineLength;

        float3 pos, forward, up;
        spline.Evaluate(spawnProgress, out pos, out forward, out up);

        forward = math.normalize(forward);
        up = math.normalize(up);
        float3 right = math.cross(up, forward);

        Quaternion baseRotation = Quaternion.LookRotation(forward, up);
        List<int> availableLanes = new List<int> { 0, 1, 2 };

        bool shouldSpawnObstacle = !playerCar.isNitroActive || playerCar.isNitroEnding;

        if (poolList.Count > 0 && shouldSpawnObstacle)
        {
            int obstacleLane = UnityEngine.Random.Range(0, 3);
            availableLanes.Remove(obstacleLane); 
            int randomObstacleIndex = UnityEngine.Random.Range(0, poolList.Count);
            
            SpawnFromPool(poolList[randomObstacleIndex], obstacleLane, baseRotation, obstacleRotationOffset, pos, right);
        }

        // ARTIK SADECE TEK ALTIN ÇIKIYOR
        if (coinPool.Count > 0 && UnityEngine.Random.Range(0f, 100f) <= coinSpawnChance && availableLanes.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableLanes.Count);
            int coinLane = availableLanes[randomIndex];
            availableLanes.Remove(coinLane); 
            
            SpawnSingleCoin(coinLane, spawnProgress, baseRotation);
        }

        if (currentPowerUpCooldown > 0) currentPowerUpCooldown--;

        if (availableLanes.Count > 0 && currentPowerUpCooldown <= 0)
        {
            float powerUpRoll = UnityEngine.Random.Range(0f, 100f);
            
            if (magnetPool.Count > 0 && !playerCar.isMagnetActive && powerUpRoll <= magnetSpawnChance)
            {
                SpawnFromPool(magnetPool, availableLanes[0], baseRotation, magnetRotationOffset, pos, right);
                currentPowerUpCooldown = minSpawnsBetweenPowerUps;
            }
            else if (nitroPool.Count > 0 && !playerCar.isNitroActive && powerUpRoll <= (magnetSpawnChance + nitroSpawnChance))
            {
                SpawnFromPool(nitroPool, availableLanes[0], baseRotation, nitroRotationOffset, pos, right);
                currentPowerUpCooldown = minSpawnsBetweenPowerUps;
            }
            else if (doubleCoinPool.Count > 0 && !playerCar.isDoubleCoinActive && powerUpRoll <= (magnetSpawnChance + nitroSpawnChance + doubleCoinSpawnChance))
            {
                SpawnFromPool(doubleCoinPool, availableLanes[0], baseRotation, doubleCoinRotationOffset, pos, right);
                currentPowerUpCooldown = minSpawnsBetweenPowerUps;
            }
            else if (shieldPool.Count > 0 && !playerCar.isShieldActive && powerUpRoll <= (magnetSpawnChance + nitroSpawnChance + doubleCoinSpawnChance + shieldSpawnChance))
            {
                SpawnFromPool(shieldPool, availableLanes[0], baseRotation, shieldRotationOffset, pos, right);
                currentPowerUpCooldown = minSpawnsBetweenPowerUps;
            }
        }
    }

    // DESENLERİ SİLDİK, YERİNE TEKLİ DOĞMA GELDİ
    void SpawnSingleCoin(int lane, float progress, Quaternion baseRot)
    {
        GameObject coin = GetPooledCoin();
        if (coin == null) return;

        float3 cPos, cForward, cUp;
        spline.Evaluate(progress, out cPos, out cForward, out cUp);
        float3 cRight = math.cross(math.normalize(cUp), math.normalize(cForward));

        float xPos = (lane - 1) * laneDistance;
        float yOffset = coinPrefab.transform.position.y;
        if (yOffset < 0.5f) yOffset = 0.5f;

        Vector3 finalPos = (Vector3)cPos + ((Vector3)cRight * xPos);
        finalPos.y += yOffset;

        coin.transform.position = finalPos;
        coin.transform.rotation = baseRot * Quaternion.Euler(coinRotationOffset);
        coin.SetActive(true);
    }

    GameObject GetPooledCoin()
    {
        foreach (GameObject obj in coinPool) if (!obj.activeInHierarchy) return obj;
        return null;
    }

    GameObject SpawnFromPool(List<GameObject> pool, int lane, Quaternion baseRot, Vector3 rotOffset, float3 pos, float3 right)
    {
        if (pool == null || pool.Count == 0) return null;
        foreach (GameObject obj in pool)
        {
            if (!obj.activeInHierarchy)
            {
                float xPos = (lane - 1) * laneDistance;
                float originalY = obj.transform.position.y;
                Vector3 finalPos = (Vector3)pos + ((Vector3)right * xPos);
                finalPos.y += originalY; 
                obj.transform.position = finalPos;
                obj.transform.rotation = baseRot * Quaternion.Euler(rotOffset);
                obj.SetActive(true);
                return obj; 
            }
        }
        return null;
    }
}