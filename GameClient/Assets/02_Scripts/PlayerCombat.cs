using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 2.0f;
    public float attackWidth = 1.5f;
    public float attackHeight = 2.0f;
    public int attackDamage = 25;
    public float attackCooldown = 0.5f;
    public float hitboxDuration = 0.2f;

    [Header("Visual")]
    public bool showDebugBox = true;

    [Header("Client Hit Detection")]
    [SerializeField] private LayerMask enemyMask; // Enemy만 체크

    private float lastAttackTime = 0f;
    private bool canAttack = true;
    private BoxCollider attackHitbox;
    private bool isAttacking = false;

    private uint hitSeq = 0;

    // 한 번 공격에서 중복 히트 방지(가장 중요한 안정장치)
    private readonly HashSet<long> hitThisSwing = new HashSet<long>();

    void Start()
    {
        // 히트박스 생성(시각화/디버그용으로 유지)
        GameObject hitboxObj = new GameObject("AttackHitbox");
        hitboxObj.transform.parent = transform;
        hitboxObj.transform.localPosition = Vector3.forward * (attackRange / 2f);

        attackHitbox = hitboxObj.AddComponent<BoxCollider>();
        attackHitbox.isTrigger = true;
        attackHitbox.size = new Vector3(attackWidth, attackHeight, attackRange);
        attackHitbox.enabled = false;

        enemyMask = LayerMask.GetMask("Enemy");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && canAttack && !isAttacking)
        {
            Attack();
        }

        if (!canAttack && Time.time - lastAttackTime >= attackCooldown)
        {
            canAttack = true;
        }
    }

    void Attack()
    {
        canAttack = false;
        isAttacking = true;
        lastAttackTime = Time.time;

        hitThisSwing.Clear();

        Debug.Log($"공격! 위치: {transform.position}, 방향: {transform.forward}");

        // 히트박스 활성화
        StartCoroutine(ActivateHitboxAndCheckHit());
    }

    IEnumerator ActivateHitboxAndCheckHit()
    {
        if (attackHitbox != null)
        {
            attackHitbox.transform.localPosition = Vector3.forward * (attackRange / 2f);
            attackHitbox.transform.localRotation = Quaternion.identity;
            attackHitbox.enabled = true;
        }

        // 클라 판정은 여기서 1회 수행
        PerformClientHitCheck();

        yield return new WaitForSeconds(hitboxDuration);

        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }

        isAttacking = false;
    }

    void PerformClientHitCheck()
    {
        // 공격 박스 중심(플레이어 기준 전방 range/2, 높이 height/2)
        Vector3 boxCenter = transform.position
                            + transform.forward * (attackRange / 2f)
                            + Vector3.up * (attackHeight / 2f);

        // OverlapBox는 half extents를 받음
        Vector3 halfExtents = new Vector3(attackWidth / 2f, attackHeight / 2f, attackRange / 2f);

        // 회전된 박스 판정 (플레이어 회전 반영)
        Collider[] hits = Physics.OverlapBox(
            boxCenter,
            halfExtents,
            transform.rotation,
            enemyMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
        {
            Debug.Log("클라 판정: 아무도 안 맞음");
            return;
        }

        hitSeq++;

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null) continue;

            // Enemy 찾기
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            long enemyId = enemy.enemyID;
            if (enemyId == 0) continue;

            // 중복 히트 방지
            if (!hitThisSwing.Add(enemyId))
                continue;

            // (선택) 로컬 피격 연출(HP는 서버 패킷으로 확정 반영)
            // enemy.PlayHitFxLocal();

            // 서버에 "맞았다" 보고 (HIT_REPORT)
            SendHitReport(enemyId, attackDamage, col.ClosestPoint(boxCenter), hitSeq);

            Debug.Log($"클라 판정 히트! enemyId={enemyId}, dmg={attackDamage}");
        }
    }

    void SendHitReport(long enemyId, int damage, Vector3 hitPos, uint seq)
    {
        P_HitReport pkt = new P_HitReport
        {
            enemyID = enemyId,
            damage = damage,
            hitX = hitPos.x,
            hitY = hitPos.y,
            hitZ = hitPos.z,
            seq = seq
        };

        Client.TCP.SendPacket(E_PACKET.HIT_REPORT, pkt);
    }

    void OnDrawGizmos()
    {
        if (!showDebugBox) return;

        Gizmos.color = isAttacking ? Color.red : new Color(1f, 0f, 0f, 0.3f);

        Vector3 boxCenter = transform.position + transform.forward * (attackRange / 2f) + Vector3.up * (attackHeight / 2f);
        Vector3 boxSize = new Vector3(attackWidth, attackHeight, attackRange);

        Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCenter, transform.rotation, boxSize);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * attackRange);
    }
}