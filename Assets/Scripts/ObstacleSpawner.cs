using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines; // Spline paketi þart
using Unity.Mathematics;   // Matematik kütüphanesi þart

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Havuz Ayarlarý (Engeller)")]
    public GameObject[] obstaclePrefabs;
    public int poolSizePerPrefab = 5;
    // Eðer modeller yan duruyorsa buradan (0, 90, 0) gibi düzeltebilirsin
    public Vector3 obstacleRotationOffset = new Vector3(0, 0, 0);
    private List<List<GameObject>> poolList;

    [Header("Havuz Ayarlarý (Altýn)")]
    public GameObject coinPrefab;
    public int coinPoolSize = 10;
    public Vector3 coinRotationOffset = new Vector3(0, 0, 0);
    private List<GameObject> coinPool;
    [Range(0f, 100f)]
    public float coinSpawnChance = 60f;

    [Header("Doðma Ayarlarý (Spline)")]
    public CarController playerCar;
    public float spawnDistanceAhead = 60f; // Arabadan ne kadar önde doðsun?
    public float laneDistance = 3.5f;

    [Header("Zorluk Ayarlarý")]
    public float spawnDistanceInterval = 25f;

    private float lastSpawnProgressDist;
    private SplineContainer spline;
    private float cachedSplineLength; // Performans için uzunluðu saklayacaðýz

    void Start()
    {
        if (playerCar != null)
        {
            spline = playerCar.trackSpline;
            if (spline != null)
            {
                // Uzunluðu bir kez hesaplayýp kaydediyoruz (Update içinde çaðýrmýyoruz)
                cachedSplineLength = spline.CalculateLength();
            }
        }

        // Engel havuzunu oluþtur
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

        // Altýn havuzunu oluþtur
        coinPool = new List<GameObject>();
        for (int i = 0; i < coinPoolSize; i++)
        {
            GameObject coin = Instantiate(coinPrefab);
            coin.SetActive(false);
            coinPool.Add(coin);
        }

        if (playerCar != null && spline != null)
        {
            lastSpawnProgressDist = playerCar.progress * cachedSplineLength;
        }
    }

    void Update()
    {
        if (spline == null || playerCar == null) return;

        // Arabanýn spline üzerindeki toplam kat ettiði mesafe (Cache üzerinden hesap)
        float currentDist = playerCar.progress * cachedSplineLength;

        if (currentDist - lastSpawnProgressDist >= spawnDistanceInterval)
        {
            SpawnObstacleAndCoin();
            lastSpawnProgressDist = currentDist;
        }
    }

    void SpawnObstacleAndCoin()
    {
        // Doðma noktasýný hesapla
        float targetSpawnDist = (playerCar.progress * cachedSplineLength) + spawnDistanceAhead;
        float spawnProgress = (targetSpawnDist % cachedSplineLength) / cachedSplineLength;

        // Spline verilerini al
        float3 pos, forward, up;
        spline.Evaluate(spawnProgress, out pos, out forward, out up);

        // Normalize et
        forward = math.normalize(forward);
        up = math.normalize(up);
        float3 right = math.cross(up, forward);

        // Temel yol rotasyonu
        Quaternion baseRotation = Quaternion.LookRotation(forward, up);

        // --- ENGEL SPAWN ---
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
            // Yol rotasyonu + Senin verdiðin ofset
            obstacle.transform.rotation = baseRotation * Quaternion.Euler(obstacleRotationOffset);
            obstacle.SetActive(true);
        }

        // --- ALTIN SPAWN ---
        if (UnityEngine.Random.Range(0f, 100f) <= coinSpawnChance)
        {
            GameObject coin = GetPooledCoin();
            if (coin != null)
            {
                int coinLane;
                do { coinLane = UnityEngine.Random.Range(0, 3); } while (coinLane == obstacleLane);

                float coinXPos = (coinLane - 1) * laneDistance;
                float coinY = coinPrefab.transform.position.y;

                Vector3 coinFinalPos = (Vector3)pos + ((Vector3)right * coinXPos);
                coinFinalPos.y += coinY;

                coin.transform.position = coinFinalPos;
                // Altýn rotasyonu + Senin verdiðin ofset
                coin.transform.rotation = baseRotation * Quaternion.Euler(coinRotationOffset);
                coin.SetActive(true);
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
}