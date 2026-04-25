using UnityEngine;
using TMPro;

public class Item : MonoBehaviour
{
    [Header("ข้อมูลไอเทม (อิงจากโฟลเดอร์ Data)")]
    public ItemData itemData; // ลากไฟล์ ItemData จากโฟลเดอร์ Data มาใส่ตรงนี้
    public string targetItemName; // ชื่อไอเทมชิ้นนี้ที่จะปลดล็อคใน Data (ต้องพิมพ์ให้ตรงกับใน Data)

    [Header("อ้างอิง UI")]
    [Tooltip("ลาก Canvas หรือ Panel ของไอเทมชิ้นนี้มาใส่ได้เลย")]
    public GameObject uiCanvas; // หน้าต่าง Canvas ที่จะให้เด้งขึ้นมาบอกว่าได้รับไอเทม
    public TextMeshProUGUI statusText; // ตัวหนังสือที่จะบอกว่าได้รับไอเทมอะไร

    private void Start()
    {
        // ซ่อน UI ไว้ก่อนตอนเริ่มเกม
        if (uiCanvas != null)
        {
            uiCanvas.SetActive(false);
        }
    }

    // ทำงานเมื่อมีบางอย่างเข้ามาชน
    private void OnTriggerEnter(Collider other)
    {
        // เช็คว่าคนที่มาชนมี Tag เป็น "Player" หรือไม่
        if (other.CompareTag("Player"))
        {
            // ทำการปลดล็อคใน ItemData
            if (itemData != null && !string.IsNullOrEmpty(targetItemName))
            {
                foreach (var item in itemData.items)
                {
                    // ค้นหาไอเทมใน Data ที่ชื่อตรงกัน
                    if (item.itemName == targetItemName)
                    {
                        item.isUnlocked = true; // ปลดล็อคให้เป็น true เลยตลอด
                        
                        // แสดงข้อความ You get [name]
                        if (statusText != null)
                        {
                            statusText.text = "You get\n[ " + item.itemName + " ]";
                        }
                        
                        Debug.Log("You get\n[ " + item.itemName + " ]");
                        break; // เจอแล้วหยุดค้นหา
                    }
                }
            }

            if (uiCanvas != null)
            {
                // แสดง UI ขึ้นมา (เช่น คำอธิบายว่าได้ไอเทมนี้แล้ว)
                uiCanvas.SetActive(true);
            }
        }
    }

    // ทำงานเมื่อเดินออกจาก Item
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (uiCanvas != null)
            {
                // ปิด UI เมื่อผู้เล่นเดินออก (แต่ใน Data จะยังเป็น isUnlocked = true อยู่)
                uiCanvas.SetActive(false);
            }
        }
    }
}
