using UnityEngine;
using System.Collections;

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

    private float lastAttackTime = 0f;
    private bool canAttack = true;
    private BoxCollider attackHitbox;
    private bool isAttacking = false;

    void Start()
    {
        // 히트박스 생성
        GameObject hitboxObj = new GameObject("AttackHitbox");
        hitboxObj.transform.parent = transform;
        hitboxObj.transform.localPosition = Vector3.forward * (attackRange / 2f);

        attackHitbox = hitboxObj.AddComponent<BoxCollider>();
        attackHitbox.isTrigger = true;
        attackHitbox.size = new Vector3(attackWidth, attackHeight, attackRange);
        attackHitbox.enabled = false;
    }

    void Update()
    {
        // 좌클릭으로 공격
        if (Input.GetMouseButtonDown(0) && canAttack && !isAttacking)
        {
            Attack();
        }

        // 쿨다운 체크
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

        Debug.Log($"공격! 위치: {transform.position}, 방향: {transform.forward}");

        // 히트박스 활성화
        StartCoroutine(ActivateHitbox());

        // 서버에 공격 요청
        P_PlayerAttackRequest attackPacket = new P_PlayerAttackRequest
        {
            attackPosition = transform.position,
            attackDirection = transform.forward
        };

        Client.TCP.SendPacket(E_PACKET.PLAYER_ATTACK_REQUEST, attackPacket);
    }

    IEnumerator ActivateHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.transform.localPosition = Vector3.forward * (attackRange / 2f);
            attackHitbox.transform.localRotation = Quaternion.identity;
            attackHitbox.enabled = true;
        }

        yield return new WaitForSeconds(hitboxDuration);

        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }

        isAttacking = false;
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