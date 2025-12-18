using System;
using System.Net.Sockets;
using UnityEngine;

public static unsafe class Client
{
    private const string IP = "127.0.0.1";
    public static NetworkClient TCP = new NetworkClient(IP, 5004, ProtocolType.Tcp);
    public static NetworkClient UDP = new NetworkClient(IP, 5025, ProtocolType.Udp);

    public static void Start()
    {
        Debug.Log("=== [Client] Start 시작 ===");  // ★ 추가

        try
        {
            Debug.Log($"[Client] TCP 연결 시도... {IP}:5004");
            TCP.Start();
            Debug.Log("[Client] TCP 연결 성공!");

            Debug.Log($"[Client] UDP 연결 시도... {IP}:5025");
            UDP.Start();
            Debug.Log("[Client] UDP 연결 성공!");

            TCP.OnDisconnect += OnDisconnect;
            Application.wantsToQuit += OnApplicationQuit;

            Debug.Log("[Client] Start 완료!");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Client] Start 에러: {e.Message}\n{e.StackTrace}");
        }
    }

    private static bool OnApplicationQuit()
    {
        Close();
        return true;
    }

    private static void OnDisconnect()
    {
        // do stuff
        // maybe display a message or something
    }

    public static void Close()
    {
        if (TCP != null)
        {
            TCP.Close();
        }
        if (UDP != null)
        {
            UDP.Close();
        }
    }
}
