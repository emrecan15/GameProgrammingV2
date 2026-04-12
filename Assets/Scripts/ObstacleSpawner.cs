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
    public int coinPoolSize = 10;
    public Vector3 coinRotationOffset = new Vector3(0, 0, 0);
    private List<GameObject> coinPool;
    [Range(0f, 100f)]
    public float coinSpawnChance = 60f;

    [Header("Havuz Ayarları (Mıknatıs)")]
    public GameObject magnetPrefab;
    public int magnetPoolSize = 3;
    public Vector3 magnetRotationOffset = new Vector3(0, 0, 0);
    private List<GameObject> magnetPool;
    [Range(0f, 100f)] 
    public float magnetSpawnChance = 5f;

    [Header("Doğma Ayarları (Spline)")]
    public CarController playerCar;
    public float spawnDistanceAhead = 60f; 
    public float laneDistance = 3.5f;

    [Header("Zorluk Ayarları")]
    public float spawnDistanceInterval = 25f;
    
    // --- YENİ EKLENEN COOLDOWN AYARLARI ---
    public int minSpawnsBetweenMagnets = 8; // İki mıknatıs arasında en az kaç kere engel doğmalı?
    private int currentMagnetCooldown = 0;

    private float lastSpawnProgressDist;
    private SplineContainer spline;
    private float cachedSplineLength; 

    void Start()
    {
        if (playerCar != null)
        {
            spline = playerCar.trackSpline;
            if (spline != null)
            {
                cachedSplineLength = spline.CalculateLength();
            }
        }

        // Engel havuzunu oluştur
        poolList = new List<List<GameObject>>();
        for (int i = 0; i < obstaclePrefabs.Length; i++)
        {
            List<GameObject> objectPool = new List<GameObject>();
            for (int j = 0; j < poolSizePerPrefab; j++)
            {
                GameObject obj = Instantiate(obstaclePrefabs[i]);
                obj.SetActive(false);
                objectPool.Add(obj);
            }
            poolList.Add(objectPool);
        }

        // Altın havuzunu oluştur
        coinPool = new List<GameObject>();
        for (int i = 0; i < coinPoolSize; i++)
        {
            GameObject coin = Instantiate(coinPrefab);
            coin.SetActive(false);
            coinPool.Add(coin);
        }

        // Mıknatıs havuzunu oluştur
        magnetPool = new List<GameObject>();
        for (int i = 0; i < magnetPoolSize; i++)
        {
            GameObject magnet = Instantiate(magnetPrefab);
            magnet.SetActive(false);
            magnetPool.Add(magnet);
        }

        if (playerCar != null && spline != null)
        {
            lastSpawnProgressDist = playerCar.progress * cachedSplineLength;
        }
    }

    void Update()
    {
        if (spline == null || playerCar == null) return;

        float currentDist = playerCar.progress * cachedSplineLength;

        if (currentDist - lastSpawnProgressDist >= spawnDistanceInterval)
        {
            SpawnObstacleAndCoin();
            lastSpawnProgressDist = currentDist;
        }
    }

    void SpawnObstacleAndCoin()
    {
        float targetSpawnDist = (playerCar.progress * cachedSplineLength) + spawnDistanceAhead;
        float spawnProgress = (targetSpawnDist % cachedSplineLength) / cachedSplineLength;

        float3 pos, forward, up;
        spline.Evaluate(spawnProgress, out pos, out forward, out up);

        forward = math.normalize(forward);
        up = math.normalize(up);
        float3 right = math.cross(up, forward);

        Quaternion baseRotation = Quaternion.LookRotation(forward, up);

        // --- 1. ENGEL SPAWN ---
        int randomObstacleIndex = UnityEngine.Random.Range(0, obstaclePrefabs.Length);
        GameObject obstacle = GetPooledObstacle(randomObstacleIndex);
        int obstacleLane = UnityEngine.Random.Range(0, 3); 

        if (obstacle != null)
        {
            float xPos = (obstacleLane - 1) * laneDistance;
            float originalY = obstaclePrefabs[randomObstacleIndex].transform.position.y;

            Vector3 finalPos = (Vector3)pos + ((Vector3)right * xPos);
            finalPos.y += originalY;

            obstacle.transform.position = finalPos;
            obstacle.transform.rotation = baseRotation * Quaternion.Euler(obstacleRotationOffset);
            obstacle.SetActive(true);
        }

        // --- 2. ALTIN SPAWN ---
        int coinLane = -1; 
        bool isCoinSpawned = false; 

        if (UnityEngine.Random.Range(0f, 100f) <= coinSpawnChance)
        {
            GameObject coin = GetPooledCoin();
            if (coin != null)
            {
                do { coinLane = UnityEngine.Random.Range(0, 3); } while (coinLane == obstacleLane);
                isCoinSpawned = true;

                float coinXPos = (coinLane - 1) * laneDistance;
                float coinY = coinPrefab.transform.position.y;

                Vector3 coinFinalPos = (Vector3)pos + ((Vector3)right * coinXPos);
                coinFinalPos.y += coinY;

                coin.transform.position = coinFinalPos;
                coin.transform.rotation = baseRotation * Quaternion.Euler(coinRotationOffset);
                coin.SetActive(true);
            }
        }

        // --- 3. MIKNATIS SPAWN (COOLDOWN EKLENDİ) ---
        if (currentMagnetCooldown > 0) currentMagnetCooldown--;

        // Eğer mıknatıs şu an aktif değilse VE bekleme süresi bittiyse zarı at!
        if (currentMagnetCooldown <= 0 && !playerCar.isMagnetActive && UnityEngine.Random.Range(0f, 100f) <= magnetSpawnChance)
        {
            GameObject magnetObj = GetPooledMagnet();
            if (magnetObj != null)
            {
                int magnetLane;
                do 
                { 
                    magnetLane = UnityEngine.Random.Range(0, 3); 
                } 
                while (magnetLane == obstacleLane || (isCoinSpawned && magnetLane == coinLane));

                float magnetXPos = (magnetLane - 1) * laneDistance;
                float magnetY = magnetPrefab.transform.position.y;

                Vector3 magnetFinalPos = (Vector3)pos + ((Vector3)right * magnetXPos);
                magnetFinalPos.y += magnetY;

                magnetObj.transform.position = magnetFinalPos;
                magnetObj.transform.rotation = baseRotation * Quaternion.Euler(magnetRotationOffset);
                magnetObj.SetActive(true);
                
                // Mıknatısı koyduk, sayacı tekrar başlat ki arka arkaya çıkmasın
                currentMagnetCooldown = minSpawnsBetweenMagnets; 
            }
        }
    }

    GameObject GetPooledObstacle(int index)
    {
        for (int i = 0; i < poolList[index].Count; i++)
        {
            if (!poolList[index][i].activeInHierarchy) return poolList[index][i];
        }
        GameObject newObj = Instantiate(obstaclePrefabs[index]);
        newObj.SetActive(false);
        poolList[index].Add(newObj);
        return newObj;
    }

    GameObject GetPooledCoin()
    {
        for (int i = 0; i < coinPool.Count; i++)
        {
            if (!coinPool[i].activeInHierarchy) return coinPool[i];
        }
        GameObject newCoin = Instantiate(coinPrefab);
        newCoin.SetActive(false);
        coinPool.Add(newCoin);
        return newCoin;
    }

    GameObject GetPooledMagnet()
    {
        for (int i = 0; i < magnetPool.Count; i++)
        {
            if (!magnetPool[i].activeInHierarchy) return magnetPool[i];
        }
        GameObject newMagnet = Instantiate(magnetPrefab);
        newMagnet.SetActive(false);
        magnetPool.Add(newMagnet);
        return newMagnet;
    }
}