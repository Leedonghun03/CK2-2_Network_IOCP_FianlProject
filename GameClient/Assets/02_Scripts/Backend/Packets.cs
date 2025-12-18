using System.Runtime.InteropServices;
using UnityEngine;

public enum E_PACKET : ushort
{
    PLAYER_NAME = 201,                  // 서버: LOGIN_REQUEST
    PLAYER_NAME_SUCCESS = 202,          // 서버: LOGIN_RESPONSE

    PLAYER_JOINED = 208,                // 서버: ROOM_NEW_USER_NTF
    CREATE_MATCH_PLAYER = 209,          // 서버: ROOM_USER_INFO_NTF

    PLAYER_MOVEMENT = 218,              // 서버: PLAYER_MOVEMENT
    UPDATE_PLAYER_MOVEMENT = 219,       // 서버: UPDATE_PLAYER_MOVEMENT

    SEND_CHAT_MESSAGE = 221,            // 서버: ROOM_CHAT_REQUEST
    RECEIVE_CHAT_MESSAGE = 223,         // 서버: ROOM_CHAT_NOTIFY

    PLAYER_LEFT = 217,                  // 서버: ROOM_LEAVE_USER_NTF

    // Inventory
    INVENTORY_INFO_REQUEST = 301,
    INVENTORY_INFO_RESPONSE = 302,
    ITEM_ADD_REQUEST = 303,
    ITEM_ADD_RESPONSE = 304,
    ITEM_USE_REQUEST = 306,
    ITEM_USE_RESPONSE = 307,

    // Combat
    PLAYER_ATTACK_REQUEST = 401,
    PLAYER_ATTACK_RESPONSE = 402,
    HIT_REPORT = 404,

    // Enemy
    ENEMY_SPAWN_NOTIFY = 421,
    ENEMY_DESPAWN_NOTIFY = 422,
    ENEMY_PATROL_UPDATE = 423,
    ENEMY_DAMAGE_NOTIFY = 424,
    ENEMY_DEATH_NOTIFY = 425,

    QUEST_TALK_REQUEST = 501,
    QUEST_TALK_RESPONSE = 502,
    QUEST_ACCEPT_REQUEST = 503,
    QUEST_ACCEPT_RESPONSE = 504,
    QUEST_PROGRESS_NOTIFY = 505,
}

public enum ITEM_TYPE : ushort
{
    NONE = 0,
    WEAPON = 1,
    ARMOR = 2,
    POTION = 3,
    MATERIAL = 4,
    QUEST = 5
}

public enum QUEST_STATE : byte
{
    NONE = 0,
    IN_PROGRESS = 1,
    COMPLETED = 2,
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
struct P_PlayerName
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string name;
}

[StructLayout(LayoutKind.Sequential, Size = 24)]
struct P_PlayerNameSuccess
{
    [MarshalAs(UnmanagedType.I8)]
    public long assigned_id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string name;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_LoginResponse
{
    public ushort Result;
}

[StructLayout(LayoutKind.Sequential, Size = 24)]
struct P_PlayerJoined
{
    [MarshalAs(UnmanagedType.I8)]
    public long id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string name;
}

[StructLayout(LayoutKind.Sequential, Size = 56)]
struct P_CreateMatchPlayer
{
    [MarshalAs(UnmanagedType.I8)]
    public long id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string name;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 position;

    [MarshalAs(UnmanagedType.Struct)]
    public Quaternion rotation;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
struct P_PlayerMovement
{
    [MarshalAs(UnmanagedType.I8)]
    public long player_id;

    [MarshalAs(UnmanagedType.R4)]
    public float dx;

    [MarshalAs(UnmanagedType.R4)]
    public float dy;

    [MarshalAs(UnmanagedType.Struct)]
    public Quaternion rotation;
}

[StructLayout(LayoutKind.Sequential, Size = 40)]
struct P_UpdatePlayerMovement
{
    [MarshalAs(UnmanagedType.I8)]
    public long player_id;

    [MarshalAs(UnmanagedType.Struct)]
    public Quaternion rotation;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 motion;

}

[StructLayout(LayoutKind.Sequential, Size = 64)]
struct P_SendChatMessage
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string message;
}

[StructLayout(LayoutKind.Sequential, Size = 80)]
struct P_ReceiveChatMessage
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string sender;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string message;
}

[StructLayout(LayoutKind.Sequential, Size = 24)]
struct P_PlayerLeft
{
    [MarshalAs(UnmanagedType.I8)]
    public long id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string name;
}

// ================= 이벤토리 =========================
[StructLayout(LayoutKind.Sequential, Size = 40)]
public struct Item
{
    [MarshalAs(UnmanagedType.U4)]
    public uint itemID;

    [MarshalAs(UnmanagedType.U2)]
    public ITEM_TYPE itemType;

    [MarshalAs(UnmanagedType.U2)]
    public ushort quantity;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string itemName;
}

[StructLayout(LayoutKind.Sequential)]
struct P_InventoryInfoRequest
{
    // 빈 구조체
}

[StructLayout(LayoutKind.Sequential)]
struct P_ItemAddRequest
{
    [MarshalAs(UnmanagedType.U4)]
    public uint itemID;

    [MarshalAs(UnmanagedType.U2)]
    public ushort quantity;
}

[StructLayout(LayoutKind.Sequential)]
struct P_ItemUseRequest
{
    [MarshalAs(UnmanagedType.U2)]
    public ushort slotIndex;
}
// ===================================================

// =================== Attack ===========================
[StructLayout(LayoutKind.Sequential, Size = 24)]
public struct P_PlayerAttackRequest
{
    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 attackPosition;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 attackDirection;
}

[StructLayout(LayoutKind.Sequential)]
public struct P_PlayerAttackResponse
{
    [MarshalAs(UnmanagedType.I2)]
    public short result;

    [MarshalAs(UnmanagedType.I8)]
    public long targetEnemyID;

    [MarshalAs(UnmanagedType.I4)]
    public int damageAmount;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_HitReport
{
    [MarshalAs(UnmanagedType.I8)]
    public long enemyID;

    [MarshalAs(UnmanagedType.I4)]
    public int damage;

    [MarshalAs(UnmanagedType.R4)]
    public float hitX;

    [MarshalAs(UnmanagedType.R4)]
    public float hitY;

    [MarshalAs(UnmanagedType.R4)]
    public float hitZ;

    [MarshalAs(UnmanagedType.U4)]
    public uint seq;
}

// 적 패킷들
[StructLayout(LayoutKind.Sequential)]
public struct P_EnemySpawnNotify
{
    [MarshalAs(UnmanagedType.I8)]
    public long enemyID;

    [MarshalAs(UnmanagedType.I4)]
    public int enemyType;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 position;

    [MarshalAs(UnmanagedType.Struct)]
    public Quaternion rotation;

    [MarshalAs(UnmanagedType.I4)]
    public int maxHealth;

    [MarshalAs(UnmanagedType.I4)]
    public int currentHealth;
}

[StructLayout(LayoutKind.Sequential)]
public struct P_EnemyDespawnNotify
{
    [MarshalAs(UnmanagedType.I8)]
    public long enemyID;
}

[StructLayout(LayoutKind.Sequential)]
public struct P_EnemyPatrolUpdate
{
    [MarshalAs(UnmanagedType.I8)]
    public long enemyID;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 position;

    [MarshalAs(UnmanagedType.Struct)]
    public Quaternion rotation;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 velocity;
}

[StructLayout(LayoutKind.Sequential)]
public struct P_EnemyDamageNotify
{
    [MarshalAs(UnmanagedType.I8)]
    public long enemyID;

    [MarshalAs(UnmanagedType.I8)]
    public long attackerID;

    [MarshalAs(UnmanagedType.I4)]
    public int damageAmount;

    [MarshalAs(UnmanagedType.I4)]
    public int remainingHealth;
}

[StructLayout(LayoutKind.Sequential)]
public struct P_EnemyDeathNotify
{
    [MarshalAs(UnmanagedType.I8)]
    public long enemyID;

    [MarshalAs(UnmanagedType.I8)]
    public long killerID;
}

// ===================================================

// =================== 퀘스트 ===========================
[StructLayout(LayoutKind.Sequential)]
public struct P_QuestTalkRequest
{
    [MarshalAs(UnmanagedType.I4)] public int npc_id;
}

[StructLayout(LayoutKind.Sequential, Size = 128)]
public struct P_QuestTalkResponse
{
    [MarshalAs(UnmanagedType.I4)] public int npc_id;
    [MarshalAs(UnmanagedType.I4)] public int quest_id;
    [MarshalAs(UnmanagedType.U1)] public QUEST_STATE state;

    [MarshalAs(UnmanagedType.U2)] public ushort current;
    [MarshalAs(UnmanagedType.U2)] public ushort required;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string title;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string desc;

    [MarshalAs(UnmanagedType.U4)] public uint rewardItemID;
    [MarshalAs(UnmanagedType.U2)] public ushort rewardQty;
}

[StructLayout(LayoutKind.Sequential)]
public struct P_QuestAcceptRequest
{
    [MarshalAs(UnmanagedType.I4)] public int npc_id;
    [MarshalAs(UnmanagedType.I4)] public int quest_id;
}

[StructLayout(LayoutKind.Sequential)]
public struct P_QuestAcceptResponse
{
    [MarshalAs(UnmanagedType.I4)] public int quest_id;
    [MarshalAs(UnmanagedType.U1)] public byte result; // 1=성공
    [MarshalAs(UnmanagedType.U1)] public QUEST_STATE state;

    [MarshalAs(UnmanagedType.U2)] public ushort current;
    [MarshalAs(UnmanagedType.U2)] public ushort required;
}

[StructLayout(LayoutKind.Sequential)]
public struct P_QuestProgressNotify
{
    [MarshalAs(UnmanagedType.I4)] public int quest_id;
    [MarshalAs(UnmanagedType.U2)] public ushort current;
    [MarshalAs(UnmanagedType.U2)] public ushort required;
    [MarshalAs(UnmanagedType.U1)] public QUEST_STATE state;
}
// ===================================================