using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemInfo
{
    public string itemName;
    public GameObject itemPrefab;
    public Vector3 itemSize;
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
