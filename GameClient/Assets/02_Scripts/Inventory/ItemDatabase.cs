using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> items = new List<ItemData>();

    // 싱글톤 인스턴스
    private static ItemDatabase instance;

    public static ItemDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (instance == null)
                {
                    Debug.LogError("ItemDatabase가 Resources 폴더에 없습니다!");
                }
            }
            return instance;
        }
    }

    // 아이템 ID로 데이터 가져오기
    public static ItemData GetItemData(uint itemID)
    {
        if (Instance == null) return null;

        return Instance.items.Find(item => item.itemID == itemID);
    }

    // 아이템 이름으로 검색
    public static ItemData GetItemDataByName(string name)
    {
        if (Instance == null) return null;

        return Instance.items.Find(item => item.itemName == name);
    }
}