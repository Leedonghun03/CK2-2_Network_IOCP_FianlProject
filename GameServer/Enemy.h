#pragma once
#include "Actor.h"
#include <chrono>

enum class ENEMY_STATE
{
    IDLE,
    PATROL,
    CHASE,
    ATTACK,
    DEAD
};

enum class ENEMY_TYPE
{
    SLIME = 1,
    GOBLIN = 2,
    WOLF = 3
};

class Enemy : public Actor
{
public:
    Enemy() = default;
    ~Enemy() = default;

    // 초기화
    void Init(const INT64 id, const Vector3& spawnPos, ENEMY_TYPE type);

    // 업데이트
    void Update(float deltaTime);

    // 데미지 받기 (반환: true=사망, false=생존)
    bool TakeDamage(INT32 damage);

    // Getter
    INT64 GetEnemyID() const { return mEnemyID; }
    ENEMY_TYPE GetEnemyType() const { return mEnemyType; }
    ENEMY_STATE GetState() const { return mState; }
    INT32 GetMaxHealth() const { return mMaxHealth; }
    INT32 GetCurrentHealth() const { return mCurrentHealth; }
    bool IsDead() const { return mState == ENEMY_STATE::DEAD; }

private:
    void SetEnemyStats(ENEMY_TYPE type);
    void UpdatePatrol(float deltaTime);
    void UpdateIdle(float deltaTime);
    void SetRandomPatrolTarget();

    INT64 mEnemyID = 0;
    ENEMY_TYPE mEnemyType = ENEMY_TYPE::SLIME;
    ENEMY_STATE mState = ENEMY_STATE::PATROL;

    // 스탯
    INT32 mMaxHealth = 100;
    INT32 mCurrentHealth = 100;
    INT32 mAttackDamage = 10;
    float mMoveSpeed = 2.0f;
    float mPatrolRange = 10.0f;
    float mDetectionRange = 5.0f;

    // 패트롤
    Vector3 mSpawnPosition;
    Vector3 mPatrolTarget;
    bool mHasPatrolTarget = false;
    float mIdleTime = 0.0f;

    float PATROL_MIN_X = 17.0f;
    float PATROL_MAX_X = 30.0f;
    float PATROL_MIN_Z = 50.0f;
    float PATROL_MAX_Z = 85.0f;

    std::chrono::steady_clock::time_point mLastUpdateTime;
};