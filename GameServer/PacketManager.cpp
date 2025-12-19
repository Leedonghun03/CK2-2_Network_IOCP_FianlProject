#include <utility>
#include <cstring>
#include <sstream>
#include <iostream>

#include "UserManager.h"
#include "RoomManager.h"
#include "PacketManager.h"
#include "RedisManager.h"

#include <strsafe.h>


void PacketManager::Init(const UINT32 maxClient_)
{
	mRecvFuntionDictionary = std::unordered_map<int, PROCESS_RECV_PACKET_FUNCTION>();

	mRecvFuntionDictionary[(int)PACKET_ID::SYS_USER_CONNECT] = &PacketManager::ProcessUserConnect;
	mRecvFuntionDictionary[(int)PACKET_ID::SYS_USER_DISCONNECT] = &PacketManager::ProcessUserDisConnect;

	mRecvFuntionDictionary[(int)PACKET_ID::LOGIN_REQUEST] = &PacketManager::ProcessLogin;
	mRecvFuntionDictionary[(int)RedisTaskID::RESPONSE_LOGIN] = &PacketManager::ProcessLoginDBResult;
	mRecvFuntionDictionary[(int)RedisTaskID::RESPONSE_NOTICE] = &PacketManager::ProcessNoticeDBResult;
	
	mRecvFuntionDictionary[(int)PACKET_ID::ROOM_ENTER_REQUEST] = &PacketManager::ProcessEnterRoom;
	mRecvFuntionDictionary[(int)PACKET_ID::ROOM_NEW_USER_NTF] = &PacketManager::ProcessEnterRoomByPlayerJoined;
	mRecvFuntionDictionary[(int)PACKET_ID::ROOM_LEAVE_REQUEST] = &PacketManager::ProcessLeaveRoom;
	mRecvFuntionDictionary[(int)PACKET_ID::ROOM_CHAT_REQUEST] = &PacketManager::ProcessRoomChatMessage;
	mRecvFuntionDictionary[(int)PACKET_ID::PLAYER_MOVEMENT] = &PacketManager::ProcessPlayerMovement;

	// 인벤토리 패킷 핸들러 등록
	mRecvFuntionDictionary[(int)PACKET_ID::INVENTORY_INFO_REQUEST] = &PacketManager::ProcessInventoryInfoRequest;
	mRecvFuntionDictionary[(int)PACKET_ID::ITEM_ADD_REQUEST] = &PacketManager::ProcessItemAddRequest;
	mRecvFuntionDictionary[(int)PACKET_ID::ITEM_USE_REQUEST] = &PacketManager::ProcessItemUseRequest;

	// 퀘스트 패킷 딕셔너리 등록
	mRecvFuntionDictionary[(int)PACKET_ID::QUEST_TALK_REQUEST] = &PacketManager::ProcessQuestTalk;
	mRecvFuntionDictionary[(int)PACKET_ID::QUEST_ACCEPT_REQUEST] = &PacketManager::ProcessQuestAccept;
	mRecvFuntionDictionary[(int)PACKET_ID::QUEST_COMPLETE_REQUEST] = &PacketManager::ProcessQuestComplete;

	// 공격 패킷 등록
	mRecvFuntionDictionary[(int)PACKET_ID::PLAYER_ATTACK_REQUEST] = &PacketManager::ProcessPlayerAttack;
	mRecvFuntionDictionary[(int)PACKET_ID::HIT_REPORT] = &PacketManager::ProcessHitReport;

	CreateCompent(maxClient_);

	mRedisMgr = new RedisManager;// std::make_unique<RedisManager>();
}

void PacketManager::CreateCompent(const UINT32 maxClient_)
{
	mUserManager = new UserManager;
	mUserManager->Init(maxClient_);

		
	UINT32 startRoomNummber = 0;
	UINT32 maxRoomCount = 10;
	UINT32 maxRoomUserCount = 4;
	mRoomManager = new RoomManager;
	mRoomManager->SendPacketFunc = SendPacketFunc;
	mRoomManager->Init(startRoomNummber, maxRoomCount, maxRoomUserCount);
}

bool PacketManager::Run()
{	
	bool redisOK = mRedisMgr->Run("127.0.0.1", 6379, 1);
	if (!redisOK)
	{
		printf("[WARN] Redis connect failed. Continue without redis.\n");
	}

	mIsRunProcessThread = true;
	mProcessThread = std::thread([this]() { ProcessPacket(); });

	return true;
}

void PacketManager::End()
{
	mRedisMgr->End();

	mIsRunProcessThread = false;

	if (mProcessThread.joinable())
	{
		mProcessThread.join();
	}
}

void PacketManager::ClearConnectionInfo(INT32 clientIndex_)
{
	auto pReqUser = mUserManager->GetUserByConnIdx(clientIndex_);

	if (pReqUser->GetDomainState() == User::DOMAIN_STATE::ROOM)
	{
		auto roomNum = pReqUser->GetCurrentRoom();
		mRoomManager->LeaveUser(roomNum, pReqUser);
	}

	if (pReqUser->GetDomainState() != User::DOMAIN_STATE::NONE)
	{
		mUserManager->DeleteUserInfo(pReqUser);
	}
}

void PacketManager::ReceivePacketData(const UINT32 clientIndex_, const UINT32 size_, char* pData_)
{
	auto pUser = mUserManager->GetUserByConnIdx(clientIndex_);
	pUser->SetPacketData(size_, pData_);

	EnqueuePacketData(clientIndex_);
}

void PacketManager::EnqueuePacketData(const UINT32 clientIndex_)
{
	std::lock_guard<std::mutex> guard(mLock);
	mInComingPacketUserIndex.push_back(clientIndex_);
}

PacketInfo PacketManager::DequePacketData()
{
	UINT32 userIndex = 0;

	{
		std::lock_guard<std::mutex> guard(mLock);
		if (mInComingPacketUserIndex.empty())
		{
			return PacketInfo();
		}

		userIndex = mInComingPacketUserIndex.front();
		mInComingPacketUserIndex.pop_front();
	}

	auto pUser = mUserManager->GetUserByConnIdx(userIndex);
	auto packetData = pUser->GetPacket();
	packetData.ClientIndex = userIndex;
	return packetData;
}

void PacketManager::PushSystemPacket(PacketInfo packet_)
{
	std::lock_guard<std::mutex> guard(mLock);
	mSystemPacketQueue.push_back(packet_);
}

PacketInfo PacketManager::DequeSystemPacketData()
{

	std::lock_guard<std::mutex> guard(mLock);
	if (mSystemPacketQueue.empty())
	{
		return PacketInfo();
	}

	auto packetData = mSystemPacketQueue.front();
	mSystemPacketQueue.pop_front();

	return packetData;
}

void PacketManager::RedisReqNotice(User& user, const std::string noticeMsg)
{
	RedisNoticeReq dbReq;
	CopyUserID(dbReq.UserID, "[GM]");
	StringCbCopyA(dbReq.UserID, sizeof(dbReq.UserID), "[GM]");
	StringCbCopyA(dbReq.Message, sizeof(dbReq.Message), noticeMsg.c_str());

	RedisTask task;
	task.UserIndex = user.GetNetConnIdx();
	task.TaskID = RedisTaskID::REQUEST_NOTICE;
	task.DataSize = sizeof(RedisNoticeReq);
	task.pData = new char[task.DataSize];
	CopyMemory(task.pData, (char*)&dbReq, task.DataSize);
	mRedisMgr->PushTask(task);

	printf("[Redis Request] Notice. userUUID(%d), userID(%s), msg:%s\n", user.GetNetConnIdx(), user.GetUserId(), noticeMsg.c_str());
}


void PacketManager::ProcessPacket()
{
	while (mIsRunProcessThread)
	{
		bool isIdle = true;

		if (auto packetData = DequePacketData(); packetData.PacketId > (UINT16)PACKET_ID::SYS_END)
		{
			isIdle = false;
			ProcessRecvPacket(packetData.ClientIndex, packetData.PacketId, packetData.DataSize, packetData.pDataPtr);
		}

		if (auto packetData = DequeSystemPacketData(); packetData.PacketId != 0)
		{
			isIdle = false;
			ProcessRecvPacket(packetData.ClientIndex, packetData.PacketId, packetData.DataSize, packetData.pDataPtr);
		}

		if (auto task = mRedisMgr->TakeResponseTask(); task.TaskID != RedisTaskID::INVALID)
		{
			isIdle = false;
			ProcessRecvPacket(task.UserIndex, (UINT16)task.TaskID, task.DataSize, task.pData);
			task.Release();
		}

		if(isIdle)
		{
			std::this_thread::sleep_for(std::chrono::milliseconds(1));
		}
	}
}

void PacketManager::ProcessRecvPacket(const UINT32 clientIndex_, const UINT16 packetId_, const UINT16 packetSize_, char* pPacket_)
{
	auto iter = mRecvFuntionDictionary.find(packetId_);
	if (iter != mRecvFuntionDictionary.end())
	{
		(this->*(iter->second))(clientIndex_, packetSize_, pPacket_);
	}
	else
	{
		printf("[WARN] No handler. packetId=%u size=%u client=%u\n", packetId_, packetSize_, clientIndex_);
	}
}

void PacketManager::ProcessUserConnect(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	printf("[ProcessUserConnect] clientIndex: %d\n", clientIndex_);
	auto pUser = mUserManager->GetUserByConnIdx(clientIndex_);
	pUser->Clear();
}

void PacketManager::ProcessUserDisConnect(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	printf("[ProcessUserDisConnect] clientIndex: %d\n", clientIndex_);
	ClearConnectionInfo(clientIndex_);
}

void PacketManager::ProcessLogin(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{ 
	if (packetSize_ < PACKET_HEADER_LENGTH)
	{
		return;
	}

	char userId[MAX_USER_ID_LEN + 1] = { 0 };
	char userPw[MAX_USER_PW_LEN + 1] = { 0 };

	// 1) 정상 로그인 패킷(201: LOGIN_REQUEST_PACKET) 크기면 기존 구조로 파싱
	if (packetSize_ == (UINT16)LOGIN_REQUEST_PACKET_SIZE)
	{
		auto pLoginReqPacket = reinterpret_cast<LOGIN_REQUEST_PACKET*>(pPacket_);
		StringCbCopyA(userId, sizeof(userId), pLoginReqPacket->userID);
		StringCbCopyA(userPw, sizeof(userPw), pLoginReqPacket->userPW);
	}
	// 2) Unity 임시 로그인: 201이지만 body에 "이름/ID만" 담아 보낸 경우(가변 길이 허용)
	else
	{
		const UINT16 bodySize = packetSize_ - (UINT16)PACKET_HEADER_LENGTH;
		const UINT16 copyLen = (bodySize > MAX_USER_ID_LEN) ? MAX_USER_ID_LEN : bodySize;

		if (copyLen == 0)
		{
			// 바디가 없으면 응답도 의미 없음
			return;
		}

		CopyMemory(userId, pPacket_ + PACKET_HEADER_LENGTH, copyLen);
		userId[copyLen] = '\0';
		// userPw는 빈 문자열로 둠
	}

	printf("[ProcessLogin] client=%u, userId=%s, packetSize=%u\n", clientIndex_, userId, packetSize_);

	LOGIN_RESPONSE_PACKET loginResPacket;

	// 서버 최대 접속자 체크 (기존 로직 유지)
	if (mUserManager->GetCurrentUserCnt() >= mUserManager->GetMaxUserCnt())
	{
		loginResPacket.Result = (UINT16)ERROR_CODE::LOGIN_USER_USED_ALL_OBJ;
		SendPacketFunc(clientIndex_, sizeof(LOGIN_RESPONSE_PACKET), (char*)&loginResPacket);
		return;
	}

	// 이미 접속중인 ID인지 체크 (동작이 완벽하진 않지만 기존 의도 유지)
	if (mUserManager->FindUserIndexByID(userId) != -1)
	{
		loginResPacket.Result = (UINT16)ERROR_CODE::LOGIN_USER_ALREADY;
		SendPacketFunc(clientIndex_, sizeof(LOGIN_RESPONSE_PACKET), (char*)&loginResPacket);
		return;
	}

	mUserManager->AddUser(userId, clientIndex_);
	mUserManager->IncreaseUserCnt();

	// Unity 대응용: 서버 코드가 원래 Result에 clientIndex_를 넣고 있었음
	loginResPacket.Result = (UINT16)clientIndex_;
	SendPacketFunc(clientIndex_, sizeof(LOGIN_RESPONSE_PACKET), (char*)&loginResPacket);

	printf("[ProcessLogin] Sent 202(LoginResponse). client=%u result=%u\n", clientIndex_, loginResPacket.Result);
}

void PacketManager::ProcessLoginDBResult(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	printf("ProcessLoginDBResult. UserIndex: %d\n", clientIndex_);

	auto pBody = (RedisLoginRes*)pPacket_;

	if (pBody->Result == (UINT16)ERROR_CODE::NONE)
	{
		//로그인 완료로 변경한다
		auto pUser = mUserManager->GetUserByConnIdx(clientIndex_);
		pUser->SetLogin(pBody->UserID);
	}

	LOGIN_RESPONSE_PACKET loginResPacket;
	//loginResPacket.Result = pBody->Result;
	// Unity3D 대응용
	loginResPacket.Result = clientIndex_;
	SendPacketFunc(clientIndex_, sizeof(LOGIN_RESPONSE_PACKET), (char*)&loginResPacket);
}

void PacketManager::ProcessNoticeDBResult(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	printf("ProcessNoticeDBResult. UserIndex: %d\n", clientIndex_);

	auto pBody = (RedisNoticeRes*)pPacket_;

	ROOM_CHAT_NOTIFY_PACKET roomChatNtfyPkt;
	StringCbCopyA(roomChatNtfyPkt.userID, sizeof(roomChatNtfyPkt.userID), "[GM]");
	StringCbCopyA(roomChatNtfyPkt.Msg, sizeof(roomChatNtfyPkt.Msg), pBody->Message);

	mRoomManager->SendToAllUser(roomChatNtfyPkt.PacketLength, (char*)&roomChatNtfyPkt, clientIndex_, false);
}



void PacketManager::ProcessEnterRoom(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	UNREFERENCED_PARAMETER(packetSize_);

	auto pRoomEnterReqPacket = reinterpret_cast<ROOM_ENTER_REQUEST_PACKET*>(pPacket_);
	auto pReqUser = mUserManager->GetUserByConnIdx(clientIndex_);

	if (!pReqUser || pReqUser == nullptr) 
	{
		return;
	}

	auto roomNumber = pRoomEnterReqPacket->RoomNumber;
	
			
	// Room::EnterUser()에서 입장하는 유저에게 방안 유저 리스트를 전송한다
	auto enterResult = mRoomManager->EnterUser(roomNumber, pReqUser);

	{
		ROOM_ENTER_RESPONSE_PACKET roomEnterResPacket;
		roomEnterResPacket.Result = enterResult;
		SendPacketFunc(clientIndex_, sizeof(ROOM_ENTER_RESPONSE_PACKET), (char*)&roomEnterResPacket);
	}
	printf("Response Packet Sended");

	if (enterResult != (UINT16)ERROR_CODE::NONE)
	{
		return;
	}

	auto pRoom = mRoomManager->GetRoomByNumber(roomNumber);

	// 방안 유저들에게 입장하는 유저의 실제 위치와 회전값을 전달
	pRoom->NotifyUserEnter(clientIndex_, pReqUser->GetUserId(), pReqUser->GetPosition(), pReqUser->GetRotation());
}

void PacketManager::ProcessEnterRoomByPlayerJoined(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	auto pReqUser = mUserManager->GetUserByConnIdx((INT32)clientIndex_);
	if (!pReqUser) return;

	// 최소 크기 체크
	if (packetSize_ < sizeof(ROOM_NEW_USER_NTF_PACKET))
	{
		printf("[EnterBy208] invalid packet size. client=%u size=%u\n", clientIndex_, packetSize_);
		return;
	}

	// 클라가 보낸 입장 패킷에서 pos/rot 읽기
	auto joinPkt = reinterpret_cast<ROOM_NEW_USER_NTF_PACKET*>(pPacket_);

	// 서버 유저 상태에 반영 (이게 없으면 기본 0,0,0 그대로 방송됨)
	pReqUser->SetPosition(joinPkt->position);
	pReqUser->SetRotation(joinPkt->rotation);

	const INT32 roomNumber = 0;

	auto enterResult = mRoomManager->EnterUser(roomNumber, pReqUser);
	if (enterResult != (UINT16)ERROR_CODE::NONE)
	{
		printf("[EnterBy208] enter failed. client=%u result=%d\n", clientIndex_, (int)enterResult);
		return;
	}

	auto pRoom = mRoomManager->GetRoomByNumber(roomNumber);
	if (!pRoom) return;

	// 방에 있는 유저들에게 "새 유저 입장(208)" 알림
	pRoom->NotifyUserEnter(clientIndex_, pReqUser->GetUserId(),
		pReqUser->GetPosition(), pReqUser->GetRotation());

	printf("[EnterBy208] client=%u entered room=%d\n", clientIndex_, roomNumber);
}


void PacketManager::ProcessHitReport(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	if (packetSize_ < sizeof(HIT_REPORT_PACKET))
	{
		printf("[HitReport] size mismatch. got=%u expected=%zu\n", packetSize_, sizeof(HIT_REPORT_PACKET));
		return;
	}

	auto* req = reinterpret_cast<HIT_REPORT_PACKET*>(pPacket_);

	auto* user = mUserManager->GetUserByConnIdx((INT32)clientIndex_);
	if (!user) return;

	if (user->GetDomainState() != User::DOMAIN_STATE::ROOM)
	{
		printf("[HitReport] user not in room. client=%u\n", clientIndex_);
		return;
	}

	INT32 roomNum = user->GetCurrentRoom();
	Room* room = mRoomManager->GetRoomByNumber(roomNum);
	if (!room) return;

	printf("[HitReport] client=%u enemy=%lld dmg=%d\n", clientIndex_, req->enemyID, req->damage);

	room->ProcessHitReport((INT64)clientIndex_, req->enemyID, req->damage);
}

void PacketManager::ProcessLeaveRoom(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	UNREFERENCED_PARAMETER(packetSize_);
	UNREFERENCED_PARAMETER(pPacket_);

	ROOM_LEAVE_RESPONSE_PACKET roomLeaveResPacket;

	auto reqUser = mUserManager->GetUserByConnIdx(clientIndex_);
	auto roomNum = reqUser->GetCurrentRoom();
				
	//TODO Room안의 UserList객체의 값 확인하기
	roomLeaveResPacket.Result = mRoomManager->LeaveUser(roomNum, reqUser);
	SendPacketFunc(clientIndex_, sizeof(ROOM_LEAVE_RESPONSE_PACKET), (char*)&roomLeaveResPacket);
}

void PacketManager::ProcessPlayerMovement(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	UNREFERENCED_PARAMETER(packetSize_);
	UNREFERENCED_PARAMETER(pPacket_);

	auto playerMovement = reinterpret_cast<PLAYER_MOVEMENT_PACKET*>(pPacket_);

	if (playerMovement->userUUID != clientIndex_)
	{
		printf("[ProcessPlayerMovement] userUUID(%lld) != clientIndex_(%ld)\n", playerMovement->userUUID, clientIndex_);
		return;
	}


	printf("[ProcessPlayerMovement] userUUID(%lld) dx=%f, dy=%f, rx:%f, ry:%f, rz:%f \n", playerMovement->userUUID, 
		playerMovement->dx, playerMovement->dy, playerMovement->rotation.x, playerMovement->rotation.y, playerMovement->rotation.z);

	auto reqUser = mUserManager->GetUserByConnIdx(clientIndex_);
	auto roomNum = reqUser->GetCurrentRoom();

	auto pRoom = mRoomManager->GetRoomByNumber(roomNum);
	if (pRoom == nullptr)
	{
		printf("[ProcessPlayerMovement] pRoom == nullptr userUUID(%lld), roomNum(%d)\n", playerMovement->userUUID, roomNum);
		return;
	}

	UPDATE_PLAYER_MOVEMENT_PACKET updateMovement;
	updateMovement.player_id = playerMovement->userUUID;
	updateMovement.rotation = playerMovement->rotation;
	// Movement 처리
	updateMovement.motion = reqUser->UpdateMovement(playerMovement->dx, playerMovement->dy, playerMovement->rotation);
	
	pRoom->SendToAllUser(updateMovement.PacketLength, (char*)&updateMovement, clientIndex_, false);
}


void PacketManager::ProcessRoomChatMessage(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	UNREFERENCED_PARAMETER(packetSize_);

	auto pRoomChatReqPacketet = reinterpret_cast<ROOM_CHAT_REQUEST_PACKET*>(pPacket_);
		
	ROOM_CHAT_RESPONSE_PACKET roomChatResPacket;
	roomChatResPacket.Result = (INT16)ERROR_CODE::NONE;

	auto reqUser = mUserManager->GetUserByConnIdx(clientIndex_);
	auto roomNum = reqUser->GetCurrentRoom();

	auto pRoom = mRoomManager->GetRoomByNumber(roomNum);
	if (pRoom == nullptr)
	{
		roomChatResPacket.Result = (INT16)ERROR_CODE::CHAT_ROOM_INVALID_ROOM_NUMBER;
		SendPacketFunc(clientIndex_, sizeof(ROOM_CHAT_RESPONSE_PACKET), (char*)&roomChatResPacket);
		return;
	}

	// 특수 명령 "/c"
	const std::string cmdMessage = pRoomChatReqPacketet->Message;
	if (cmdMessage.find("/c", 0) == 0)
	{
		// Npc를 생성한다
		pRoom->EnterNpc();
		return;
	}

	// 공지 "/n"
	//const std::string cmdMessage = pRoomChatReqPacketet->Message;
	if (cmdMessage.find("/n", 0) == 0)
	{
		// 앞에 "/n"로 시작하는 부분을 잘라낸다
		const std::string noticeMsg = cmdMessage.substr(2);
		RedisReqNotice(*reqUser, noticeMsg);
		return;
	}

	// 길찾기
	if (cmdMessage.find("/p", 0) == 0)
	{
		// 앞에 "/p"로 시작하는 부분을 잘라낸다
		const std::string endPosStr = cmdMessage.substr(2);
		TempFindPath(endPosStr, *reqUser, *pRoom);
		return;
	}
		
	SendPacketFunc(clientIndex_, sizeof(ROOM_CHAT_RESPONSE_PACKET), (char*)&roomChatResPacket);

	pRoom->NotifyChat(clientIndex_, reqUser->GetUserId().c_str(), pRoomChatReqPacketet->Message);		
}

// ================= 인벤토리 =========================
void PacketManager::ProcessInventoryInfoRequest(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	auto pUser = mUserManager->GetUserByConnIdx(clientIndex_);
	if (!pUser) return;

	INVENTORY_INFO_RESPONSE_PACKET response;
	response.Result = (UINT16)ERROR_CODE::NONE;

	const auto& items = pUser->GetInventory().GetAllItems();
	response.itemCount = 0;

	for (UINT16 i = 0; i < items.size() && i < MAX_INVENTORY_SIZE; ++i)
	{
		if (items[i].itemID != 0)
		{
			response.items[response.itemCount++] = items[i];
		}
	}

	SendPacketFunc(clientIndex_, sizeof(INVENTORY_INFO_RESPONSE_PACKET), (char*)&response);
}

void PacketManager::ProcessItemAddRequest(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	auto pRequest = reinterpret_cast<ITEM_ADD_REQUEST_PACKET*>(pPacket_);
	auto pUser = mUserManager->GetUserByConnIdx(clientIndex_);
	if (!pUser) return;

	ITEM_ADD_RESPONSE_PACKET response;

	// TODO: 아이템 ID로 아이템 정보를 가져오는 로직 (아이템 테이블 필요)
	// 여기서는 예시로 하드코딩
	UINT16 slotIndex = 0;
	bool success = pUser->GetInventory().AddItem(
		pRequest->itemID,
		ITEM_TYPE::POTION,
		pRequest->quantity,
		"Health Potion",
		slotIndex
	);

	if (success)
	{
		response.Result = (UINT16)ERROR_CODE::NONE;
		response.slotIndex = slotIndex;
		response.addedItem = pUser->GetInventory().GetItem(slotIndex);
	}
	else
	{
		response.Result = (UINT16)ERROR_CODE::INVENTORY_FULL;
	}

	SendPacketFunc(clientIndex_, sizeof(ITEM_ADD_RESPONSE_PACKET), (char*)&response);
}

void PacketManager::ProcessItemUseRequest(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	auto pRequest = reinterpret_cast<ITEM_USE_REQUEST_PACKET*>(pPacket_);
	auto pUser = mUserManager->GetUserByConnIdx(clientIndex_);
	if (!pUser) return;

	ITEM_USE_RESPONSE_PACKET response;
	UINT16 remainingQuantity = 0;

	bool success = pUser->GetInventory().UseItem(pRequest->slotIndex, remainingQuantity);

	if (success)
	{
		response.Result = (UINT16)ERROR_CODE::NONE;
		response.slotIndex = pRequest->slotIndex;
		response.remainingQuantity = remainingQuantity;

		// TODO: 아이템 효과 적용 (체력 회복 등)
	}
	else
	{
		response.Result = (UINT16)ERROR_CODE::ITEM_USE_FAILED;
	}

	SendPacketFunc(clientIndex_, sizeof(ITEM_USE_RESPONSE_PACKET), (char*)&response);
}
// =================================================

// ====================== Quest =====================
void PacketManager::ProcessQuestTalk(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	User* pUser = mUserManager->GetUserByConnIdx((INT32)clientIndex_);
	if (!pUser) return;

	auto* pReq = reinterpret_cast<QUEST_TALK_REQUEST_PACKET*>(pPacket_);

	QUEST_TALK_RESPONSE_PACKET res;
	res.npc_id = pReq->npc_id;
	res.quest_id = 1;
	res.state = (UINT8)pUser->GetQuestState();   // 0/1/2가 Unity와 동일해야 함
	res.current = 0;
	res.required = 1;

	strncpy_s(res.title, "1. Monster", MAX_QUEST_TITLE_LEN - 1);
	strncpy_s(res.desc, "Eliminate One Monster", MAX_QUEST_DESC_LEN - 1);

	res.rewardItemID = 1001; // 예시: 포션 아이템 ID
	res.rewardQty = 1;

	SendPacketFunc(clientIndex_, sizeof(res), (char*)&res);
}

void PacketManager::ProcessQuestAccept(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	User* pUser = mUserManager->GetUserByConnIdx((INT32)clientIndex_);
	if (!pUser) return;

	auto* pReq = reinterpret_cast<QUEST_ACCEPT_REQUEST_PACKET*>(pPacket_);

	QUEST_ACCEPT_RESPONSE_PACKET res;
	res.quest_id = pReq->quest_id;

	// 이미 수락/진행 중이면 실패
	if (pUser->GetQuestState() != QUEST_STATE::NOT_ACCEPTED)
	{
		res.result = 0;
		res.state = (UINT8)pUser->GetQuestState();
		res.current = 0;
		res.required = 1;
		SendPacketFunc(clientIndex_, sizeof(res), (char*)&res);
		return;
	}

	// 유저 상태 변경
	pUser->SetQuestState(QUEST_STATE::IN_PROGRESS);

	// 응답 구성
	res.result = 1; // Unity는 1이면 성공 처리
	res.state = (UINT8)pUser->GetQuestState();
	res.current = 0;
	res.required = 1;

	// Room에 퀘스트 진행 데이터 생성/저장
	{
		INT32 roomNum = pUser->GetCurrentRoom();
		Room* room = mRoomManager->GetRoomByNumber(roomNum);
		if (room)
		{
			// Room.h에 추가한 함수
			room->SetQuestAccepted((INT64)clientIndex_, (INT32)pReq->quest_id, (UINT16)res.required);
		}
		else
		{
			printf("[QuestAccept] room not found. client=%u room=%d\n", clientIndex_, roomNum);
		}
	}

	// 응답 전송
	SendPacketFunc(clientIndex_, sizeof(res), (char*)&res);
}

void PacketManager::ProcessQuestComplete(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	UNREFERENCED_PARAMETER(packetSize_);

	User* pUser = mUserManager->GetUserByConnIdx((INT32)clientIndex_);
	if (!pUser) return;

	auto pReq = reinterpret_cast<QUEST_COMPLETE_REQUEST_PACKET*>(pPacket_);

	QUEST_COMPLETE_RESPONSE_PACKET res;
	res.QuestId = pReq->QuestId;

	if (pUser->GetQuestState() == QUEST_STATE::NOT_ACCEPTED)
	{
		res.Result = (UINT16)ERROR_CODE::QUEST_NOT_ACCEPTED;
		res.State = (UINT8)pUser->GetQuestState();
		SendPacketFunc(clientIndex_, sizeof(res), (char*)&res);
		return;
	}

	if (pUser->GetQuestState() == QUEST_STATE::COMPLETED)
	{
		res.Result = (UINT16)ERROR_CODE::QUEST_ALREADY_COMPLETED;
		res.State = (UINT8)pUser->GetQuestState();
		SendPacketFunc(clientIndex_, sizeof(res), (char*)&res);
		return;
	}

	pUser->SetQuestState(QUEST_STATE::COMPLETED);

	res.Result = (UINT16)ERROR_CODE::NONE;
	res.State = (UINT8)pUser->GetQuestState();
	SendPacketFunc(clientIndex_, sizeof(res), (char*)&res);
}
// =================================================

// ====================== Attack =====================
void PacketManager::ProcessPlayerAttack(UINT32 clientIndex_, UINT16 packetSize_, char* pPacket_)
{
	auto pAttackPacket = reinterpret_cast<PLAYER_ATTACK_REQUEST_PACKET*>(pPacket_);
	auto pUser = mUserManager->GetUserByConnIdx(clientIndex_);

	if (!pUser)
	{
		printf("[ProcessPlayerAttack] User not found: %d\n", clientIndex_);
		return;
	}

	auto roomNum = pUser->GetCurrentRoom();
	auto pRoom = mRoomManager->GetRoomByNumber(roomNum);

	if (!pRoom)
	{
		printf("[ProcessPlayerAttack] Room not found: %d\n", roomNum);
		return;
	}

	printf("[Attack] Player %lld attacking at (%.1f, %.1f, %.1f) dir(%.2f, %.2f, %.2f)\n",
		(INT64)clientIndex_,
		pAttackPacket->attackPosition.x,
		pAttackPacket->attackPosition.y,
		pAttackPacket->attackPosition.z,
		pAttackPacket->attackDirection.x,
		pAttackPacket->attackDirection.y,
		pAttackPacket->attackDirection.z);

	// 방에서 공격 처리
	pRoom->ProcessPlayerAttack((INT64)clientIndex_,
		pAttackPacket->attackPosition,
		pAttackPacket->attackDirection);
}
// =================================================

void PacketManager::TempFindPath(const std::string& endPosStr, User& user, Room& room)
{

	printf("[TempFindPath] userUUID(%lld) pos(%f,%f,%f), endPos(%s)\n", user.GetNetConnIdx(),
		user.GetPosition().x, user.GetPosition().y, user.GetPosition().z, endPosStr.c_str());

	Vector3 end = stringToVector3(endPosStr);
	std::vector<Vector3> path = room.FindPath(user.GetPosition(), end);

	MOVE_PATH_RESPONSE_PACKET movePathResponse;
	movePathResponse.userUUID = user.GetNetConnIdx();
	movePathResponse.pathCount = path.size() > 10 ? 10 : path.size();

	for (int i = 0; i < path.size() && i < 10; ++i)
	{
		movePathResponse.path[i] = path[i];
		printf("[TempFindPath] path[%i](%f,%f,%f)\n", i, path[i].x, path[i].y, path[i].z);
	}

	// Send
	room.SendToAllUser(movePathResponse.PacketLength, (char*)&movePathResponse, user.GetNetConnIdx(), false);
}



Vector3 stringToVector3(const std::string& s) {
	std::stringstream ss(s);
	char discardChar; // To consume parentheses and commas
	float x, y, z;

	// Expected format: "x, y, z"
	ss >> x >> discardChar >> y >> discardChar >> z;

	if (ss.fail()) {
		std::cerr << "Error parsing Vector3 string: " << s << std::endl;
		return Vector3(); // Return a default Vector3 or throw an exception
	}
	return Vector3{ x, y, z };
}