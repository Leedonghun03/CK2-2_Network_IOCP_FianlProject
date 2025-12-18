#include "Enemy.h"
#include <cmath>
#include <cstdlib>

// 유틸리티 함수들
namespace {
    inline float Vector3_Distance(const Vector3& a, const Vector3& b)
    {
        float dx = b.x - a.x;
        float dy = b.y - a.y;
        float dz = b.z - a.z;
        return sqrtf(dx * dx + dy * dy + dz * dz);
    }

    inline float Vector3_Length(const Vector3& v)
    {
        return sqrtf(v.x * v.x + v.y * v.y + v.z * v.z);
    }

    inline Vector3 Vector3_Subtract(const Vector3& a, const Vector3& b)
    {
        return Vector3{ a.x - b.x, a.y - b.y, a.z - b.z };
    }

    inline Vector3 Vector3_Normalize(const Vector3& v)
    {
        float len = Vector3_Length(v);
        if (len < 0.0001f)
            return Vector3{ 0, 0, 0 };
        return Vector3{ v.x / len, v.y / len, v.z / len };
    }

    inline Quaternion QuaternionLookRotation(const Vector3& forward)
    {
        float angle = atan2f(forward.x, forward.z);
        return Quaternion{ 0, sinf(angle / 2.0f), 0, cosf(angle / 2.0f) };
    }

    inline float ClampFloat(float v, float minV, float maxV)
    {
        if (v < minV) return minV;
        if (v > maxV) return maxV;
        return v;
    }
}

void Enemy::Init(const INT64 id, const Vector3& spawnPos, ENEMY_TYPE type)
{
    mEnemyID = id;
    mEnemyType = type;
    position = spawnPos;
    mSpawnPosition = spawnPos;

    SetEnemyStats(type);

    mCurrentHealth = mMaxHealth;
    mState = ENEMY_STATE::PATROL;
    mLastUpdateTime = std::chrono::steady_clock::now();
}

void Enemy::SetEnemyStats(ENEMY_TYPE type)
{
    switch (type)
    {
    case ENEMY_TYPE::SLIME:
        mMaxHealth = 100;
        mAttackDamage = 10;
        mMoveSpeed = 2.0f;
        mPatrolRange = 10.0f;
        mDetectionRange = 5.0f;
        break;

    case ENEMY_TYPE::GOBLIN:
        mMaxHealth = 150;
        mAttackDamage = 20;
        mMoveSpeed = 3.0f;
        mPatrolRange = 15.0f;
        mDetectionRange = 7.0f;
        break;

    case ENEMY_TYPE::WOLF:
        mMaxHealth = 200;
        mAttackDamage = 30;
        mMoveSpeed = 4.5f;
        mPatrolRange = 20.0f;
        mDetectionRange = 10.0f;
        break;
    }
}

void Enemy::Update(float deltaTime)
{
    if (mState == ENEMY_STATE::DEAD)
        return;

    switch (mState)
    {
    case ENEMY_STATE::PATROL:
        UpdatePatrol(deltaTime);
        break;

    case ENEMY_STATE::IDLE:
        UpdateIdle(deltaTime);
        break;

        // TODO: CHASE, ATTACK
    }
}

void Enemy::UpdatePatrol(float deltaTime)
{
    // 목표 위치가 없거나 도착했으면 새 목표 설정
    if (!mHasPatrolTarget || Vector3_Distance(position, mPatrolTarget) < 1.0f)
    {
        SetRandomPatrolTarget();
    }

    // 목표를 향해 이동
    Vector3 direction = Vector3_Normalize(Vector3_Subtract(mPatrolTarget, position));
    Vector3 movement = Vector3_Multiply(direction, mMoveSpeed * deltaTime);
    position = Vector3_Addition(position, movement);

    position.x = ClampFloat(position.x, PATROL_MIN_X, PATROL_MAX_X);
    position.z = ClampFloat(position.z, PATROL_MIN_Z, PATROL_MAX_Z);

    // 회전
    if (Vector3_Length(direction) > 0.01f)
    {
        rotation = QuaternionLookRotation(direction);
    }
}

void Enemy::UpdateIdle(float deltaTime)
{
    mIdleTime += deltaTime;

    // 2초 대기 후 다시 패트롤
    if (mIdleTime >= 2.0f)
    {
        mState = ENEMY_STATE::PATROL;
        mIdleTime = 0.0f;
    }
}

void Enemy::SetRandomPatrolTarget()
{
    float randomX = ((rand() % 200) - 100) / 100.0f * mPatrolRange;
    float randomZ = ((rand() % 200) - 100) / 100.0f * mPatrolRange;

    mPatrolTarget = mSpawnPosition;
    mPatrolTarget.x += randomX;
    mPatrolTarget.z += randomZ;

    // 경계 밖 목적지 방지 (BoxCollider 범위로 제한)
    mPatrolTarget.x = ClampFloat(mPatrolTarget.x, PATROL_MIN_X, PATROL_MAX_X);
    mPatrolTarget.z = ClampFloat(mPatrolTarget.z, PATROL_MIN_Z, PATROL_MAX_Z);

    mHasPatrolTarget = true;
}

bool Enemy::TakeDamage(INT32 damage)
{
    if (mState == ENEMY_STATE::DEAD)
        return false;

    mCurrentHealth -= damage;

    if (mCurrentHealth <= 0)
    {
        mCurrentHealth = 0;
        mState = ENEMY_STATE::DEAD;
        return true; // 사망
    }

    return false; // 생존
}