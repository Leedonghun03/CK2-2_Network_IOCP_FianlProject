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
        ushort packetId = packet.pbase.packet_id;

        switch ((E_PACKET)packetId)
        {
            case E_PACKET.INVENTORY_INFO_RESPONSE:
                // TODO: 구조체 파싱 필요
                Debug.Log("인벤토리 정보 수신");
                UpdateInventoryUI();
                break;

            case E_PACKET.ITEM_ADD_RESPONSE:
                Debug.Log("아이템 추가 완료");
                RequestInventoryInfo(); // 갱신
                break;

            case E_PACKET.ITEM_USE_RESPONSE:
                Debug.Log("아이템 사용 완료");
                RequestInventoryInfo(); // 갱신
                break;
        }
    }
    private void HandleInventoryInfoResponse(byte[] data)
    {
        // TODO: 실제 패킷 파싱
        Debug.Log("인벤토리 정보 수신");

        // 임시 테스트 데이터
        // items[0] = ItemDatabase.GetItem(1001); // Health Potion

        UpdateInventoryUI();
    }

    private void HandleItemAddResponse(byte[] data)
    {
        Debug.Log("아이템 추가 완료");
        RequestInventoryInfo(); // 갱신
    }

    private void HandleItemUseResponse(byte[] data)
    {
        Debug.Log("아이템 사용 완료");
        RequestInventoryInfo(); // 갱신
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