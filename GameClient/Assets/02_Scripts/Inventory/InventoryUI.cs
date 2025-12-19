using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform itemSlotContainer;  // Content Transform
    public GameObject itemSlotPrefab;
    public Button closeButton;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.I;

    private List<ItemSlotUI> itemSlots = new List<ItemSlotUI>();
    private bool isInventoryOpen = false;

    void Start()
    {
        // 닫기 버튼 이벤트
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }

        // 슬롯 생성 (40개)
        CreateSlots(40);

        // 시작 시 닫힌 상태
        CloseInventory();

        // InventoryManager에 UI 등록
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetInventoryUI(this);
        }
    }

    void Update()
    {
        // I 키로 인벤토리 열기/닫기
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    void CreateSlots(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, itemSlotContainer);
            ItemSlotUI slot = slotObj.AddComponent<ItemSlotUI>();
            slot.Initialize(i);
            itemSlots.Add(slot);
        }
    }

    public void UpdateInventory(Item[] items)
    {
        if (items == null) return;

        int count = Mathf.Min(itemSlots.Count, items.Length);

        for (int i = 0; i < count; i++)
        {
            var item = items[i];

            // 빈 슬롯 처리
            if (item.itemID == 0 || item.quantity == 0)
                itemSlots[i].ClearSlot();
            else
                itemSlots[i].SetItem(item);
        }

        for (int i = count; i < itemSlots.Count; i++)
            itemSlots[i].ClearSlot();
    }

    public void ToggleInventory()
    {
        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    public void OpenInventory()
    {
        isInventoryOpen = true;
        inventoryPanel.SetActive(true);

        // 마우스 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 인벤토리 정보 요청
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RequestInventoryInfo();
        }
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        inventoryPanel.SetActive(false);

        // 마우스 커서 숨김
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}