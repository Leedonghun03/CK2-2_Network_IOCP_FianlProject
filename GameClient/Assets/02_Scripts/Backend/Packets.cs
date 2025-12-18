using System.Runtime.InteropServices;
using UnityEngine;

public enum E_PACKET
{
    PLAYER_NAME,
    PLAYER_NAME_SUCCESS,
    PLAYER_JOINED,
    CREATE_MATCH_PLAYER,
    PLAYER_MOVEMENT,
    UPDATE_PLAYER_MOVEMENT,
    SEND_CHAT_MESSAGE,
    RECEIVE_CHAT_MESSAGE,
    PLAYER_LEFT,

    INVENTORY_INFO_REQUEST = 401,
    INVENTORY_INFO_RESPONSE = 402,
    ITEM_ADD_REQUEST = 403,
    ITEM_ADD_RESPONSE = 404,
    ITEM_USE_REQUEST = 406,
    ITEM_USE_RESPONSE = 407,

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