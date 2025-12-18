using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour, IPacketReceiver
{
    public static EnemyManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject enemyPrefab;

    private Dictionary<long, Enemy> enemies = new Dictionary<long, Enemy>();

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
        byte packetId = packet.pbase.packet_id;

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

            // 공격자가 나면 메시지 표시
            if (damageData.attackerID == LocalPlayerInfo.ID)
            {
                Debug.Log($"Hit! Damage: {damageData.damageAmount}");
            }
        }
    }

    void HandleEnemyDeath(byte[] data)
    {
        P_EnemyDeathNotify deathData = UnsafeCode.ByteArrayToStructure<P_EnemyDeathNotify>(data);

        if (enemies.TryGetValue(deathData.enemyID, out Enemy enemy))
        {
            enemy.Die();
            enemies.Remove(deathData.enemyID);

            // 처치자가 나면 메시지 표시
            if (deathData.killerID == LocalPlayerInfo.ID)
            {
                Debug.Log("적을 처치했습니다!");
            }

            Debug.Log($"[EnemyManager] Enemy died: ID={deathData.enemyID}, Total={enemies.Count}");
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