using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour, IPacketReceiver
{
    public static EnemyManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject enemyPrefab;

    private Dictionary<long, Enemy> enemies = new Dictionary<long, Enemy>();
    private readonly Dictionary<long, P_EnemyDamageNotify> pendingDamage = new Dictionary<long, P_EnemyDamageNotify>();
    private readonly HashSet<long> pendingDeath = new HashSet<long>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Client.TCP.AddPacketReceiver(this);
        Debug.Log("[EnemyManager] Started");
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        ushort packetId = packet.pbase.packet_id;

        switch ((E_PACKET)packetId)
        {
            case E_PACKET.ENEMY_SPAWN_NOTIFY:
                HandleEnemySpawn(packet.data);
                break;

            case E_PACKET.ENEMY_DESPAWN_NOTIFY:
                HandleEnemyDespawn(packet.data);
                break;

            case E_PACKET.ENEMY_PATROL_UPDATE:
                HandleEnemyPatrolUpdate(packet.data);
                break;

            case E_PACKET.ENEMY_DAMAGE_NOTIFY:
                HandleEnemyDamage(packet.data);
                break;

            case E_PACKET.ENEMY_DEATH_NOTIFY:
                HandleEnemyDeath(packet.data);
                break;
        }
    }

    void HandleEnemySpawn(byte[] data)
    {
        P_EnemySpawnNotify spawnData = UnsafeCode.ByteArrayToStructure<P_EnemySpawnNotify>(data);

        if (enemies.ContainsKey(spawnData.enemyID))
        {
            Debug.LogWarning($"[EnemyManager] Enemy {spawnData.enemyID} already exists!");
            return;
        }

        GameObject enemyObj = Instantiate(enemyPrefab, spawnData.position, spawnData.rotation);
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null)
        {
            enemy = enemyObj.AddComponent<Enemy>();
        }

        enemy.Initialize(spawnData.enemyID, spawnData.enemyType,
                        spawnData.position, spawnData.rotation,
                        spawnData.maxHealth, spawnData.currentHealth);

        enemies.Add(spawnData.enemyID, enemy);

        Debug.Log($"[EnemyManager] Enemy spawned: ID={spawnData.enemyID}, Type={spawnData.enemyType}, Total={enemies.Count}");

        if (pendingDamage.TryGetValue(spawnData.enemyID, out var dmg))
        {
            enemy.TakeDamage(dmg.damageAmount, dmg.remainingHealth);
            pendingDamage.Remove(spawnData.enemyID);
        }

        if (pendingDeath.Contains(spawnData.enemyID))
        {
            enemy.Die();
            enemies.Remove(spawnData.enemyID);
            pendingDeath.Remove(spawnData.enemyID);
        }
    }

    void HandleEnemyDespawn(byte[] data)
    {
        P_EnemyDespawnNotify despawnData = UnsafeCode.ByteArrayToStructure<P_EnemyDespawnNotify>(data);

        if (enemies.TryGetValue(despawnData.enemyID, out Enemy enemy))
        {
            Destroy(enemy.gameObject);
            enemies.Remove(despawnData.enemyID);
            Debug.Log($"[EnemyManager] Enemy despawned: ID={despawnData.enemyID}");
        }
    }

    void HandleEnemyPatrolUpdate(byte[] data)
    {
        P_EnemyPatrolUpdate updateData = UnsafeCode.ByteArrayToStructure<P_EnemyPatrolUpdate>(data);

        if (enemies.TryGetValue(updateData.enemyID, out Enemy enemy))
        {
            enemy.UpdatePosition(updateData.position, updateData.rotation);
        }
    }

    void HandleEnemyDamage(byte[] data)
    {
        P_EnemyDamageNotify damageData = UnsafeCode.ByteArrayToStructure<P_EnemyDamageNotify>(data);

        if (enemies.TryGetValue(damageData.enemyID, out Enemy enemy))
        {
            enemy.TakeDamage(damageData.damageAmount, damageData.remainingHealth);

            if (damageData.attackerID == LocalPlayerInfo.ID)
                Debug.Log($"Hit! Damage: {damageData.damageAmount}");
        }
        else
        {
            // 스폰이 아직이면 저장해뒀다가 스폰 시 적용
            pendingDamage[damageData.enemyID] = damageData;
            Debug.LogWarning($"[EnemyManager] Damage arrived before spawn. Cached. enemyID={damageData.enemyID}");
        }
    }

    void HandleEnemyDeath(byte[] data)
    {
        P_EnemyDeathNotify deathData = UnsafeCode.ByteArrayToStructure<P_EnemyDeathNotify>(data);

        if (enemies.TryGetValue(deathData.enemyID, out Enemy enemy))
        {
            enemy.Die();
            enemies.Remove(deathData.enemyID);

            if (deathData.killerID == LocalPlayerInfo.ID)
                Debug.Log("적을 처치했습니다!");

            Debug.Log($"[EnemyManager] Enemy died: ID={deathData.enemyID}, Total={enemies.Count}");
        }
        else
        {
            pendingDeath.Add(deathData.enemyID);
            Debug.LogWarning($"[EnemyManager] Death arrived before spawn. Cached. enemyID={deathData.enemyID}");
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Client.TCP.RemovePacketReceiver(this);
            Instance = null;
        }
    }
}