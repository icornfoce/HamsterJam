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

    // Properties to access the current main item
    public string ItemName => firstItem?.itemName ?? string.Empty;
    public GameObject ItemPrefab => firstItem?.itemPrefab;
    public Vector3 ItemSize => firstItem?.itemSize ?? Vector3.zero;

    private void Awake()
    {
        // รีเซ็ตสถานะการปลดล็อคทั้งหมดเมื่อเริ่มเกมใหม่
        // (เพราะ ItemData เป็น ScriptableObject ค่ามันจะค้างอยู่จากการกด Play ครั้งก่อนใน Editor)
        if (itemData != null)
        {
            foreach (var item in itemData.items)
            {
                item.isUnlocked = false;
            }
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
            PlaySFX(matchSFX);
            SpawnVFX(matchVFXPrefab, vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position);
            Debug.Log($"<color=cyan>First Item: {firstItem.itemName}</color>");
        }
        else
        {
            secondItem = matchedItem;
            Debug.Log($"<color=cyan>Second Item: {secondItem.itemName}</color>");
            CheckCombination();
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
                firstItem = combo.resultItem;
                secondItem = null;
                
                PlaySFX(combineSFX);
                SpawnVFX(combineVFXPrefab, vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position);
                return;
            }
        }
        
        Debug.Log("No combination found. Replacing first item with latest.");
        firstItem = secondItem;
        secondItem = null;
        PlaySFX(matchSFX); // Play match sound if just replaced
    }

    private void ReleaseItem()
    {
        if (firstItem != null && firstItem.itemPrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * 2f;
            GameObject dropped = Instantiate(firstItem.itemPrefab, spawnPos, Quaternion.identity);
            dropped.transform.localScale = firstItem.itemSize;
            
            PlaySFX(releaseSFX);
            SpawnVFX(releaseVFXPrefab, spawnPos);

            Debug.Log($"Released: {firstItem.itemName}");
            ClearMatch();
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
        firstItem = null;
        secondItem = null;
    }
}
