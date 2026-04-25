using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("อ้างอิง UI")]
    [Tooltip("ลาก Canvas หรือ Panel ของไอเทมชิ้นนี้มาใส่ได้เลย (คุณสามารถไปพิมพ์ข้อความตกแต่งเตรียมไว้ใน UI ได้เลย)")]
    public GameObject uiCanvas; // หน้าต่าง Canvas ที่จะให้เด้งขึ้นมา

    private void Start()
    {
        // ซ่อน UI ไว้ก่อนตอนเริ่มเกม
        if (uiCanvas != null)
        {
            uiCanvas.SetActive(false);
        }
    }

    // ทำงานเมื่อมีบางอย่างเข้ามาชน (ตัว Item ต้องติ๊ก Is Trigger ใน Collider ด้วย)
    private void OnTriggerEnter(Collider other)
    {
        // เช็คว่าคนที่มาชนมี Tag เป็น "Player" หรือไม่
        if (other.CompareTag("Player"))
        {
            if (uiCanvas != null)
            {
                // แสดง UI ที่คุณทำเตรียมไว้ขึ้นมา
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
                // ปิด UI เมื่อผู้เล่นเดินออก
                uiCanvas.SetActive(false);
            }
        }
    }
}
