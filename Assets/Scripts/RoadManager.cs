using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class RoadManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    public SplineContainer trackSpline;
    public CarController playerCar;

    [Header("Normal Yol Ayarları")]
    public GameObject[] roadPrefabs;
    public float roadLength = 26f;
    public int amountOfRoadsOnScreen = 7;

    [Header("Özel Yollar (Tünel & Köprü)")]
    public GameObject tunnelStartPrefab;
    public GameObject tunnelMiddlePrefab;
    public GameObject tunnelEndPrefab;
    public GameObject[] bridgePrefabs;

    [Header("Özel Yol İhtimalleri")]
    [Range(0f, 1f)] public float tunnelChance = 0.02f;
    [Range(0f, 1f)] public float bridgeChance = 0.03f;
    // İki özel yol arasında en az bu kadar normal yol geçmeli
    public int minNormalRoadsBetweenSpecials = 15;

    private Dictionary<GameObject, List<GameObject>> prefabPools = new Dictionary<GameObject, List<GameObject>>();
    private List<GameObject> activeRoads = new List<GameObject>();

    private float spawnDistance;
    private float carTotalDistance;
    private float cachedSplineLength;

    // Tünel üç parçadan oluşuyor: Start → Middle → End
    // tunnelState: 0=bekleme, 1=middle gelecek, 2=end gelecek
    private int tunnelState;
    // Oyun başında zaten yeterince sayılmış gibi başlatıyoruz ki ilk yollarda özel yol çıkabilsin
    private int normalRoadCount = 15;

    void Start()
    {
        if (trackSpline != null)
            cachedSplineLength = trackSpline.CalculateLength();

        InitializePools();

        // Tüm başlangıç yollarını tek seferde, aynı karede spawn et.
        // Böylece oyun açılırken yollar tek tek belirmez.
        for (int i = 0; i < amountOfRoadsOnScreen; i++)
            SpawnRoad();
    }

    void Update()
    {
        if (playerCar == null || trackSpline == null) return;

        carTotalDistance += playerCar.forwardSpeed * Time.deltaTime;

        // Araç yeterince ilerlediğinde yeni yol ekle ve en arkadakini havuza iade et
        float spawnTrigger = spawnDistance - amountOfRoadsOnScreen * roadLength;
        if (carTotalDistance - roadLength > spawnTrigger)
        {
            SpawnRoad();
            RecycleRoad();
        }
    }

    void InitializePools()
    {
        foreach (GameObject prefab in roadPrefabs)
            CreatePoolForPrefab(prefab, amountOfRoadsOnScreen);

        CreatePoolForPrefab(tunnelStartPrefab, 2);
        CreatePoolForPrefab(tunnelMiddlePrefab, 2);
        CreatePoolForPrefab(tunnelEndPrefab, 2);

        if (bridgePrefabs != null)
            foreach (GameObject bridge in bridgePrefabs)
                CreatePoolForPrefab(bridge, 2);
    }

    void CreatePoolForPrefab(GameObject prefab, int count)
    {
        if (prefab == null) return;
        prefabPools[prefab] = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            prefabPools[prefab].Add(obj);
        }
    }

    void SpawnRoad()
    {
        GameObject prefabToSpawn = ChooseNextRoadPrefab();
        if (prefabToSpawn == null) return;

        GameObject spawnedObj = GetFromPool(prefabToSpawn);
        if (spawnedObj == null) return;

        float spawnProgress = (spawnDistance % cachedSplineLength) / cachedSplineLength;
        trackSpline.Evaluate(spawnProgress, out float3 pos, out float3 forward, out float3 up);

        forward = math.normalize(forward);
        up = math.normalize(up);

        spawnedObj.transform.position = (Vector3)pos;
        spawnedObj.transform.rotation = Quaternion.LookRotation(forward, up);
        spawnedObj.SetActive(true);

        activeRoads.Add(spawnedObj);
        spawnDistance += roadLength;
    }

    GameObject ChooseNextRoadPrefab()
    {
        // Tünel sırası devam ediyorsa kesinlikle sıradaki parçayı koy
        if (tunnelState == 1)
        {
            tunnelState = 2;
            return tunnelMiddlePrefab;
        }
        if (tunnelState == 2)
        {
            tunnelState = 0;
            normalRoadCount = 0;
            return tunnelEndPrefab;
        }

        // Yeterli normal yol geçtiyse özel yol zarı at
        if (normalRoadCount >= minNormalRoadsBetweenSpecials)
        {
            float roll = UnityEngine.Random.value;

            if (roll < tunnelChance && tunnelStartPrefab != null)
            {
                tunnelState = 1;
                return tunnelStartPrefab;
            }

            if (roll < tunnelChance + bridgeChance && bridgePrefabs != null && bridgePrefabs.Length > 0)
            {
                normalRoadCount = 0;
                return bridgePrefabs[UnityEngine.Random.Range(0, bridgePrefabs.Length)];
            }
        }

        // Normal yol
        normalRoadCount++;
        return roadPrefabs[UnityEngine.Random.Range(0, roadPrefabs.Length)];
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (prefab == null || !prefabPools.ContainsKey(prefab)) return null;

        foreach (GameObject obj in prefabPools[prefab])
            if (!obj.activeInHierarchy) return obj;

        // Havuzda boş kalmadıysa yeni üret ve havuza ekle (nadir durum)
        GameObject newObj = Instantiate(prefab);
        prefabPools[prefab].Add(newObj);
        return newObj;
    }

    void RecycleRoad()
    {
        if (activeRoads.Count == 0) return;
        activeRoads[0].SetActive(false);
        activeRoads.RemoveAt(0);
    }
}