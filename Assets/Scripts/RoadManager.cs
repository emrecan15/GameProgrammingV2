using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    [Header("Normal Roads")]
    public GameObject[] roadPrefabs;

    [Header("Bridges (Özel Yollar)")]
    public GameObject[] bridgePrefabs; // Köprüleri buraya koyacağız
    [Range(0f, 100f)] public float bridgeChance = 15f; // Köprü çıkma ihtimali

    [Header("Tunnel Sequence")]
    public GameObject tunnelStartPrefab;
    public GameObject tunnelMiddlePrefab;
    public GameObject tunnelEndPrefab;
    [Range(0f, 100f)] public float tunnelChance = 15f; 
    public int tunnelMiddleCount = 3; 

    [Header("Pacing (Spam Engelleme)")]
    public int minRoadsBetweenSpecials = 5; // İki özel yol (tünel/köprü) arasına girecek MİNİMUM normal yol sayısı

    [Header("General Settings")]
    public Transform playerTransform;
    public float spawnZ = 0.0f;
    public float roadLength = 30f;
    public int amountOfRoadsOnScreen = 5;

    private List<GameObject> activeRoads = new List<GameObject>();

    private bool isSpawningTunnel = false;
    private int spawnedMiddleCount = 0;
    private int normalRoadsSinceLastSpecial = 0; 

    void Start()
    {
        for (int i = 0; i < amountOfRoadsOnScreen; i++)
        {
            // First roads guarante will be normal road not a tunnel or bridge
            GameObject go = Instantiate(roadPrefabs[0], transform.forward * spawnZ, transform.rotation);
            activeRoads.Add(go);
            spawnZ += roadLength;
            normalRoadsSinceLastSpecial++; 
        }
    }

    void Update()
    {
        if (playerTransform.position.z - roadLength > (spawnZ - amountOfRoadsOnScreen * roadLength))
        {
            SpawnRoad();
            DeleteRoad();
        }
    }

    private void SpawnRoad()
    {
        GameObject roadToSpawn;

        // tunnel spawn
        if (isSpawningTunnel)
        {
            if (spawnedMiddleCount < tunnelMiddleCount)
            {
                roadToSpawn = tunnelMiddlePrefab;
                spawnedMiddleCount++;
            }
            else
            {
                roadToSpawn = tunnelEndPrefab;
                isSpawningTunnel = false;
                normalRoadsSinceLastSpecial = 0; 
            }
        }
        else
        {
            // check cooldawn for spawning tunnel
            if (normalRoadsSinceLastSpecial >= minRoadsBetweenSpecials)
            {
                float randomValue = Random.Range(0f, 100f);

                
                if (randomValue <= tunnelChance)
                {
                    roadToSpawn = tunnelStartPrefab;
                    isSpawningTunnel = true;
                    spawnedMiddleCount = 0;
                }
                
                else if (randomValue <= (tunnelChance + bridgeChance))
                {
                    int bIndex = Random.Range(0, bridgePrefabs.Length);
                    roadToSpawn = bridgePrefabs[bIndex];
                    normalRoadsSinceLastSpecial = 0; 
                }
                
                else
                {
                    int rIndex = Random.Range(0, roadPrefabs.Length);
                    roadToSpawn = roadPrefabs[rIndex];
                    normalRoadsSinceLastSpecial++; 
                }
            }
            else
            {
                int rIndex = Random.Range(0, roadPrefabs.Length);
                roadToSpawn = roadPrefabs[rIndex];
                normalRoadsSinceLastSpecial++;
            }
        }

        GameObject go = Instantiate(roadToSpawn, transform.forward * spawnZ, transform.rotation);
        activeRoads.Add(go);
        spawnZ += roadLength;
    }

    private void DeleteRoad()
    {
        Destroy(activeRoads[0]);
        activeRoads.RemoveAt(0);
    }
}