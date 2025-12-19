using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Components")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI itemNameText;

    private int slotIndex;
    private Item currentItem;
    private bool hasItem;

    void Awake()
    {
        // 인스펙터로 이미 연결돼 있으면 그대로 사용 (덮어쓰지 않기!)
        if (itemIcon == null)
            itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();

        if (quantityText == null)
            quantityText = transform.Find("QuantityText")?.GetComponent<TextMeshProUGUI>();

        if (itemNameText == null)
            itemNameText = transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();
    }

    public void Initialize(int index)
    {
        slotIndex = index;
        ClearSlot();
    }

    public void SetItem(Item item)
    {
        if (item.itemID == 0 || item.quantity == 0)
        {
            ClearSlot();
            return;
        }

        hasItem = true;
        currentItem = item;

        var itemData = ItemDatabase.GetItemData(item.itemID);

        // 이름은 서버 문자열 대신 DB를 쓰는 게 안전
        if (itemNameText != null)
        {
            itemNameText.text = (itemData != null) ? itemData.itemName : item.itemName;
            itemNameText.gameObject.SetActive(true);
        }

        if (quantityText != null)
        {
            quantityText.SetText("{0}", item.quantity);
            quantityText.gameObject.SetActive(true);
        }

        if (itemIcon != null)
        {
            if (itemData != null && itemData.icon != null)
            {
                itemIcon.sprite = itemData.icon;
                itemIcon.color = Color.white;
                itemIcon.preserveAspect = true;
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                itemIcon.sprite = null;
                itemIcon.gameObject.SetActive(false);
            }
        }
    }

    public void ClearSlot()
    {
        hasItem = false;
        currentItem = new Item();

        if (itemNameText != null) { itemNameText.text = ""; itemNameText.gameObject.SetActive(false); }
        if (quantityText != null) { quantityText.text = ""; quantityText.gameObject.SetActive(false); }
        if (itemIcon != null) { itemIcon.sprite = null; itemIcon.gameObject.SetActive(false); }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!hasItem) return;
        if (eventData.button == PointerEventData.InputButton.Left)
            InventoryManager.Instance?.RequestUseItem((ushort)slotIndex);
    }
}
