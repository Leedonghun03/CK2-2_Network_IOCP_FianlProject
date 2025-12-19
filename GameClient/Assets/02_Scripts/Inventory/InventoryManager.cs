using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour, IPacketReceiver
{
    public static InventoryManager Instance { get; private set; }

    private const int MAX_INVENTORY_SIZE = 40;
    private Item[] items = new Item[MAX_INVENTORY_SIZE];

    private InventoryUI inventoryUI;

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 아이템 배열 초기화
        for (int i = 0; i < MAX_INVENTORY_SIZE; i++)
        {
            items[i] = new Item();
        }
    }

    void Start()
    {
        Client.TCP.AddPacketReceiver(this);
    }

    public void SetInventoryUI(InventoryUI ui)
    {
        inventoryUI = ui;
    }

    public void RequestInventoryInfo()
    {
        P_InventoryInfoRequest request = new P_InventoryInfoRequest();
        Client.TCP.SendPacket(E_PACKET.INVENTORY_INFO_REQUEST, request);
        Debug.Log("인벤토리 정보 요청");
    }

    // 아이템 ID로 추가 (NPC 보상용)
    public void RequestAddItemByID(uint itemID, ushort quantity = 1)
    {
        P_ItemAddRequest request = new P_ItemAddRequest
        {
            itemID = itemID,
            quantity = quantity
        };
        Client.TCP.SendPacket(E_PACKET.ITEM_ADD_REQUEST, request);
        Debug.Log($"아이템 추가 요청: ID={itemID}, 수량={quantity}");
    }

    public void RequestAddItem(uint itemID, ushort quantity)
    {
        P_ItemAddRequest request = new P_ItemAddRequest
        {
            itemID = itemID,
            quantity = quantity
        };
        Client.TCP.SendPacket(E_PACKET.ITEM_ADD_REQUEST, request);
        Debug.Log($"아이템 추가 요청: ID={itemID}, 수량={quantity}");
    }

    public void RequestUseItem(ushort slotIndex)
    {
        P_ItemUseRequest request = new P_ItemUseRequest
        {
            slotIndex = slotIndex
        };
        Client.TCP.SendPacket(E_PACKET.ITEM_USE_REQUEST, request);
        Debug.Log($"아이템 사용 요청: 슬롯={slotIndex}");
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        switch ((E_PACKET)packet.pbase.packet_id)
        {
            case E_PACKET.INVENTORY_INFO_RESPONSE:
                HandleInventoryInfoResponse(packet.data);
                break;

            case E_PACKET.ITEM_ADD_RESPONSE:
                HandleItemAddResponse(packet.data);
                break;

            case E_PACKET.ITEM_USE_RESPONSE:
                HandleItemUseResponse(packet.data);
                break;
        }
    }
    private static string ReadZString(byte[] data, int offset, int maxLen)
    {
        int len = 0;
        for (int i = 0; i < maxLen; i++)
        {
            if (data[offset + i] == 0) break;
            len++;
        }
        return System.Text.Encoding.UTF8.GetString(data, offset, len);
    }

    private void HandleInventoryInfoResponse(byte[] data)
    {
        // payload: Result(2) + itemCount(2) + Item[40]
        ushort result = System.BitConverter.ToUInt16(data, 0);
        ushort itemCount = System.BitConverter.ToUInt16(data, 2);

        // 무조건 전체 슬롯 초기화
        for (int i = 0; i < MAX_INVENTORY_SIZE; i++)
            items[i] = new Item();

        if (result != 0)
        {
            UpdateInventoryUI();
            return;
        }

        const int itemSize = 40;
        int offset = 4;

        // payload 길이랑 itemCount 둘 다 고려해서 안전하게
        int maxReadable = (data.Length - offset) / itemSize;
        int count = Mathf.Min((int)itemCount, Mathf.Min(MAX_INVENTORY_SIZE, maxReadable));

        for (int i = 0; i < count; i++)
        {
            uint id = System.BitConverter.ToUInt32(data, offset + 0);
            ushort typeU16 = System.BitConverter.ToUInt16(data, offset + 4);
            ushort qty = System.BitConverter.ToUInt16(data, offset + 6);
            string name = ReadZString(data, offset + 8, 32);

            // 빈 아이템이면 그냥 빈 슬롯 유지
            if (id == 0 || qty == 0)
            {
                offset += itemSize;
                continue;
            }

            items[i] = new Item
            {
                itemID = id,
                itemType = (ITEM_TYPE)typeU16,
                quantity = qty,
                itemName = name
            };

            offset += itemSize;
        }

        UpdateInventoryUI();
    }

    private void HandleItemAddResponse(byte[] data)
    {
        // payload: Result(2) + Item(40) + slotIndex(2) = 44 bytes
        if (data == null || data.Length < 44)
        {
            Debug.LogWarning("[Inventory] ITEM_ADD_RESPONSE payload too small");
            return;
        }

        ushort result = System.BitConverter.ToUInt16(data, 0);
        ushort slotIndex = System.BitConverter.ToUInt16(data, 42);

        Debug.Log($"[Inventory] ItemAddResponse result={result}, slotIndex={slotIndex}");

        // 성공이면 최신 인벤토리 다시 요청(지금 구조에 가장 안전)
        if (result == 0)
            RequestInventoryInfo();
    }


    private void HandleItemUseResponse(byte[] data)
    {
        Debug.Log("[Inventory] ItemUseResponse");
        RequestInventoryInfo();
    }

    void UpdateInventoryUI()
    {
        if (inventoryUI != null)
        {
            inventoryUI.UpdateInventory(items);
        }
    }

    void OnDestroy()
    {
        Client.TCP.RemovePacketReceiver(this);
    }
}