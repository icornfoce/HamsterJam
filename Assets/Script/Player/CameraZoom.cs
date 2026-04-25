using UnityEngine;
using Unity.Cinemachine; // จำเป็นสำหรับ Cinemachine 3

[RequireComponent(typeof(CinemachineCamera))]
public class CameraZoom : MonoBehaviour
{
    [Header("การตั้งค่าซูม")]
    public float zoomSpeed = 5f;   // ความเร็วในการซูม
    public float minZoom = 2f;     // ซูมเข้าใกล้สุดได้แค่ไหน
    public float maxZoom = 15f;    // ซูมออกไกลสุดได้แค่ไหน

    private CinemachineOrbitalFollow orbitalFollow;

    void Start()
    {
        // ดึงตัวควบคุมการโคจร (Orbital Follow) ที่อยู่ในกล้องมาใช้งาน
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();

        if (orbitalFollow == null)
        {
            Debug.LogWarning("ไม่พบระบบ Orbital Follow ในกล้องตัวนี้ครับ");
        }
    }

    void Update()
    {
        if (orbitalFollow == null) return;

        // อ่านค่าการกลิ้งลูกกลิ้งเมาส์ (เลื่อนขึ้นจะได้ค่าบวก เลื่อนลงจะได้ค่าลบ)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0f)
        {
            // คำนวณระยะห่างใหม่ (เลื่อนขึ้น=ซูมเข้า รัศมีต้องลดลง)
            float targetRadius = orbitalFollow.Radius - (scrollInput * zoomSpeed);

            // บังคับไม่ให้ซูมทะลุค่า Min/Max ที่ตั้งไว้
            orbitalFollow.Radius = Mathf.Clamp(targetRadius, minZoom, maxZoom);
        }
    }
}
