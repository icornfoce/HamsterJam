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

    [Header("ตั้งค่าการเก็บไอเทม")]
    [Tooltip("ถ้าเปิดใช้งาน ไอเทมจะหายไปเมื่อเก็บเสร็จ, ถ้าปิด ไอเทมจะวางอยู่ที่เดิม")]
    public bool destroyOnCollect = true;
    [Tooltip("ลากโมเดลของไอเทม (กล่อง/หนังสือ) มาใส่ตรงนี้เพื่อทำให้หายไปตอนเก็บ (ถ้าไม่ใส่ระบบจะพยายามซ่อนเอง)")]
    public GameObject itemModel;
    [Tooltip("ระยะเวลาที่จะโชว์ UI ค้างไว้ก่อนลบทิ้ง (วินาที)")]
    public float showUITime = 2f;

    private bool isCollected = false;

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
        // ถ้าตั้งค่าให้หายไป และโดนเก็บไปแล้ว ไม่ต้องทำซ้ำ
        if (destroyOnCollect && isCollected) return;

        // เช็คว่าคนที่มาชนมี Tag เป็น "Player" หรือไม่
        if (other.CompareTag("Player"))
        {
            isCollected = true; // มาร์คว่าเก็บแล้ว

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

            // ถ้าระบบเปิดให้ของหาย
            if (destroyOnCollect)
            {
                // ซ่อนโมเดลของไอเทม (ทำให้ดูเหมือนหยิบไปแล้ว)
                HideItemVisuals();

                // สั่งให้รอตามเวลา showUITime แล้วค่อยปิด UI และทำลาย Object ทิ้ง
                Invoke("FinishCollection", showUITime);
            }
        }
    }

    private void HideItemVisuals()
    {
        // ถ้ามีการลาก Model มาใส่ช่อง ก็ปิดมันเลย
        if (itemModel != null)
        {
            itemModel.SetActive(false);
        }
        else
        {
            // ถ้าไม่ได้ลากใส่ ให้ลองซ่อน MeshRenderer
            MeshRenderer rend = GetComponent<MeshRenderer>();
            if (rend != null) rend.enabled = false;

            // และลองซ่อนลูกๆ ทั้งหมดที่ไม่ใช่ UI
            foreach (Transform child in transform)
            {
                if (uiCanvas == null || child.gameObject != uiCanvas)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    private void FinishCollection()
    {
        if (uiCanvas != null)
        {
            uiCanvas.SetActive(false);
        }
        // ลบ Object ทิ้งออกจากฉากเลย
        Destroy(gameObject);
    }

    // ทำงานเมื่อเดินออกจาก Item (เผื่อตั้งค่าให้ไอเทมไม่หาย)
    private void OnTriggerExit(Collider other)
    {
        if (!destroyOnCollect && other.CompareTag("Player"))
        {
            if (uiCanvas != null)
            {
                // ปิด UI เมื่อผู้เล่นเดินออก 
                uiCanvas.SetActive(false);
            }
        }
    }
}
