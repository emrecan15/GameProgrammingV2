using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class RoadManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    public SplineContainer trackSpline;
    public CarController playerCar;

    [Header("Yol Ayarları")]
    public GameObject[] roadPrefabs;
    public float roadLength = 26f;
    public int amountOfRoadsOnScreen = 7;

    // Nesne Havuzlama (Object Pooling) için listeler
    private List<GameObject> activeRoads = new List<GameObject>();
    private Dictionary<int, List<GameObject>> roadPools = new Dictionary<int, List<GameObject>>();

    private float spawnDistance = 0f;
    private float carTotalDistance = 0f;
    private float cachedSplineLength;

    void Start()
    {
        if (trackSpline != null)
        {
            cachedSplineLength = trackSpline.CalculateLength();
        }

        // Havuzları önceden hazırlayarak oyun içindeki yükü azaltıyoruz
        InitializePools();

        // İlk yolları havuzdan çekerek oluştur
        for (int i = 0; i < amountOfRoadsOnScreen; i++)
        {
            SpawnRoad();
        }
    }

    void Update()
    {
        if (playerCar == null || trackSpline == null) return;

        // Mesafeyi takip et
        carTotalDistance += playerCar.forwardSpeed * Time.deltaTime;

        // Yeni yol ekleme zamanı gelmiş mi?
        if (carTotalDistance - roadLength > (spawnDistance - amountOfRoadsOnScreen * roadLength))
        {
            SpawnRoad();
            RecycleRoad(); // Destroy yerine Recycle (Geri dönüşüm) kullanıyoruz
        }
    }

    private void InitializePools()
    {
        for (int i = 0; i < roadPrefabs.Length; i++)
        {
            roadPools[i] = new List<GameObject>();

            // Her prefab tipi için başlangıçta birkaç tane oluşturup gizle
            for (int j = 0; j < amountOfRoadsOnScreen; j++)
            {
                GameObject obj = Instantiate(roadPrefabs[i]);
                obj.SetActive(false);
                roadPools[i].Add(obj);
            }
        }
    }

    private void SpawnRoad()
    {
        int randomIndex = UnityEngine.Random.Range(0, roadPrefabs.Length);
        GameObject roadToSpawn = GetRoadFromPool(randomIndex);

        float spawnProgress = (spawnDistance % cachedSplineLength) / cachedSplineLength;

        float3 pos, forward, up;
        trackSpline.Evaluate(spawnProgress, out pos, out forward, out up);

        // Vektör temizliği
        forward = math.normalize(forward);
        up = math.normalize(up);
        Quaternion rotation = Quaternion.LookRotation(forward, up);

        // Objeyi yerleştir ve aktif et
        roadToSpawn.transform.position = (Vector3)pos;
        roadToSpawn.transform.rotation = rotation;
        roadToSpawn.SetActive(true);

        activeRoads.Add(roadToSpawn);
        spawnDistance += roadLength;
    }

    private GameObject GetRoadFromPool(int index)
    {
        // Havuzda boşta duran (inaktif) bir obje var mı?
        foreach (GameObject obj in roadPools[index])
        {
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
        }

        // Havuz yetmediyse (nadir bir durum), yeni bir tane oluşturup havuza ekle
        GameObject newObj = Instantiate(roadPrefabs[index]);
        roadPools[index].Add(newObj);
        return newObj;
    }

    private void RecycleRoad()
    {
        if (activeRoads.Count > 0)
        {
            // En arkadaki yolu silme, sadece görünmez yap (Havuza geri döner)
            GameObject roadToRecycle = activeRoads[0];
            roadToRecycle.SetActive(false);
            activeRoads.RemoveAt(0);
        }
    }
}