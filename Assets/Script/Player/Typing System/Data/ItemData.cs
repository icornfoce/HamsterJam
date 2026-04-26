using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemInfo
{
    public string itemName;
    public GameObject itemPrefab;
    public Vector3 itemSize;
    public bool isUnlocked = false; // เก็บสถานะว่าผู้เล่นเคยแตะเพื่อปลดล็อคแล้วหรือยัง

    [Header("Skill")]
    public GameObject itemSkill; // Prefab ของ Skill ที่จะถูกเรียกใช้เมื่อปล่อยไอเทมนี้
    [Tooltip("จุดเกิดสกิลเทียบกับตัว Player (เช่น 0,1.5,1 = ลอยอยู่หน้าอก ด้านหน้า)")]
    public Vector3 skillSpawnOffset = new Vector3(0f, 1f, 1f);
}

[System.Serializable]
public class ItemCombination
{
    public string itemAName;
    public string itemBName;
    public ItemInfo resultItem;
}

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public List<ItemInfo> items = new List<ItemInfo>();
    public List<ItemCombination> combinations = new List<ItemCombination>();
}
