using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Components")]
    public Image itemIcon;
    public Text quantityText;
    public Text itemNameText;

    private int slotIndex;
    private Item currentItem;
    private bool hasItem = false;

    void Awake()
    {
        // 자동으로 컴포넌트 찾기
        itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
        quantityText = transform.Find("QuantityText")?.GetComponent<Text>();
        itemNameText = transform.Find("ItemNameText")?.GetComponent<Text>();
    }

    public void Initialize(int index)
    {
        slotIndex = index;
        ClearSlot();
    }

    public void SetItem(Item item)
    {
        currentItem = item;
        hasItem = true;

        ItemData itemData = ItemDatabase.GetItemData(item.itemID);

        // 아이템 이름
        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
            itemNameText.enabled = true;
        }

        // 아이템 수량
        if (quantityText != null)
        {
            quantityText.text = item.quantity.ToString();
            quantityText.enabled = true;
        }

        // 아이템 아이콘 (TODO: 실제 스프라이트 로드)
        if (itemIcon != null)
        {
            // 임시: 아이템 타입별 색상
            itemIcon.color = GetItemTypeColor(item.itemType);
            itemIcon.enabled = true;
        }
    }

    public void ClearSlot()
    {
        hasItem = false;
        currentItem = new Item();

        if (itemNameText != null)
            itemNameText.enabled = false;

        if (quantityText != null)
            quantityText.enabled = false;

        if (itemIcon != null)
            itemIcon.enabled = false;
    }

    // 클릭 이벤트
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!hasItem) return;

        // 좌클릭: 아이템 사용
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            UseItem();
        }
        // 우클릭: 아이템 정보 표시
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            ShowItemInfo();
        }
    }

    void UseItem()
    {
        Debug.Log($"아이템 사용: {currentItem.itemName}");

        InventoryManager inventory = FindObjectOfType<InventoryManager>();
        if (inventory != null)
        {
            inventory.RequestUseItem((ushort)slotIndex);
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RequestUseItem((ushort)slotIndex);
        }
    }

    void ShowItemInfo()
    {
        Debug.Log($"아이템 정보: {currentItem.itemName} (ID: {currentItem.itemID}, 수량: {currentItem.quantity})");
        // TODO: 툴팁 UI 표시
    }

    // 임시: 아이템 타입별 색상
    Color GetItemTypeColor(ITEM_TYPE type)
    {
        switch (type)
        {
            case ITEM_TYPE.WEAPON:
                return new Color(1f, 0.5f, 0.5f); // 빨강
            case ITEM_TYPE.ARMOR:
                return new Color(0.5f, 0.5f, 1f); // 파랑
            case ITEM_TYPE.POTION:
                return new Color(0.5f, 1f, 0.5f); // 초록
            case ITEM_TYPE.MATERIAL:
                return new Color(0.8f, 0.8f, 0.8f); // 회색
            case ITEM_TYPE.QUEST:
                return new Color(1f, 1f, 0.5f); // 노랑
            default:
                return Color.white;
        }
    }
}