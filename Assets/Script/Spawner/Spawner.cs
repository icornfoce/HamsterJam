using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Spawner : MonoBehaviour
{
    [Header("Normal Enemy Settings")]
    public GameObject[] normalEnemyPrefabs;
    
    [Tooltip("ใส่ Plane หรือพื้นที่ ที่ต้องการให้มอนสเตอร์เกิดแบบสุ่ม (ใช้ร่วมกับ Collider)")]
    public Collider spawnAreaPlane; 
    
    [Tooltip("ปรับความสูงของการเกิด (ออฟเซ็ตจากค่า Y ของ Plane)")]
    public float spawnHeightOffset = 0f;
    
    [Tooltip("จุดเกิดแบบเจาะจง (จะใช้เมื่อไม่ได้ใส่ Spawn Area Plane)")]
    public Transform[] spawnPoints;
    
    public float spawnInterval = 2f;
    
    [Tooltip("จำนวนมอนสเตอร์สูงสุดที่สามารถอยู่ในฉากได้พร้อมกัน")]
    public int maxMonstersOnMap = 10;

    [Header("Boss Settings")]
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    
    [Tooltip("เวลา (นาที) ที่มอนสเตอร์ธรรมดาจะหยุดเกิด และรอให้ผู้เล่นเคลียร์มอนสเตอร์ให้หมดก่อนบอสจะเกิด")]
    public float timeToSpawnBossMinutes = 5f;

    [Header("UI Settings")]
    public GameObject winUI; // หน้า UI ที่จะขึ้นเมื่อล้มบอส
    public string startSceneName = "MainMenu"; // ชื่อ Scene เริ่มต้นที่จะกลับไป

    private float timer = 0f;
    private bool timeIsUp = false;
    private bool bossSpawned = false;
    private bool bossDefeated = false;
    private GameObject currentBoss;
    
    // เก็บรายการมอนสเตอร์ที่เกิดมาและยังไม่ตาย
    private List<GameObject> activeMonsters = new List<GameObject>();

    void Start()
    {
        // ซ่อนหน้า UI ไว้ก่อนตอนเริ่มเกม
        if (winUI != null)
        {
            winUI.SetActive(false);
        }
        
        // เริ่มการเกิดของมอนสเตอร์ธรรมดา
        StartCoroutine(SpawnNormalEnemies());
    }

    void Update()
    {
        if (!timeIsUp)
        {
            // นับเวลาจนกว่าจะถึงเวลาที่กำหนด
            timer += Time.deltaTime;
            if (timer >= timeToSpawnBossMinutes * 60f) // แปลงนาทีเป็นวินาที
            {
                timeIsUp = true; // หยุดเกิดมอนสเตอร์
            }
        }
        else if (timeIsUp && !bossSpawned)
        {
            // เมื่อเวลาหมด ให้เช็คว่ามอนสเตอร์บนฉากตายหมดหรือยัง
            // ทำความสะอาดลิสต์โดยลบ object ที่ตาย (ถูก Destroy) ไปแล้ว
            activeMonsters.RemoveAll(monster => monster == null);

            // ถ้าผู้เล่นกำจัดมอนสเตอร์บนฉากหมดแล้ว ให้บอสเกิด
            if (activeMonsters.Count == 0)
            {
                SpawnBoss();
            }
        }
        else if (bossSpawned && !bossDefeated)
        {
            // ถ้าบอสเกิดแล้ว และบอสถูกทำลาย (Destroy) ให้แสดง UI
            if (currentBoss == null)
            {
                BossDefeated();
            }
        }
    }

    IEnumerator SpawnNormalEnemies()
    {
        while (!timeIsUp)
        {
            // ทำความสะอาดลิสต์ ลบมอนสเตอร์ที่ตายไปแล้ว
            activeMonsters.RemoveAll(monster => monster == null);

            // ถ้าจำนวนมอนสเตอร์ในฉากยังไม่เกินขีดจำกัด
            if (activeMonsters.Count < maxMonstersOnMap)
            {
                // สุ่มเกิดมอนสเตอร์ธรรมดา
                if (normalEnemyPrefabs.Length > 0)
                {
                    GameObject prefab = normalEnemyPrefabs[Random.Range(0, normalEnemyPrefabs.Length)];
                    Vector3 spawnPos = GetRandomSpawnPosition();
                    
                    GameObject newMonster = Instantiate(prefab, spawnPos, Quaternion.identity);
                    activeMonsters.Add(newMonster);
                }
            }
            
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        // 1. ถ้ามี Plane (Collider) ให้สุ่มจุดในพื้นที่ Plane
        if (spawnAreaPlane != null)
        {
            Bounds bounds = spawnAreaPlane.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            float yPos = spawnAreaPlane.transform.position.y + spawnHeightOffset;
            return new Vector3(randomX, yPos, randomZ);
        }
        // 2. ถ้าไม่มี Plane ให้ใช้จุดเกิดจากอาเรย์ spawnPoints
        else if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return point.position;
        }
        
        // 3. ถ้าไม่มีทั้งคู่ ให้เกิดที่ตัว Spawner เลย
        return transform.position;
    }

    void SpawnBoss()
    {
        bossSpawned = true;

        if (bossPrefab != null && bossSpawnPoint != null)
        {
            // เสกบอส
            currentBoss = Instantiate(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
        }
        else if (bossPrefab != null)
        {
            // ถ้าไม่ได้กำหนดจุดเกิดบอส ให้เกิดตรงจุดสุ่ม
            currentBoss = Instantiate(bossPrefab, GetRandomSpawnPosition(), Quaternion.identity);
        }
    }

    // ฟังก์ชันนี้จะถูกเรียกเมื่อบอสตาย
    public void BossDefeated()
    {
        if (bossDefeated) return;
        bossDefeated = true;

        // แสดงหน้า UI
        if (winUI != null)
        {
            winUI.SetActive(true);
            // Time.timeScale = 0f; // เอาคอมเมนต์ออกถ้าต้องการหยุดเกม
        }
    }

    // ฟังก์ชันนี้เอาไว้ใส่ใน Event ของปุ่ม OnClick ในหน้า UI เพื่อทำการ Reset
    public void ResetGame()
    {
        // ย้อนเวลาของเกมกลับมาเป็นปกติ
        Time.timeScale = 1f;
        // โหลด Scene เริ่มต้น
        SceneManager.LoadScene(startSceneName);
    }
}
