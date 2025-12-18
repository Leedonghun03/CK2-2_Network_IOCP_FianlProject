using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour, IPacketReceiver
{
    private const int MAX_INVENTORY_SIZE = 40;
    private List<Item> items = new List<Item>();

    void Start()
    {
        Client.TCP.AddPacketReceiver(this);

        // 인벤토리 정보 요청
        RequestInventoryInfo();
    }

    public void RequestInventoryInfo()
    {
        P_InventoryInfoRequest request = new P_InventoryInfoRequest();
        Client.TCP.SendPacket(E_PACKET.INVENTORY_INFO_REQUEST, request);
    }

    public void RequestAddItem(uint itemID, ushort quantity)
    {
        P_ItemAddRequest request = new P_ItemAddRequest
        {
            itemID = itemID,
            quantity = quantity
        };
        Client.TCP.SendPacket(E_PACKET.ITEM_ADD_REQUEST, request);
    }

    public void RequestUseItem(ushort slotIndex)
    {
        P_ItemUseRequest request = new P_ItemUseRequest
        {
            slotIndex = slotIndex
        };
        Client.TCP.SendPacket(E_PACKET.ITEM_USE_REQUEST, request);
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        byte packetId = packet.pbase.packet_id;

        switch ((E_PACKET)packetId)
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

    private void HandleInventoryInfoResponse(byte[] data)
    {
        // TODO: 구조체 역직렬화 및 UI 업데이트
        Debug.Log("Inventory info received");
    }

    private void HandleItemAddResponse(byte[] data)
    {
        // TODO: 아이템 추가 UI 업데이트
        Debug.Log("Item added");
    }

    private void HandleItemUseResponse(byte[] data)
    {
        // TODO: 아이템 사용 처리
        Debug.Log("Item used");
    }

    void OnDestroy()
    {
        Client.TCP.RemovePacketReceiver(this);
    }
}