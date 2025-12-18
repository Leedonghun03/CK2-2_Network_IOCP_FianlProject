using UnityEngine;

[System.Serializable]
public class ItemData
{
    public uint itemID;
    public string itemName;
    public ITEM_TYPE itemType;
    public string description;
    public Sprite icon;

    // 포션 관련
    public int healAmount;      // 회복량
    public int duration;        // 지속 시간

    // 가격 등
    public int buyPrice;
    public int sellPrice;

    // 스택 가능 여부
    public bool stackable = true;
    public int maxStack = 99;
}