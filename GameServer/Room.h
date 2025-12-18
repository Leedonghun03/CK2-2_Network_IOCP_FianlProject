#pragma once

#include "Npc.h"
#include "UserManager.h"
#include "Packet.h"
#include "NavMeshManager.h"
#include "Enemy.h"
#include "EnemySpawner.h"

#include <functional>
#include <unordered_map>
#include <thread>

void CopyUserID(char* userID, const Actor& user);
void CopyUserID(char* userID, const std::string& userID_);
void CopyUserID(char* userID, const char* userID_);

class Room 
{
public:
	Room() = default;
	~Room()
	{
		mIsRunning = false;
		if (mUpdateThread.joinable())
		{
			mUpdateThread.join();
		}

		// 적 정리
		for (auto& pair : mEnemies)
		{
			delete pair.second;
		}
		mEnemies.clear();

		// 스포너 정리
		for (auto spawner : mSpawners)
		{
			delete spawner;
		}
		mSpawners.clear();
	}

	INT32 GetMaxUserCount() { return mMaxUserCount; }

	INT32 GetCurrentUserCount() { return mCurrentUserCount; }

	INT32 GetRoomNumber() { return mRoomNum; }


	void Init(const INT32 roomNum_, const INT32 maxUserCount_, const std::string& navMeshFileName)
	{
		mRoomNum = roomNum_;
		mMaxUserCount = maxUserCount_;
		//InitNavMesh(navMeshFileName);

		// 스포너 생성
		CreateSpawners();

		// 초기 적 스폰
		SpawnInitialEnemies();

		// 업데이트 스레드 시작
		mIsRunning = true;
		mUpdateThread = std::thread([this]() { UpdateLoop(); });
	}

	void InitNavMesh(const std::string& navMeshFileName)
	{
		if (navMeshManager.LoadNavMesh(navMeshFileName.c_str())) {
			std::cout << "NavMesh Loaded!" << std::endl;

			// Unity에서 보낸 좌표라고 가정
			Vector3 start = { 10.5f, 0.0f, -5.2f };
			Vector3 end = { 42.1f, 0.0f, 15.8f };

			std::vector<Vector3> path = navMeshManager.FindPath(start, end);

			std::cout << "Path Found: " << path.size() << " points." << std::endl;
			for (const auto& p : path) {
				std::cout << "(" << p.x << ", " << p.y << ", " << p.z << ")" << std::endl;
			}
		}
		else {
			std::cout << "Failed to load NavMesh." << std::endl;
		}
	}

	std::vector<Vector3> FindPath(const Vector3& start, const Vector3& end)
	{
		return navMeshManager.FindPath(start, end);
	}

    // 스포너 생성 (5개)
    void CreateSpawners()
    {
        // 스포너 위치 정의
        Vector3 spawnerPositions[] = {
            { 19.0f, 4.2f, 60.0f },
            { 19.0f, 4.2f, 70.0f },
            { 25.0f, 4.2f, 55.0f },
            { 25.0f, 4.2f, 75.0f },
            { 25.0f, 4.2f, 65.0f }
        };

        ENEMY_TYPE spawnerTypes[] = {
            ENEMY_TYPE::SLIME,
            ENEMY_TYPE::SLIME,
            ENEMY_TYPE::GOBLIN,
            ENEMY_TYPE::GOBLIN,
            ENEMY_TYPE::WOLF
        };

        for (int i = 0; i < 5; ++i)
        {
            INT64 spawnerID = (INT64)mRoomNum * 1000 + i;

            EnemySpawner* spawner = new EnemySpawner();
            spawner->Init(spawnerID, spawnerPositions[i], spawnerTypes[i], 30.0f); // 30초 리스폰

            mSpawners.push_back(spawner);

            printf("[Room %d] Spawner created: ID=%lld, Type=%d, Pos=(%.1f, %.1f, %.1f)\n",
                mRoomNum, spawnerID, (int)spawnerTypes[i],
                spawnerPositions[i].x, spawnerPositions[i].y, spawnerPositions[i].z);
        }
    }

    // 초기 적 스폰
    void SpawnInitialEnemies()
    {
        for (auto spawner : mSpawners)
        {
            INT64 enemyID = GenerateEnemyID();
            Enemy* enemy = spawner->SpawnEnemy(enemyID);

            if (enemy != nullptr)
            {
                mEnemies[enemyID] = enemy;
            }
        }

        printf("[Room %d] Initial enemies spawned: %d enemies\n", mRoomNum, (int)mEnemies.size());
    }

    // 업데이트 루프
    void UpdateLoop()
    {
        auto lastUpdate = std::chrono::steady_clock::now();

        while (mIsRunning)
        {
            auto now = std::chrono::steady_clock::now();
            float deltaTime = std::chrono::duration<float>(now - lastUpdate).count();
            lastUpdate = now;

            // 적 업데이트
            for (auto& pair : mEnemies)
            {
                Enemy* enemy = pair.second;
                if (!enemy->IsDead())
                {
                    enemy->Update(deltaTime);
                }
            }

            // 스포너 업데이트 (리스폰)
            UpdateSpawners(deltaTime);

            // 0.1초마다 위치 동기화 (10 FPS)
            static float syncTimer = 0.0f;
            syncTimer += deltaTime;
            if (syncTimer >= 0.1f)
            {
                SyncEnemyPositions();
                syncTimer = 0.0f;
            }

            std::this_thread::sleep_for(std::chrono::milliseconds(33)); // ~30 FPS
        }
    }

    // 스포너 업데이트
    void UpdateSpawners(float deltaTime)
    {
        for (auto spawner : mSpawners)
        {
            INT64 newEnemyID = 0;

            // 리스폰 체크
            if (spawner->Update(deltaTime, newEnemyID))
            {
                // 새 적 스폰
                INT64 enemyID = GenerateEnemyID();
                Enemy* newEnemy = spawner->SpawnEnemy(enemyID);

                if (newEnemy != nullptr)
                {
                    mEnemies[enemyID] = newEnemy;

                    // 모든 플레이어에게 스폰 알림
                    ENEMY_SPAWN_NOTIFY_PACKET spawnPacket;
                    spawnPacket.enemyID = enemyID;
                    spawnPacket.enemyType = (INT32)newEnemy->GetEnemyType();
                    spawnPacket.position = newEnemy->GetPosition();
                    spawnPacket.rotation = newEnemy->GetRotation();
                    spawnPacket.maxHealth = newEnemy->GetMaxHealth();
                    spawnPacket.currentHealth = newEnemy->GetCurrentHealth();

                    SendToAllUser(spawnPacket.PacketLength, (char*)&spawnPacket, -1, false);

                    printf("[Room %d] Enemy respawned: ID=%lld, Type=%d\n",
                        mRoomNum, enemyID, (int)newEnemy->GetEnemyType());
                }
            }
        }
    }

    // 적 위치 동기화
    void SyncEnemyPositions()
    {
        for (auto& pair : mEnemies)
        {
            Enemy* enemy = pair.second;
            if (enemy->IsDead())
                continue;

            ENEMY_PATROL_UPDATE_PACKET packet;
            packet.enemyID = enemy->GetEnemyID();
            packet.position = enemy->GetPosition();
            packet.rotation = enemy->GetRotation();
            packet.velocity = Vector3{ 0, 0, 0 };

            SendToAllUser(packet.PacketLength, (char*)&packet, -1, false);
        }
    }

    // 플레이어 공격 처리
    void ProcessPlayerAttack(INT64 attackerID, const Vector3& attackPos, const Vector3& attackDir)
    {
        const float ATTACK_RANGE = 2.0f;
        const float ATTACK_WIDTH = 1.5f;
        const float ATTACK_HEIGHT = 2.0f;

        INT64 hitEnemyID = 0;
        Enemy* hitEnemy = nullptr;

        // BoxCollider 범위 내 적 찾기
        for (auto& pair : mEnemies)
        {
            Enemy* enemy = pair.second;
            if (enemy->IsDead())
                continue;

            Vector3 enemyPos = enemy->GetPosition();

            // 공격자 앞쪽 박스 범위 체크
            Vector3 forward = attackDir;
            forward.y = 0.0f;
            float len = sqrtf(forward.x * forward.x + forward.z * forward.z);
            if (len < 0.0001f) { forward = { 0,0,1 }; }
            else { forward.x /= len; forward.z /= len; }

            Vector3 attackCenter = attackPos;
            attackCenter.x += attackDir.x * (ATTACK_RANGE / 2.0f);
            attackCenter.z += attackDir.z * (ATTACK_RANGE / 2.0f);
            attackCenter.y = attackPos.y;

            // 박스 충돌 체크
            if (IsPointInBox(enemyPos, attackCenter, forward, ATTACK_WIDTH, ATTACK_HEIGHT, ATTACK_RANGE))
            {
                hitEnemy = enemy;
                hitEnemyID = enemy->GetEnemyID();
                break;
            }
        }

        // 적을 맞췄으면
        if (hitEnemy != nullptr)
        {
            INT32 damage = 25;
            bool isDead = hitEnemy->TakeDamage(damage);

            // 데미지 알림
            ENEMY_DAMAGE_NOTIFY_PACKET damagePacket;
            damagePacket.enemyID = hitEnemyID;
            damagePacket.attackerID = attackerID;
            damagePacket.damageAmount = damage;
            damagePacket.remainingHealth = hitEnemy->GetCurrentHealth();
            SendToAllUser(damagePacket.PacketLength, (char*)&damagePacket, -1, false);

            printf("[Room %d] Enemy %lld took %d damage from player %lld. HP: %d/%d\n",
                mRoomNum, hitEnemyID, damage, attackerID,
                hitEnemy->GetCurrentHealth(), hitEnemy->GetMaxHealth());

            // 사망 처리
            if (isDead)
            {
                ENEMY_DEATH_NOTIFY_PACKET deathPacket;
                deathPacket.enemyID = hitEnemyID;
                deathPacket.killerID = attackerID;
                SendToAllUser(deathPacket.PacketLength, (char*)&deathPacket, -1, false);

                printf("[Room %d] Enemy %lld killed by player %lld\n", mRoomNum, hitEnemyID, attackerID);

                // 스포너에 사망 알림
                NotifySpawnerEnemyDeath(hitEnemy);
            }
        }
        else
        {
            printf("[Room %d] Player %lld attack missed!\n", mRoomNum, attackerID);
        }
    }

    // 스포너에게 적 사망 알림
    void NotifySpawnerEnemyDeath(Enemy* deadEnemy)
    {
        for (auto spawner : mSpawners)
        {
            if (spawner->GetEnemy() == deadEnemy)
            {
                spawner->OnEnemyDeath();
                break;
            }
        }
    }

    // 박스 충돌 체크 (간단한 AABB)
    bool IsPointInBox(const Vector3& point, const Vector3& boxCenter, const Vector3& forward,
        float width, float height, float depth)
    {
        const float halfW = width * 0.5f;
        const float halfH = height * 0.5f;
        const float halfD = depth * 0.5f;

        // forward는 XZ 평면 기준 정규화된 벡터가 들어온다고 가정
        Vector3 f = forward;
        f.y = 0.0f;

        float flen = sqrtf(f.x * f.x + f.z * f.z);
        if (flen < 0.0001f) { f = { 0,0,1 }; }
        else { f.x /= flen; f.z /= flen; }

        // right = forward를 90도 회전
        Vector3 r = { -f.z, 0.0f, f.x };
        Vector3 u = { 0.0f, 1.0f, 0.0f };

        Vector3 d = point;
        d.x -= boxCenter.x;
        d.y -= boxCenter.y;
        d.z -= boxCenter.z;

        float localX = d.x * r.x + d.y * r.y + d.z * r.z; // right
        float localY = d.x * u.x + d.y * u.y + d.z * u.z; // up
        float localZ = d.x * f.x + d.y * f.y + d.z * f.z; // forward

        return (fabs(localX) <= halfW &&
            fabs(localY) <= halfH &&
            fabs(localZ) <= halfD);
    }

	UINT16 EnterUser(User* user_)
	{
		if (mCurrentUserCount >= mMaxUserCount)
		{
			return (UINT16)ERROR_CODE::ENTER_ROOM_FULL_USER;
		}

		mUserList.push_back(user_);
		++mCurrentUserCount;

		user_->EnterRoom(mRoomNum);

		// 입장하는 유저에게, Zone 내 유저 수 만큼 정보 송신
		for (auto pRoomUser : mUserList)
		{
			if (pRoomUser == nullptr || pRoomUser == user_) {
				continue;
			}

			ROOM_USER_INFO_NTF_PACKET roomUserInfoNtf;
			roomUserInfoNtf.userUUID = pRoomUser->GetNetConnIdx();
			CopyUserID(roomUserInfoNtf.userID, *pRoomUser);
			roomUserInfoNtf.position = pRoomUser->GetPosition();
			roomUserInfoNtf.rotation = pRoomUser->GetRotation();
			SendPacketFunc(user_->GetNetConnIdx(), roomUserInfoNtf.PacketLength, (char*)&roomUserInfoNtf);
		}

		// 입장하는 유저에게, Zone 내 Npc 수 만큼 정보 송신
		for (auto pRoomNpc : mNpcList)
		{
			if (pRoomNpc == nullptr) {
				continue;
			}

			ROOM_USER_INFO_NTF_PACKET roomUserInfoNtf;
			roomUserInfoNtf.userUUID = pRoomNpc->GetNetConnIdx();
			CopyUserID(roomUserInfoNtf.userID, *pRoomNpc);
			roomUserInfoNtf.position = pRoomNpc->GetPosition();
			roomUserInfoNtf.rotation = pRoomNpc->GetRotation();
			SendPacketFunc(user_->GetNetConnIdx(), roomUserInfoNtf.PacketLength, (char*)&roomUserInfoNtf);
		}

        int sent = 0;
        for (auto& pair : mEnemies)
        {
            Enemy* enemy = pair.second;
            if (!enemy || enemy->IsDead()) continue;

            ENEMY_SPAWN_NOTIFY_PACKET spawnPacket;
            spawnPacket.enemyID = enemy->GetEnemyID();
            spawnPacket.enemyType = (INT32)enemy->GetEnemyType();
            spawnPacket.position = enemy->GetPosition();
            spawnPacket.rotation = enemy->GetRotation();
            spawnPacket.maxHealth = enemy->GetMaxHealth();
            spawnPacket.currentHealth = enemy->GetCurrentHealth();

            SendPacketFunc(user_->GetNetConnIdx(), spawnPacket.PacketLength, (char*)&spawnPacket);
            sent++;
        }
        printf("[Room %d] Sent initial enemies to user(%d): %d\n", mRoomNum, user_->GetNetConnIdx(), sent);

		return (UINT16)ERROR_CODE::NONE;
	}

	Npc* CreateNpc()
	{
		INT32 uuid = 10000 + mNpcList.size();
		auto npmID = std::to_string(uuid);

		Npc* npc = new Npc();
		npc->Init(uuid);
		npc->SetLogin(npmID.c_str());

		mNpcList.push_back(npc);
		return npc;
	}

	UINT16 EnterNpc()
	{
		Npc* newNpc = CreateNpc();
		newNpc->EnterRoom(mRoomNum);
		NotifyUserEnter(newNpc->GetNetConnIdx(), newNpc->GetUserId());
		return (UINT16)ERROR_CODE::NONE;
	}

		
	void LeaveUser(User* leaveUser_)
	{
		mUserList.remove_if([leaveUserId = leaveUser_->GetUserId()](User* pUser) {
			return leaveUserId == pUser->GetUserId();
		});

		--mCurrentUserCount;

		ROOM_LEAVE_USER_NTF_PACKET notifyPkt;
		notifyPkt.userUUID = leaveUser_->GetNetConnIdx();
		CopyUserID(notifyPkt.userID, *leaveUser_);
		bool EXCEPT_ME = true; // 퇴장하는 유저에겐 보내지 않음
		SendToAllUser(notifyPkt.PacketLength, (char*)&notifyPkt, notifyPkt.userUUID, EXCEPT_ME);
	}
						
	void NotifyChat(INT32 clientIndex_, const char* userID_, const char* msg_)
	{
		ROOM_CHAT_NOTIFY_PACKET roomChatNtfyPkt;
		CopyMemory(roomChatNtfyPkt.Msg, msg_, sizeof(roomChatNtfyPkt.Msg));
		CopyUserID(roomChatNtfyPkt.userID, userID_);
		SendToAllUser(sizeof(roomChatNtfyPkt), (char*)&roomChatNtfyPkt, clientIndex_, false);
	}

	void NotifyUserEnter(INT32 clientIndex_, const std::string& userID)
	{
		ROOM_NEW_USER_NTF_PACKET roomNewUserNtfPkt;
		roomNewUserNtfPkt.userUUID = clientIndex_;
		CopyUserID(roomNewUserNtfPkt.userID, userID);
		bool EXCEPT_ME = false; // 입장하는 유저도 본 패킷이 로직에 필요함
		SendToAllUser(roomNewUserNtfPkt.PacketLength, (char*)&roomNewUserNtfPkt, clientIndex_, EXCEPT_ME);
	}

		
	std::function<void(UINT32, UINT32, char*)> SendPacketFunc;

    void ProcessHitReport(INT64 attackerID, INT64 enemyID, INT32 damage)
    {
        auto it = mEnemies.find(enemyID);
        if (it == mEnemies.end() || it->second == nullptr)
        {
            printf("[Room %d] HitReport enemy not found. enemy=%lld\n", mRoomNum, enemyID);
            return;
        }

        Enemy* enemy = it->second;
        if (enemy->IsDead()) return;

        if (damage <= 0) damage = 1;

        bool isDead = enemy->TakeDamage(damage);

        ENEMY_DAMAGE_NOTIFY_PACKET damagePacket;
        damagePacket.enemyID = enemyID;
        damagePacket.attackerID = attackerID;
        damagePacket.damageAmount = damage;
        damagePacket.remainingHealth = enemy->GetCurrentHealth();
        SendToAllUser(damagePacket.PacketLength, (char*)&damagePacket, -1, false);

        printf("[Room %d] Sent 424 damage. enemy=%lld hp=%d\n", mRoomNum, enemyID, enemy->GetCurrentHealth());

        if (isDead)
        {
            ENEMY_DEATH_NOTIFY_PACKET deathPacket;
            deathPacket.enemyID = enemyID;
            deathPacket.killerID = attackerID;
            SendToAllUser(deathPacket.PacketLength, (char*)&deathPacket, -1, false);

            // 킬러 퀘스트 진행도 +1 및 505 전송
            OnEnemyKilledForQuest(attackerID);

            NotifySpawnerEnemyDeath(enemy);
        }
    }

	void SendToAllUser(const UINT16 dataSize_, char* data_, const INT32 passUserIndex_, bool exceptMe)
	{
		for (auto pUser : mUserList)
		{
			if (pUser == nullptr) {
				continue;
			}

			if (exceptMe && pUser->GetNetConnIdx() == passUserIndex_) {
				continue;
			}

			SendPacketFunc((UINT32)pUser->GetNetConnIdx(), (UINT32)dataSize_, data_);
		}
	}

    // 헬퍼 함수
    int GetAliveEnemyCount() const
    {
        int count = 0;
        for (auto& pair : mEnemies)
        {
            if (!pair.second->IsDead())
                count++;
        }
        return count;
    }

    User* FindUserByConnIdx(INT64 connIdx)
    {
        for (auto* u : mUserList)
        {
            if (u && u->GetNetConnIdx() == connIdx)
                return u;
        }
        return nullptr;
    }

    Enemy* FindEnemyById(INT64 enemyID)
    {
        auto it = mEnemies.find(enemyID);
        if (it == mEnemies.end()) return nullptr;
        return it->second;
    }

    void SetQuestAccepted(INT64 userConnIdx, INT32 questId, UINT16 required)
    {
        auto& qp = mQuestProgressByUser[userConnIdx];
        qp.questId = questId;
        qp.state = QUEST_STATE::IN_PROGRESS;
        qp.current = 0;
        qp.required = (required == 0 ? 1 : required);

        printf("[Room %d] Quest accepted. user=%lld quest=%d req=%d\n",
            mRoomNum, userConnIdx, questId, (int)qp.required);
    }

    void OnEnemyKilledForQuest(INT64 killerConnIdx)
    {
        auto it = mQuestProgressByUser.find(killerConnIdx);
        if (it == mQuestProgressByUser.end())
            return;

        QuestProgress& qp = it->second;
        if (qp.state != QUEST_STATE::IN_PROGRESS)
            return;

        qp.current++;
        if (qp.current >= qp.required)
        {
            qp.current = qp.required;
            qp.state = QUEST_STATE::COMPLETED;
        }

        QUEST_PROGRESS_NOTIFY_PACKET ntf;
        ntf.quest_id = qp.questId;
        ntf.current = qp.current;
        ntf.required = qp.required;
        ntf.state = (UINT8)qp.state;

        SendPacketFunc((UINT32)killerConnIdx, (UINT32)ntf.PacketLength, (char*)&ntf);

        printf("[Room %d] Quest progress notify(505). user=%lld quest=%d %d/%d state=%d\n",
            mRoomNum, killerConnIdx, qp.questId, qp.current, qp.required, (int)qp.state);
    }

private:
    INT64 GenerateEnemyID()
    {
        static INT64 nextID = 1;
        return (INT64)mRoomNum * 10000 + (nextID++);
    }

    NavMeshManager navMeshManager;

    INT32 mRoomNum = -1;

    std::list<User*> mUserList;
    std::list<Npc*> mNpcList;

    // 적 관리 (map으로 변경)
    std::unordered_map<INT64, Enemy*> mEnemies;

    // 스포너 리스트
    std::vector<EnemySpawner*> mSpawners;

    INT32 mMaxUserCount = 0;
    UINT16 mCurrentUserCount = 0;

    // 업데이트 스레드
    bool mIsRunning = false;
    std::thread mUpdateThread;

    struct QuestProgress
    {
        INT32 questId = 0;
        QUEST_STATE state = QUEST_STATE::NOT_ACCEPTED;
        UINT16 current = 0;
        UINT16 required = 1;
    };

    std::unordered_map<INT64, QuestProgress> mQuestProgressByUser;
};


void CopyUserID(char* userID, const Actor& user)
{
	CopyUserID(userID, user.GetUserId());
}

void CopyUserID(char* userID, const std::string& userID_)
{
	CopyMemory(userID, userID_.c_str(), sizeof(userID));
}

void CopyUserID(char* userID, const char* userID_)
{
	CopyMemory(userID, userID_, (MAX_USER_ID_LEN + 1));
}