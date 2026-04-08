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

	[Header("Özel Yollar (Tünel ve Köprü)")]
	public GameObject tunnelStartPrefab;
	public GameObject tunnelMiddlePrefab;
	public GameObject tunnelEndPrefab;
	public GameObject[] bridgePrefabs; // Değişti: Artık birden fazla köprü koyabilirsin

	[Header("Çıkma İhtimalleri")]
	[Range(0f, 1f)] public float tunnelChance = 0.02f; // %2 ihtimalle tünel başlar (Nadir)
	[Range(0f, 1f)] public float bridgeChance = 0.03f; // %3 ihtimalle köprü çıkar (Nadir)
	public int minNormalRoadsBetweenSpecials = 15; // İki özel yol arasında en az kaç normal yol olmalı

	// Nesne Havuzlama: Artık numaralarla değil, direkt Prefab dosyasıyla objeleri eşleştiriyoruz
	private Dictionary<GameObject, List<GameObject>> prefabPools = new Dictionary<GameObject, List<GameObject>>();
	private List<GameObject> activeRoads = new List<GameObject>();

	private float spawnDistance = 0f;
	private float carTotalDistance = 0f;
	private float cachedSplineLength;

	// Tünel sırasını takip etmek için basit bir durum (state) yapısı
	// 0: Tünel yok, 1: Orta parça lazım, 2: Son parça lazım
	private int tunnelState = 0;
	private int normalRoadCount = 15; // Oyun başlar başlamaz çıkabilmesi için limiti doldurmuş gibi başlatıyoruz

	void Start()
	{
		if (trackSpline != null)
		{
			cachedSplineLength = trackSpline.CalculateLength();
		}

		InitializePools();

		for (int i = 0; i < amountOfRoadsOnScreen; i++)
		{
			SpawnRoad();
		}
	}

	void Update()
	{
		if (playerCar == null || trackSpline == null) return;

		carTotalDistance += playerCar.forwardSpeed * Time.deltaTime;

		if (carTotalDistance - roadLength > (spawnDistance - amountOfRoadsOnScreen * roadLength))
		{
			SpawnRoad();
			RecycleRoad();
		}
	}

	private void InitializePools()
	{
		// Tüm normal yolları havuza ekle
		foreach (GameObject prefab in roadPrefabs)
		{
			CreatePoolForPrefab(prefab, amountOfRoadsOnScreen);
		}

		// Özel yolları havuza ekle (Ekranda aynı anda çok fazla olmayacakları için azar azar üretiyoruz)
		CreatePoolForPrefab(tunnelStartPrefab, 2);
		CreatePoolForPrefab(tunnelMiddlePrefab, 2);
		CreatePoolForPrefab(tunnelEndPrefab, 2);

		// Eklenen tüm köprü çeşitlerini havuza ekle
		foreach (GameObject bridge in bridgePrefabs)
		{
			CreatePoolForPrefab(bridge, 2);
		}
	}

	private void CreatePoolForPrefab(GameObject prefab, int count)
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

	private void SpawnRoad()
	{
		GameObject prefabToSpawn = null;

		// --- HANGİ YOLUN ÇIKACAĞINA KARAR VERME KISMI ---
		if (tunnelState == 1)
		{
			// Başlangıç çıkmıştı, sıradaki kesinlikle orta parça olmalı
			prefabToSpawn = tunnelMiddlePrefab;
			tunnelState = 2; // Bir sonraki tur için "son parça lazım" dedik
		}
		else if (tunnelState == 2)
		{
			// Orta parça çıkmıştı, şimdi bitişi koy
			prefabToSpawn = tunnelEndPrefab;
			tunnelState = 0; // Tünel bitti, normal yollara dönüyoruz
			normalRoadCount = 0; // Tünel bittikten sonra sayacı sıfırla ki hemen peşine köprü çıkmasın
		}
		else
		{
			// Normal durumdayız, ihtimallere göre zar atıyoruz
			float randomValue = UnityEngine.Random.value; // 0.0 ile 1.0 arası rastgele sayı

			// Eğer yeterince normal yol geçtiyse özel yol (tünel/köprü) çıkmasına izin ver
			if (normalRoadCount >= minNormalRoadsBetweenSpecials)
			{
				if (randomValue < tunnelChance)
				{
					// Tünel başlat
					prefabToSpawn = tunnelStartPrefab;
					tunnelState = 1; // Bir sonraki turda orta parçayı koyması için işaretledik
				}
				else if (randomValue < tunnelChance + bridgeChance)
				{
					// Köprü koy (Listeden rastgele bir köprü seç)
					if (bridgePrefabs != null && bridgePrefabs.Length > 0)
					{
						int randomBridgeIndex = UnityEngine.Random.Range(0, bridgePrefabs.Length);
						prefabToSpawn = bridgePrefabs[randomBridgeIndex];
						normalRoadCount = 0; // Köprüden sonra sayacı sıfırla ki hemen yeni bir şey çıkmasın
					}
				}
			}

			// Zarlar tutmadıysa veya henüz özel yol çıkma limitine ulaşılmadıysa normal yol koy
			if (prefabToSpawn == null)
			{
				int randomIndex = UnityEngine.Random.Range(0, roadPrefabs.Length);
				prefabToSpawn = roadPrefabs[randomIndex];
				normalRoadCount++; // Normal yol sayacını artır
			}
		}

		// Havuzdan objeyi çek
		GameObject spawnedObject = GetFromPool(prefabToSpawn);

		if (spawnedObject == null) return;

		// Spline üzerindeki pozisyonunu hesapla
		float spawnProgress = (spawnDistance % cachedSplineLength) / cachedSplineLength;
		float3 pos, forward, up;
		trackSpline.Evaluate(spawnProgress, out pos, out forward, out up);

		forward = math.normalize(forward);
		up = math.normalize(up);
		Quaternion rotation = Quaternion.LookRotation(forward, up);

		// Yerleştir ve aktif et
		spawnedObject.transform.position = (Vector3)pos;
		spawnedObject.transform.rotation = rotation;
		spawnedObject.SetActive(true);

		activeRoads.Add(spawnedObject);
		spawnDistance += roadLength;
	}

	private GameObject GetFromPool(GameObject prefab)
	{
		if (prefab == null || !prefabPools.ContainsKey(prefab)) return null;

		// Havuzda boşta duran (görünmez) var mı bak
		foreach (GameObject obj in prefabPools[prefab])
		{
			if (!obj.activeInHierarchy)
			{
				return obj;
			}
		}

		// Havuzda boş obje kalmadıysa (nadir durum), yeni bir tane üretip havuza ekle
		GameObject newObj = Instantiate(prefab);
		prefabPools[prefab].Add(newObj);
		return newObj;
	}

	private void RecycleRoad()
	{
		if (activeRoads.Count > 0)
		{
			// En arkada kalan yolu silme, sadece görünmez yap (Havuza geri döner)
			GameObject roadToRecycle = activeRoads[0];
			roadToRecycle.SetActive(false);
			activeRoads.RemoveAt(0);
		}
	}
}