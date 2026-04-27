using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class GameTimer : MonoBehaviour
{
    public enum TimerType { CountUp, CountDown }

    [Header("ตั้งค่าเวลา")]
    [Tooltip("CountUp = นับขึ้น (จับเวลา), CountDown = นับถอยหลัง")]
    public TimerType timerType = TimerType.CountDown;
    [Tooltip("เวลาเริ่มต้น (วินาที) (ใช้เฉพาะเวลานับถอยหลัง)")]
    public float startTimeInSeconds = 60f;
    [Tooltip("ให้เริ่มนับทันทีที่เปิดเกมหรือไม่")]
    public bool startOnAwake = true;

    [Header("อ้างอิง UI")]
    [Tooltip("ลาก TextMeshProUGUI มาใส่ตรงนี้เพื่อแสดงเวลา")]
    public TextMeshProUGUI timerText;

    [Header("เหตุการณ์ (ใช้เฉพาะนับถอยหลัง)")]
    [Tooltip("ใส่สิ่งที่จะเกิดขึ้นเมื่อเวลาหมด (เช่น เรียกฟังก์ชันตาย หรือ จบเกม)")]
    public UnityEvent OnTimeOut;

    private float currentTime;
    private bool isRunning = false;

    private void Start()
    {
        // กำหนดเวลาเริ่มต้น
        ResetTimer();

        if (startOnAwake)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        if (timerType == TimerType.CountUp)
        {
            currentTime += Time.deltaTime;
        }
        else if (timerType == TimerType.CountDown)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0f)
            {
                currentTime = 0f;
                StopTimer();
                OnTimeOut?.Invoke(); // เรียก Event เมื่อเวลาหมด
            }
        }

        UpdateTimerUI();
    }

    /// <summary>
    /// สั่งให้เริ่มจับเวลา
    /// </summary>
    public void StartTimer()
    {
        isRunning = true;
    }

    /// <summary>
    /// สั่งให้หยุดจับเวลา
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }

    /// <summary>
    /// รีเซ็ตเวลาใหม่
    /// </summary>
    public void ResetTimer()
    {
        currentTime = timerType == TimerType.CountDown ? startTimeInSeconds : 0f;
        UpdateTimerUI();
    }

    /// <summary>
    /// แสดงผลเวลาออกทาง UI ให้อยู่ในรูปแบบ นาที:วินาที:มิลลิวินาที
    /// </summary>
    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        // คำนวณเป็น นาที วินาที และ มิลลิวินาที
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        int milliseconds = Mathf.FloorToInt((currentTime * 100f) % 100f);

        // จัดรูปแบบ String เช่น 01:25:40
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }
}
