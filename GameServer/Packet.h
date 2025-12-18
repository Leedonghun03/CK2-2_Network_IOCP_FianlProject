#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "unity.h"

struct RawPacketData
{
	UINT32 ClientIndex = 0;
	UINT32 DataSize = 0;
	char* pPacketData = nullptr;

	void Set(RawPacketData& vlaue)
	{
		ClientIndex = vlaue.ClientIndex;
		DataSize = vlaue.DataSize;

		pPacketData = new char[vlaue.DataSize];
		CopyMemory(pPacketData, vlaue.pPacketData, vlaue.DataSize);
	}

	void Set(UINT32 clientIndex_, UINT32 dataSize_, char* pData)
	{
		ClientIndex = clientIndex_;
		DataSize = dataSize_;

		pPacketData = new char[dataSize_];
		CopyMemory(pPacketData, pData, dataSize_);
	}

	void Release()
	{
		delete pPacketData;
	}
};


struct PacketInfo
{
	UINT32 ClientIndex = 0;
	UINT16 PacketId = 0;
	UINT16 DataSize = 0;
	char* pDataPtr = nullptr;
};

// ================= 이벤토리 =========================

enum class ITEM_TYPE : UINT16
{
	NONE = 0,
	WEAPON = 1,      // 무기
	ARMOR = 2,       // 방어구
	POTION = 3,      // 포션
	MATERIAL = 4,    // 재료
	QUEST = 5        // 퀘스트 아이템
};

struct Item
{
	UINT32 itemID;           // 아이템 고유 ID
	ITEM_TYPE itemType;      // 아이템 타입
	UINT16 quantity;         // 수량
	char itemName[32];       // 아이템 이름
};

const UINT32 MAX_INVENTORY_SIZE = 40; // 인벤토리 최대 슬롯

enum class QUEST_STATE : UINT8
{
	NOT_ACCEPTED = 0,
	IN_PROGRESS = 1,
	COMPLETED = 2, // 완료 버튼 눌렀음(제출)
};

// ====================================================

enum class  PACKET_ID : UINT16
{
	//SYSTEM
	SYS_USER_CONNECT = 11,
	SYS_USER_DISCONNECT = 12,
	SYS_END = 30,

	//DB
	DB_END = 199,

	//Client
	LOGIN_REQUEST = 201,
	LOGIN_RESPONSE = 202,

	// Enter
	ROOM_ENTER_REQUEST = 206,
	ROOM_ENTER_RESPONSE = 207,
	ROOM_NEW_USER_NTF = 208, // 입장하는 유저에게도 전송
	ROOM_USER_INFO_NTF = 209, // Zone에 있던 유저 정보 (입장하는 유저에게만 보냄)

	// Leave
	ROOM_LEAVE_REQUEST = 215,
	ROOM_LEAVE_RESPONSE = 216,
	ROOM_LEAVE_USER_NTF = 217,

	// Move
	PLAYER_MOVEMENT,
	UPDATE_PLAYER_MOVEMENT,

	ROOM_CHAT_REQUEST = 221,
	ROOM_CHAT_RESPONSE = 222,
	ROOM_CHAT_NOTIFY = 223,

	// Path
	MOVE_PATH_REQUEST = 225,
	MOVE_PATH_RESPONSE = 226,
	MOVE_PATH_NOTIFY = 227,

	// Inventory
	INVENTORY_INFO_REQUEST = 301,      // 인벤토리 정보 요청
	INVENTORY_INFO_RESPONSE = 302,     // 인벤토리 정보 응답

	ITEM_ADD_REQUEST = 303,            // 아이템 추가 요청
	ITEM_ADD_RESPONSE = 304,           // 아이템 추가 응답
	ITEM_ADD_NOTIFY = 305,             // 아이템 추가 알림 (다른 유저에게)

	ITEM_USE_REQUEST = 306,            // 아이템 사용 요청
	ITEM_USE_RESPONSE = 307,           // 아이템 사용 응답

	ITEM_DROP_REQUEST = 308,           // 아이템 버리기 요청
	ITEM_DROP_RESPONSE = 309,          // 아이템 버리기 응답

	ITEM_MOVE_REQUEST = 310,           // 아이템 이동 (슬롯 변경)
	ITEM_MOVE_RESPONSE = 311,          // 아이템 이동 응답

	// Quest
	QUEST_TALK_REQUEST = 601,
	QUEST_TALK_RESPONSE = 602,
	QUEST_ACCEPT_REQUEST = 603,
	QUEST_ACCEPT_RESPONSE = 604,
	QUEST_COMPLETE_REQUEST = 605,
	QUEST_COMPLETE_RESPONSE = 606,
};

#pragma pack(push,1)
struct PACKET_HEADER
{
	const UINT16 PacketLength;
	const UINT16 PacketId;
	const UINT8 Type = 0; //압축여부 암호화여부 등 속성을 알아내는 값
	PACKET_HEADER(UINT16 PacketLength, PACKET_ID PacketId, UINT8 Type = 0) : PacketLength{ PacketLength }, PacketId{ (UINT16)PacketId }, Type{ Type }
	{
	}
};
const UINT32 PACKET_HEADER_LENGTH = sizeof(PACKET_HEADER);

//- 로그인 요청
const int MAX_USER_ID_LEN = 32;
const int MAX_USER_PW_LEN = 32;

struct LOGIN_REQUEST_PACKET : public PACKET_HEADER
{
	char userID[MAX_USER_ID_LEN + 1];
	char userPW[MAX_USER_PW_LEN + 1];

	LOGIN_REQUEST_PACKET() : userID{ 0, }, PACKET_HEADER(sizeof(*this), PACKET_ID::LOGIN_REQUEST) {}
};
const size_t LOGIN_REQUEST_PACKET_SIZE = sizeof(LOGIN_REQUEST_PACKET);


struct LOGIN_RESPONSE_PACKET : public PACKET_HEADER
{
	UINT16 Result;

	LOGIN_RESPONSE_PACKET() : Result{ 0 }, PACKET_HEADER(sizeof(*this), PACKET_ID::LOGIN_RESPONSE) {}
};


//- 룸에 들어가기 요청
//const int MAX_ROOM_TITLE_SIZE = 32;
struct ROOM_ENTER_REQUEST_PACKET : public PACKET_HEADER
{
	INT32 RoomNumber;
	ROOM_ENTER_REQUEST_PACKET() : RoomNumber{ 0 },PACKET_HEADER(sizeof(*this), PACKET_ID::ROOM_ENTER_REQUEST) {}
};

struct ROOM_ENTER_RESPONSE_PACKET : public PACKET_HEADER
{
	INT16 Result;
	//char RivaluserID[MAX_USER_ID_LEN + 1] = { 0, };
	ROOM_ENTER_RESPONSE_PACKET() : Result{ 0 }, PACKET_HEADER(sizeof(*this), PACKET_ID::ROOM_ENTER_RESPONSE) {}
};


struct ROOM_NEW_USER_NTF_PACKET : public PACKET_HEADER
{
	INT64 userUUID;
	char userID[MAX_USER_ID_LEN + 1];

	ROOM_NEW_USER_NTF_PACKET(): PACKET_HEADER(sizeof(*this), PACKET_ID::ROOM_NEW_USER_NTF) {}
};

struct ROOM_USER_INFO_NTF_PACKET : public PACKET_HEADER
{
	
	INT64 userUUID;
	char userID[MAX_USER_ID_LEN + 1];
	Vector3 position;
	Quaternion rotation;

	ROOM_USER_INFO_NTF_PACKET() : PACKET_HEADER(sizeof(*this), PACKET_ID::ROOM_USER_INFO_NTF) {}
};

struct PLAYER_MOVEMENT_PACKET : public PACKET_HEADER
{
	INT64 userUUID;
	float dx;
	float dy;
	Quaternion rotation;

	PLAYER_MOVEMENT_PACKET() : PACKET_HEADER(sizeof(*this), PACKET_ID::PLAYER_MOVEMENT) {}
};


struct UPDATE_PLAYER_MOVEMENT_PACKET : public PACKET_HEADER
{
	INT64 userUUID;
	Quaternion rotation;
	Vector3 motion;

	UPDATE_PLAYER_MOVEMENT_PACKET() : PACKET_HEADER(sizeof(*this), PACKET_ID::UPDATE_PLAYER_MOVEMENT) {}
};


//- 룸 나가기 요청
struct ROOM_LEAVE_REQUEST_PACKET : public PACKET_HEADER
{
	ROOM_LEAVE_REQUEST_PACKET() : PACKET_HEADER(sizeof(*this), PACKET_ID::ROOM_LEAVE_REQUEST) {}
};


struct ROOM_LEAVE_RESPONSE_PACKET : public PACKET_HEADER
{
	INT16 Result;
	ROOM_LEAVE_RESPONSE_PACKET() : PACKET_HEADER(sizeof(*this), PACKET_ID::ROOM_LEAVE_RESPONSE) {}
};


struct ROOM_LEAVE_USER_NTF_PACKET : public PACKET_HEADER
{
	INT64 userUUID;
	char userID[MAX_USER_ID_LEN + 1];
	ROOM_LEAVE_USER_NTF_PACKET() : PACKET_HEADER(sizeof(*this), PACKET_ID::ROOM_LEAVE_USER_NTF) {}
};


// 룸 채팅
const static int MAX_CHAT_MSG_SIZE = 256;
struct ROOM_CHAT_REQUEST_PACKET : public PACKET_HEADER
{
	char Message[MAX_CHAT_MSG_SIZE + 1] = { 0, };

	ROOM_CHAT_REQUEST_PACKET() : PACKET_HEADER(sizeof(*this), PACKET_ID::ROOM_CHAT_REQUEST) {}
};


struct ROOM_CHAT_RESPONSE_PACKET : public PACKET_HEADER
{
	INT16 Result;

	ROOM_CHAT_RESPONSE_PACKET() : Result{ 0 }, PACKET_HEADER(sizeof(*this), PACKET_ID::ROOM_CHAT_RESPONSE) {}
};


struct ROOM_CHAT_NOTIFY_PACKET : public PACKET_HEADER
{
	char userID[MAX_USER_ID_LEN + 1] = { 0, };
	char Msg[MAX_CHAT_MSG_SIZE + 1] = { 0, };

	ROOM_CHAT_NOTIFY_PACKET() : PACKET_HEADER(sizeof(*this), PACKET_ID::ROOM_CHAT_NOTIFY) {}
};


struct MOVE_PATH_REQUEST_PACKET : public PACKET_HEADER
{
	INT64 userUUID;
	Vector3 startPos;
	Vector3 endPos;

	MOVE_PATH_REQUEST_PACKET() : PACKET_HEADER(sizeof(*this), PACKET_ID::MOVE_PATH_REQUEST) {}
};


struct MOVE_PATH_RESPONSE_PACKET : public PACKET_HEADER
{
	INT64 userUUID;
	Vector3 path[10];
	INT16 pathCount;

	MOVE_PATH_RESPONSE_PACKET() : PACKET_HEADER(sizeof(*this), PACKET_ID::MOVE_PATH_RESPONSE) {}
};

// ================= 이벤토리 =========================
// 인벤토리 정보 요청
struct INVENTORY_INFO_REQUEST_PACKET : public PACKET_HEADER
{
	INVENTORY_INFO_REQUEST_PACKET()
		: PACKET_HEADER(sizeof(*this), PACKET_ID::INVENTORY_INFO_REQUEST) {
	}
};

// 인벤토리 정보 응답
struct INVENTORY_INFO_RESPONSE_PACKET : public PACKET_HEADER
{
	UINT16 Result;
	UINT16 itemCount;
	Item items[MAX_INVENTORY_SIZE];

	INVENTORY_INFO_RESPONSE_PACKET()
		: Result(0), itemCount(0),
		PACKET_HEADER(sizeof(*this), PACKET_ID::INVENTORY_INFO_RESPONSE) {
	}
};

// 아이템 추가 요청
struct ITEM_ADD_REQUEST_PACKET : public PACKET_HEADER
{
	UINT32 itemID;
	UINT16 quantity;

	ITEM_ADD_REQUEST_PACKET()
		: itemID(0), quantity(0),
		PACKET_HEADER(sizeof(*this), PACKET_ID::ITEM_ADD_REQUEST) {
	}
};

// 아이템 추가 응답
struct ITEM_ADD_RESPONSE_PACKET : public PACKET_HEADER
{
	UINT16 Result;
	Item addedItem;
	UINT16 slotIndex;  // 추가된 슬롯 인덱스

	ITEM_ADD_RESPONSE_PACKET()
		: Result(0), slotIndex(0),
		PACKET_HEADER(sizeof(*this), PACKET_ID::ITEM_ADD_RESPONSE) {
	}
};

// 아이템 사용 요청
struct ITEM_USE_REQUEST_PACKET : public PACKET_HEADER
{
	UINT16 slotIndex;

	ITEM_USE_REQUEST_PACKET()
		: slotIndex(0),
		PACKET_HEADER(sizeof(*this), PACKET_ID::ITEM_USE_REQUEST) {
	}
};

// 아이템 사용 응답
struct ITEM_USE_RESPONSE_PACKET : public PACKET_HEADER
{
	UINT16 Result;
	UINT16 slotIndex;
	UINT16 remainingQuantity;

	ITEM_USE_RESPONSE_PACKET()
		: Result(0), slotIndex(0), remainingQuantity(0),
		PACKET_HEADER(sizeof(*this), PACKET_ID::ITEM_USE_RESPONSE) {
	}
};

// ===================== Quest =========================

const int MAX_QUEST_TITLE_LEN = 32;
const int MAX_QUEST_DESC_LEN = 64;

struct QUEST_TALK_REQUEST_PACKET : public PACKET_HEADER
{
	INT32 NpcId;
	QUEST_TALK_REQUEST_PACKET()
		: NpcId{ 0 }, PACKET_HEADER(sizeof(*this), PACKET_ID::QUEST_TALK_REQUEST) {
	}
};

struct QUEST_TALK_RESPONSE_PACKET : public PACKET_HEADER
{
	UINT16 Result;

	INT32 NpcId;
	INT32 QuestId;

	UINT8 State;      // QUEST_STATE
	UINT16 Current;   // 0/1
	UINT16 Required;  // 1

	char Title[MAX_QUEST_TITLE_LEN + 1];
	char Desc[MAX_QUEST_DESC_LEN + 1];

	QUEST_TALK_RESPONSE_PACKET()
		: Result{ 0 }, NpcId{ 0 }, QuestId{ 1 }, State{ (UINT8)QUEST_STATE::NOT_ACCEPTED },
		Current{ 0 }, Required{ 1 },
		Title{ 0, }, Desc{ 0, },
		PACKET_HEADER(sizeof(*this), PACKET_ID::QUEST_TALK_RESPONSE) {
	}
};

struct QUEST_ACCEPT_REQUEST_PACKET : public PACKET_HEADER
{
	INT32 QuestId;
	QUEST_ACCEPT_REQUEST_PACKET()
		: QuestId{ 1 }, PACKET_HEADER(sizeof(*this), PACKET_ID::QUEST_ACCEPT_REQUEST) {
	}
};

struct QUEST_ACCEPT_RESPONSE_PACKET : public PACKET_HEADER
{
	UINT16 Result; // 0=성공, 그 외=실패 코드
	INT32 QuestId;
	UINT8 State;

	QUEST_ACCEPT_RESPONSE_PACKET()
		: Result{ 0 }, QuestId{ 1 }, State{ (UINT8)QUEST_STATE::NOT_ACCEPTED },
		PACKET_HEADER(sizeof(*this), PACKET_ID::QUEST_ACCEPT_RESPONSE) {
	}
};

struct QUEST_COMPLETE_REQUEST_PACKET : public PACKET_HEADER
{
	INT32 QuestId;
	QUEST_COMPLETE_REQUEST_PACKET()
		: QuestId{ 1 }, PACKET_HEADER(sizeof(*this), PACKET_ID::QUEST_COMPLETE_REQUEST) {
	}
};

struct QUEST_COMPLETE_RESPONSE_PACKET : public PACKET_HEADER
{
	UINT16 Result; // 0=성공, 그 외=실패 코드
	INT32 QuestId;
	UINT8 State;

	QUEST_COMPLETE_RESPONSE_PACKET()
		: Result{ 0 }, QuestId{ 1 }, State{ (UINT8)QUEST_STATE::NOT_ACCEPTED },
		PACKET_HEADER(sizeof(*this), PACKET_ID::QUEST_COMPLETE_RESPONSE) {
	}
};


#pragma pack(pop) //위에 설정된 패킹설정이 사라짐

