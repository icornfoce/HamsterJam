using UnityEngine;
using TMPro;

public class TypingSystem : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private GameObject typingUI; 
    [SerializeField] private TMP_InputField inputField;

    [Header("Matched Items Slots")]
    [SerializeField] private ItemInfo firstItem;
    [SerializeField] private ItemInfo secondItem;

    [Header("Settings")]
    [Range(0.1f, 1f)] [SerializeField] private float slowTimeScale = 0.2f;
    private bool isSlowed = false;

    [Header("Audio Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSFX;
    [SerializeField] private AudioClip matchSFX;
    [SerializeField] private AudioClip combineSFX;
    [SerializeField] private AudioClip releaseSFX;
    [SerializeField] private AudioClip errorSFX;

    [Header("Visual Effects (VFX)")]
    [SerializeField] private GameObject matchVFXPrefab;
    [SerializeField] private GameObject combineVFXPrefab;
    [SerializeField] private GameObject releaseVFXPrefab;
    [SerializeField] private Transform vfxSpawnPoint; // Where to spawn player-centered VFX

    [Header("Item Spawning Settings")]
    [SerializeField] private Transform playerTransform; // ลาก Player มาใส่ตรงนี้ (ถ้าไม่ใส่จะหา Tag "Player" อัตโนมัติ)
    [SerializeField] private Vector3 firstSlotOffset = new Vector3(-0.6f, 1.8f, -1.0f);
    [SerializeField] private Vector3 secondSlotOffset = new Vector3(0.6f, 1.8f, -1.0f);
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.1f;
    [SerializeField] private float followSpeed = 5f;
    [Range(0f, 1f)] [SerializeField] private float itemAlpha = 0.5f; // ความโปร่งใสของไอเทมตอนที่ยังไม่ปล่อย
    
    private GameObject spawnedFirst;
    private GameObject spawnedSecond;

    // Properties to access the current main item
    public string ItemName => firstItem?.itemName ?? string.Empty;
    public GameObject ItemPrefab => firstItem?.itemPrefab;
    public Vector3 ItemSize => firstItem?.itemSize ?? Vector3.zero;

    private void Awake()
    {
        // รีเซ็ตสถานะการปลดล็อคทั้งหมดเมื่อเริ่มเกมใหม่
        if (itemData != null)
        {
            foreach (var item in itemData.items)
            {
                item.isUnlocked = false;
            }
        }

        // ถ้าไม่ได้ลาก Player มาใน Inspector ให้หาจาก Tag "Player"
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else playerTransform = transform; // ถ้าหาไม่เจอจริงๆ ให้ใช้ตัวเองไปก่อน
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            ReleaseItem();
        }

        if (Input.GetKeyDown(KeyCode.E) && !isSlowed)
        {
            OpenTyping();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && isSlowed)
        {
            SetSlowMotion(false);
        }

        // ทำให้ไอเทมที่ลอยอยู่มีการขยับขึ้นลง (Bobbing Effect)
        ApplyFloatingEffect();
    }

    private void ApplyFloatingEffect()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        
        if (spawnedFirst != null)
        {
            Vector3 targetLocalPos = new Vector3(
                firstSlotOffset.x, 
                firstSlotOffset.y + bob, 
                firstSlotOffset.z - (firstItem.itemSize.z * 0.5f)
            );
            // ใช้ Lerp เพื่อให้การตาม Player ดูนุ่มนวลขึ้น
            spawnedFirst.transform.localPosition = Vector3.Lerp(
                spawnedFirst.transform.localPosition, 
                targetLocalPos, 
                Time.deltaTime * followSpeed
            );
            // ให้หันหน้าไปทางเดียวกับ Player
            spawnedFirst.transform.localRotation = Quaternion.Lerp(
                spawnedFirst.transform.localRotation, 
                Quaternion.identity, 
                Time.deltaTime * followSpeed
            );
        }
        
        if (spawnedSecond != null)
        {
            Vector3 targetLocalPos = new Vector3(
                secondSlotOffset.x, 
                secondSlotOffset.y + bob, 
                secondSlotOffset.z - (secondItem.itemSize.z * 0.5f)
            );
            spawnedSecond.transform.localPosition = Vector3.Lerp(
                spawnedSecond.transform.localPosition, 
                targetLocalPos, 
                Time.deltaTime * followSpeed
            );
            spawnedSecond.transform.localRotation = Quaternion.Lerp(
                spawnedSecond.transform.localRotation, 
                Quaternion.identity, 
                Time.deltaTime * followSpeed
            );
        }
    }

    public bool TryMatchItem(string input)
    {
        if (itemData == null) return false;

        foreach (var item in itemData.items)
        {
            if (string.Equals(item.itemName, input, System.StringComparison.OrdinalIgnoreCase))
            {
                if (item.isUnlocked) // เช็คว่าปลดล็อค (แตะในด่าน) แล้วหรือยัง
                {
                    ProcessItemMatch(item);
                    SetSlowMotion(false);
                    return true;
                }
                else
                {
                    Debug.Log($"Item {item.itemName} is found but not unlocked yet!");
                    break; // ไม่ return true เพื่อให้เล่นเสียง Error
                }
            }
        }

        PlaySFX(errorSFX);
        return false;
    }

    private void ProcessItemMatch(ItemInfo matchedItem)
    {
        if (firstItem == null || string.IsNullOrEmpty(firstItem.itemName))
        {
            firstItem = matchedItem;
            spawnedFirst = SpawnItemAtOffset(firstItem, firstSlotOffset);
            
            PlaySFX(matchSFX);
            SpawnVFX(matchVFXPrefab, vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position);
            Debug.Log($"<color=cyan>First Item: {firstItem.itemName}</color>");
        }
        else
        {
            secondItem = matchedItem;
            spawnedSecond = SpawnItemAtOffset(secondItem, secondSlotOffset);
            
            Debug.Log($"<color=cyan>Second Item: {secondItem.itemName}</color>");
            CheckCombination();
        }
    }

    private GameObject SpawnItemAtOffset(ItemInfo item, Vector3 relativeOffset)
    {
        if (item.itemPrefab == null) return null;

        // สร้างไอเทมโดยให้มันเป็นลูกของ playerTransform เพื่อให้มันเคลื่อนที่ตาม Player
        GameObject obj = Instantiate(item.itemPrefab, playerTransform);
        obj.transform.localScale = item.itemSize;
        
        // กำหนดตำแหน่ง local ทันที (Update จะคอยคุมตำแหน่ง floating ต่อ)
        obj.transform.localPosition = new Vector3(
            relativeOffset.x, 
            relativeOffset.y, 
            relativeOffset.z - (item.itemSize.z * 0.5f)
        );
        
        obj.transform.localRotation = Quaternion.identity;

        // ทำให้ไอเทมดูใสๆ (Transparency)
        ApplyTransparency(obj, itemAlpha);

        return obj;
    }

    private void ApplyTransparency(GameObject obj, float alpha)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // ใช้ .materials เพื่อสร้าง instance ของ material มาแก้ (ไม่กระทบไฟล์ต้นฉบับ)
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;

                    // --- สำหรับ Standard Shader ---
                    if (alpha < 1.0f)
                    {
                        mat.SetFloat("_Mode", 3); // 3 คือ Transparent mode
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = 3000;
                    }
                    else
                    {
                        mat.SetFloat("_Mode", 0); // 0 คือ Opaque mode
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        mat.SetInt("_ZWrite", 1);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.DisableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = -1;
                    }

                    // --- สำหรับ URP (Universal Render Pipeline) ---
                    if (mat.HasProperty("_Surface"))
                    {
                        mat.SetFloat("_Surface", alpha < 1.0f ? 1 : 0); // 1 = Transparent, 0 = Opaque
                        mat.SetFloat("_Blend", 0); // 0 = Alpha blend
                    }
                }
            }
        }
    }

    private void CheckCombination()
    {
        foreach (var combo in itemData.combinations)
        {
            bool match1 = (combo.itemAName == firstItem.itemName && combo.itemBName == secondItem.itemName);
            bool match2 = (combo.itemBName == firstItem.itemName && combo.itemAName == secondItem.itemName);

            if (match1 || match2)
            {
                Debug.Log($"<color=yellow>Combination Success! Result: {combo.resultItem.itemName}</color>");
                
                // ทำลายของเก่าทั้งคู่
                if (spawnedFirst != null) Destroy(spawnedFirst);
                if (spawnedSecond != null) Destroy(spawnedSecond);

                // ตั้งค่าไอเทมใหม่
                firstItem = combo.resultItem;
                secondItem = null;
                
                // สร้างไอเทมผลลัพธ์
                spawnedFirst = SpawnItemAtOffset(firstItem, firstSlotOffset);
                spawnedSecond = null;
                
                PlaySFX(combineSFX);
                SpawnVFX(combineVFXPrefab, vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position);
                return;
            }
        }
        
        Debug.Log("No combination found. Replacing first item with latest.");
        
        // ถ้าผสมไม่ได้ ให้เอาอันที่สองมาแทนอันแรก
        if (spawnedFirst != null) Destroy(spawnedFirst);
        
        firstItem = secondItem;
        spawnedFirst = spawnedSecond;
        
        // เลื่อนตำแหน่ง spawnedFirst มาอยู่ที่ Slot 1 (Update จะจัดการเรื่อง bobbing ให้เอง)
        if (spawnedFirst != null)
        {
            spawnedFirst.transform.localPosition = new Vector3(
                firstSlotOffset.x,
                firstSlotOffset.y,
                firstSlotOffset.z - (firstItem.itemSize.z * 0.5f)
            );
        }

        secondItem = null;
        spawnedSecond = null;
        
        PlaySFX(matchSFX); // Play match sound if just replaced
    }

    private void ReleaseItem()
    {
        if (firstItem != null && spawnedFirst != null)
        {
            // คืนค่าความชัด (Alpha = 1) ก่อนปล่อย
            ApplyTransparency(spawnedFirst, 1.0f);

            // ปล่อยไอเทมออกจากตัว
            spawnedFirst.transform.SetParent(null);
            
            // วางไว้ข้างหน้าผู้เล่น
            Vector3 spawnPos = playerTransform.position + playerTransform.forward * 2f;
            spawnedFirst.transform.position = spawnPos;
            
            PlaySFX(releaseSFX);
            SpawnVFX(releaseVFXPrefab, spawnPos);

            Debug.Log($"Released: {firstItem.itemName}");
            
            // ล้างสถานะแต่ไม่ทำลาย Object เพราะเราปล่อยมันลงพื้นแล้ว
            firstItem = null;
            spawnedFirst = null;
            secondItem = null;
            spawnedSecond = null;
        }
    }

    public void OpenTyping()
    {
        SetSlowMotion(true);
        PlaySFX(openSFX);
    }

    private void SetSlowMotion(bool state)
    {
        isSlowed = state;
        Time.timeScale = isSlowed ? slowTimeScale : 1f;
        Time.fixedDeltaTime = 0.02f * (isSlowed ? slowTimeScale : 1f);

        if (typingUI != null)
        {
            typingUI.SetActive(isSlowed);
        }

        if (isSlowed && inputField != null)
        {
            inputField.text = "";
            inputField.Select();
            inputField.ActivateInputField();
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            // PlayOneShot is great because it can overlap and isn't affected by AudioSource.Stop()
            audioSource.PlayOneShot(clip);
        }
    }

    private void SpawnVFX(GameObject prefab, Vector3 position)
    {
        if (prefab != null)
        {
            GameObject vfx = Instantiate(prefab, position, Quaternion.identity);
            // Optional: Auto-destroy VFX after 2 seconds to keep the scene clean
            Destroy(vfx, 2f);
        }
    }

    public void MatchItem(string input) => TryMatchItem(input);

    public void ClearMatch()
    {
        if (spawnedFirst != null) Destroy(spawnedFirst);
        if (spawnedSecond != null) Destroy(spawnedSecond);
        
        firstItem = null;
        secondItem = null;
        spawnedFirst = null;
        spawnedSecond = null;
    }
}
