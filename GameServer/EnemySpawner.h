#pragma once
#include "Enemy.h"

class EnemySpawner
{
public:
    EnemySpawner() = default;
    ~EnemySpawner() = default;

    void Init(INT64 spawnerID, const Vector3& spawnPos, ENEMY_TYPE enemyType, float respawnTime = 30.0f)
    {
        mSpawnerID = spawnerID;
        mSpawnPosition = spawnPos;
        mEnemyType = enemyType;
        mRespawnTime = respawnTime;
        mIsActive = true;
    }

    // 적 생성
    Enemy* SpawnEnemy(INT64 enemyID)
    {
        if (mCurrentEnemy != nullptr)
        {
            printf("[Spawner %lld] Already has an enemy!\n", mSpawnerID);
            return nullptr;
        }

        Enemy* enemy = new Enemy();
        enemy->Init(enemyID, mSpawnPosition, mEnemyType);
        mCurrentEnemy = enemy;
        mIsWaitingRespawn = false;

        printf("[Spawner %lld] Spawned enemy %lld (Type:%d) at (%.1f, %.1f, %.1f)\n",
            mSpawnerID, enemyID, (int)mEnemyType,
            mSpawnPosition.x, mSpawnPosition.y, mSpawnPosition.z);

        return enemy;
    }

    // 적이 죽었을 때 호출
    void OnEnemyDeath()
    {
        mCurrentEnemy = nullptr;
        mIsWaitingRespawn = true;
        mRespawnTimer = mRespawnTime;

        printf("[Spawner %lld] Enemy died. Respawning in %.1f seconds...\n",
            mSpawnerID, mRespawnTime);
    }

    // 업데이트 (리스폰 타이머)
    // 반환: true=리스폰 준비 완료, false=대기 중
    bool Update(float deltaTime, INT64& outNewEnemyID)
    {
        if (!mIsActive || !mIsWaitingRespawn)
            return false;

        mRespawnTimer -= deltaTime;

        if (mRespawnTimer <= 0.0f)
        {
            // 리스폰 준비 완료
            outNewEnemyID = 0; // Room에서 ID 생성
            return true;
        }

        return false;
    }

    // Getter
    INT64 GetSpawnerID() const { return mSpawnerID; }
    bool IsActive() const { return mIsActive; }
    bool HasEnemy() const { return mCurrentEnemy != nullptr; }
    Enemy* GetEnemy() const { return mCurrentEnemy; }
    const Vector3& GetSpawnPosition() const { return mSpawnPosition; }
    ENEMY_TYPE GetEnemyType() const { return mEnemyType; }

private:
    INT64 mSpawnerID = 0;
    Vector3 mSpawnPosition;
    ENEMY_TYPE mEnemyType;
    float mRespawnTime = 30.0f;         // 리스폰 시간 (초)

    Enemy* mCurrentEnemy = nullptr;
    bool mIsWaitingRespawn = false;
    float mRespawnTimer = 0.0f;
    bool mIsActive = true;
};