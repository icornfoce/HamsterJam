using UnityEngine;
using TMPro;

public class TypingSystem : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private GameObject typingUI; 
    [SerializeField] private TMP_InputField inputField;

    [Header("Vignette Effects")]
    [Tooltip("ลาก Q_Vignette_Base (ตั้งเป็นสีโทนมืด) มาใส่ช่องนี้")]
    [SerializeField] private Q_Vignette_Base typingVignette;
    [Tooltip("ความเร็วในการเข้มขึ้น/จางลงของ Overlay")]
    [SerializeField] private float overlayFadeSpeed = 10f;
    [Tooltip("ความเข้มสูงสุดตอนพิมพ์ (0-1)")]
    [SerializeField] private float maxOverlayAlpha = 0.8f;

    private float currentOverlayAlpha = 0f;

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

        // กำหนดโปร่งใสเริ่มต้น
        if (typingVignette != null)
        {
            currentOverlayAlpha = 0f;
            SetTypingVignetteAlpha(0f);
            typingVignette.gameObject.SetActive(false);
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

        // ตรวจสอบการกด Enter เพื่อส่งคำศัพท์ (ส่งเฉพาะตอนที่เปิดหน้าต่างพิมพ์อยู่)
        if (isSlowed && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            if (inputField != null && !string.IsNullOrEmpty(inputField.text))
            {
                TryMatchItem(inputField.text);
                inputField.text = "";
                inputField.ActivateInputField();
            }
        }

        // อัปเดตความเข้มของ Q Vignette ตอนพิมพ์
        if (typingVignette != null)
        {
            float targetAlpha = isSlowed ? maxOverlayAlpha : 0f;
            // ใช้ Time.unscaledDeltaTime เพราะเกมถูก slow อยู่
            currentOverlayAlpha = Mathf.MoveTowards(currentOverlayAlpha, targetAlpha, overlayFadeSpeed * Time.unscaledDeltaTime);
            SetTypingVignetteAlpha(currentOverlayAlpha);

            if (currentOverlayAlpha > 0 && !typingVignette.gameObject.activeSelf)
            {
                typingVignette.gameObject.SetActive(true);
            }
            else if (currentOverlayAlpha <= 0 && typingVignette.gameObject.activeSelf)
            {
                typingVignette.gameObject.SetActive(false);
            }
        }

        // ทำให้ไอเทมที่ลอยอยู่มีการขยับขึ้นลง (Bobbing Effect)
        ApplyFloatingEffect();
    }

    private void SetTypingVignetteAlpha(float alpha)
    {
        if (typingVignette.cornerImages == null) return;
        for (int i = 0; i < typingVignette.cornerImages.Length; i++)
        {
            if (typingVignette.cornerImages[i] != null)
            {
                Color c = typingVignette.cornerImages[i].color;
                c.a = alpha;
                typingVignette.cornerImages[i].color = c;
            }
        }
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
                    break; // ออกจาก Loop เพื่อไปปิดหน้าต่างและเล่นเสียง Error
                }
            }
        }

        // ถ้าพิมพ์ผิด หรือหาไม่เจอ ให้ปิดหน้าต่างพิมพ์เหมือนกัน
        SetSlowMotion(false);
        PlaySFX(errorSFX);
        return false;
    }

    private void ProcessItemMatch(ItemInfo matchedItem)
    {
        // ถ้าช่องแรกว่าง ให้ใส่ช่องแรก
        if (firstItem == null || string.IsNullOrEmpty(firstItem.itemName))
        {
            firstItem = matchedItem;
            spawnedFirst = SpawnItemAtOffset(firstItem, firstSlotOffset);
            
            PlaySFX(matchSFX);
            SpawnVFX(matchVFXPrefab, spawnedFirst.transform.position);
            Debug.Log($"<color=cyan>First Item: {firstItem.itemName}</color>");
        }
        // ถ้าช่องสองว่าง ให้ใส่ช่องสอง
        else if (secondItem == null || string.IsNullOrEmpty(secondItem.itemName))
        {
            secondItem = matchedItem;
            spawnedSecond = SpawnItemAtOffset(secondItem, secondSlotOffset);
            
            PlaySFX(matchSFX);
            SpawnVFX(matchVFXPrefab, spawnedSecond.transform.position);
            Debug.Log($"<color=cyan>Second Item: {secondItem.itemName}</color>");
            CheckCombination();
        }
        // ถ้าเต็มทั้งสองช่อง ให้เปลี่ยนอันที่สองเป็นอันใหม่
        else
        {
            if (spawnedSecond != null) Destroy(spawnedSecond);
            secondItem = matchedItem;
            spawnedSecond = SpawnItemAtOffset(secondItem, secondSlotOffset);
            
            PlaySFX(matchSFX);
            SpawnVFX(matchVFXPrefab, spawnedSecond.transform.position);
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
                spawnedSecond = null;
                
                // สร้างไอเทมผลลัพธ์
                spawnedFirst = SpawnItemAtOffset(firstItem, firstSlotOffset);
                
                PlaySFX(combineSFX);
                SpawnVFX(combineVFXPrefab, spawnedFirst.transform.position);
                return;
            }
        }
        
        Debug.Log("No combination found. Keeping both items in their slots.");
        // ไม่ต้องทำอะไร ปล่อยให้มันลอยอยู่คนละฝั่งตามปกติ
    }

    private void ReleaseItem()
    {
        // ปล่อยชิ้นที่สองก่อน (ถ้ามี)
        if (secondItem != null && spawnedSecond != null)
        {
            PerformRelease(ref secondItem, ref spawnedSecond);
        }
        // ถ้าไม่มีชิ้นที่สอง ให้ปล่อยชิ้นแรก
        else if (firstItem != null && spawnedFirst != null)
        {
            PerformRelease(ref firstItem, ref spawnedFirst);
        }
    }

    private void PerformRelease(ref ItemInfo item, ref GameObject obj)
    {
        if (item == null || obj == null) return;

        // ── เรียกใช้ Skill ของไอเทมนี้ (ถ้ามี) ──
        if (item.itemSkill != null)
        {
            // คำนวณจุดเกิดสกิล โดยใช้ค่า Offset ที่ตั้งไว้ใน ItemData
            // แปลง Local Offset ให้เป็น World Position (คำนึงถึงทิศทางที่ Player หันหน้าอยู่)
            Vector3 spawnPos = playerTransform.position
                + playerTransform.right   * item.skillSpawnOffset.x   // ซ้าย/ขวา
                + playerTransform.up      * item.skillSpawnOffset.y   // บน/ล่าง
                + playerTransform.forward * item.skillSpawnOffset.z;  // หน้า/หลัง

            // คำนวณองศาการเกิด โดยอ้างอิงจากมุมที่ Player หันหน้าอยู่ และบวกด้วยองศาที่ตั้งไว้ใน ItemData
            Quaternion spawnRot = playerTransform.rotation * Quaternion.Euler(item.skillSpawnEulerAngles);

            GameObject skillObj = Instantiate(item.itemSkill, spawnPos, spawnRot);
            
            BaseItemSkill skill = skillObj.GetComponent<BaseItemSkill>();
            if (skill != null)
            {
                skill.Activate(playerTransform);
            }
        }
        
        PlaySFX(releaseSFX);
        SpawnVFX(releaseVFXPrefab, playerTransform.position);

        Debug.Log($"Released: {item.itemName}");

        // ทำลายไอเทมที่ลอยอยู่ (ไม่ต้องวางลงพื้น)
        Destroy(obj);
        
        // ล้างสถานะ
        item = null;
        obj = null;
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
