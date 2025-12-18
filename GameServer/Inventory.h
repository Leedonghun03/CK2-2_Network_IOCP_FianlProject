#pragma once
#include "Packet.h"
#include <vector>
#include <unordered_map>

class Inventory
{
public:
    Inventory() = default;
    ~Inventory() = default;

    void Init(UINT32 maxSlots = MAX_INVENTORY_SIZE)
    {
        mMaxSlots = maxSlots;
        mItems.resize(maxSlots);
    }

    // 아이템 추가
    bool AddItem(UINT32 itemID, ITEM_TYPE itemType, UINT16 quantity, const char* itemName, UINT16& outSlotIndex)
    {
        // 1. 같은 아이템이 있으면 수량 증가
        for (UINT16 i = 0; i < mMaxSlots; ++i)
        {
            if (mItems[i].itemID == itemID && mItems[i].quantity > 0)
            {
                mItems[i].quantity += quantity;
                outSlotIndex = i;
                return true;
            }
        }

        // 2. 빈 슬롯에 추가
        for (UINT16 i = 0; i < mMaxSlots; ++i)
        {
            if (mItems[i].itemID == 0) // 빈 슬롯
            {
                mItems[i].itemID = itemID;
                mItems[i].itemType = itemType;
                mItems[i].quantity = quantity;
                CopyMemory(mItems[i].itemName, itemName, sizeof(mItems[i].itemName));
                outSlotIndex = i;
                return true;
            }
        }

        return false; // 인벤토리 가득 찼음
    }

    // 아이템 사용
    bool UseItem(UINT16 slotIndex, UINT16& outRemainingQuantity)
    {
        if (slotIndex >= mMaxSlots || mItems[slotIndex].quantity == 0)
        {
            return false;
        }

        mItems[slotIndex].quantity--;
        outRemainingQuantity = mItems[slotIndex].quantity;

        // 수량이 0이 되면 슬롯 비우기
        if (mItems[slotIndex].quantity == 0)
        {
            mItems[slotIndex] = Item(); // 초기화
        }

        return true;
    }

    // 아이템 제거
    bool RemoveItem(UINT16 slotIndex, UINT16 quantity)
    {
        if (slotIndex >= mMaxSlots || mItems[slotIndex].quantity < quantity)
        {
            return false;
        }

        mItems[slotIndex].quantity -= quantity;

        if (mItems[slotIndex].quantity == 0)
        {
            mItems[slotIndex] = Item();
        }

        return true;
    }

    // 인벤토리 전체 정보 가져오기
    const std::vector<Item>& GetAllItems() const { return mItems; }

    // 특정 슬롯 아이템 가져오기
    const Item& GetItem(UINT16 slotIndex) const
    {
        static Item emptyItem;
        if (slotIndex >= mMaxSlots)
            return emptyItem;
        return mItems[slotIndex];
    }

private:
    UINT32 mMaxSlots = MAX_INVENTORY_SIZE;
    std::vector<Item> mItems;
};