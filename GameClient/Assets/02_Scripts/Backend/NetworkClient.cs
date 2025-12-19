using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PacketBase
{
    public ushort length;     // 2
    public ushort packet_id;  // 2
    public byte type;         // 1
}

public unsafe struct Packet
{
    public PacketBase pbase;
    public byte[] data;
}

public unsafe class NetworkClient
{
    private const int UDP_MAX_DATA_LENGTH = 512;
    private Socket socket;
    private readonly IPEndPoint endPoint;
    private readonly ProtocolType socketProtocol;
    private readonly SynchronizationContext synchronizationContext;
    private readonly List<IPacketReceiver> packetReceivers = new List<IPacketReceiver>();

    public event Action OnDisconnect;

    public NetworkClient(string ip, int port, ProtocolType protocol)
    {
        endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        socket = new Socket(endPoint.AddressFamily, (protocol == ProtocolType.Udp) ? SocketType.Dgram : SocketType.Stream, protocol);
        socketProtocol = protocol;
        synchronizationContext = SynchronizationContext.Current;
    }

    public void Start()
    {
        Debug.Log($"[NetworkClient] Start - Protocol: {socketProtocol}, EndPoint: {endPoint}");  // ★ 추가

        try
        {
            socket.Connect(endPoint);
            Debug.Log($"[NetworkClient] Connect 성공! {endPoint}");  // ★ 추가

            if (socketProtocol == ProtocolType.Tcp)
            {
                Debug.Log("[NetworkClient] TCP ReadThread 시작");  // ★ 추가
                new Thread(ReadTcpDataThread).Start();
            }
            else if (socketProtocol == ProtocolType.Udp)
            {
                Debug.Log("[NetworkClient] UDP ReadThread 시작");  // ★ 추가
                new Thread(ReadUdpDataThread).Start();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[NET] Send failed: {e}");
            Close();
        }
    }

    private void ReadUdpDataThread()
    {
        int headerSize = Marshal.SizeOf<PacketBase>();
        byte[] clientBuffer = new byte[UDP_MAX_DATA_LENGTH];
        EndPoint ep = socket.RemoteEndPoint;

        while (socket != null)
        {
            int bytesReceived = 0;
            try { bytesReceived = socket.ReceiveFrom(clientBuffer, 0, UDP_MAX_DATA_LENGTH, SocketFlags.None, ref ep); }
            catch { break; }

            if (bytesReceived >= headerSize)
            {
                PacketBase pb = default;
                pb.length = BitConverter.ToUInt16(clientBuffer, 0);
                pb.packet_id = BitConverter.ToUInt16(clientBuffer, 2);
                pb.type = clientBuffer[4];

                if (pb.length < headerSize || pb.length > bytesReceived) continue;

                Packet packet = default;
                packet.pbase = pb;
                packet.data = UnsafeCode.SubArray(clientBuffer, headerSize, pb.length - headerSize);

                synchronizationContext.Post(_ => HandlePacket(packet), null);
            }

            Thread.Sleep(50);
        }

        Close();
    }

    private void ReadTcpDataThread()
    {
        int headerSize = Marshal.SizeOf<PacketBase>(); // 5
        int offset = 0;

        byte[] packetBaseBuffer = new byte[headerSize];
        byte[] clientBuffer = null;
        bool bBase = false;

        while (socket != null)
        {
            if (!bBase)
            {
                int received = 0;
                try { received = socket.Receive(packetBaseBuffer, offset, headerSize - offset, SocketFlags.None); }
                catch { break; }

                offset += received;
                bBase = (offset == headerSize);
            }
            else
            {
                Packet packet = default;
                packet.pbase = UnsafeCode.ByteArrayToStructure<PacketBase>(packetBaseBuffer);

                if (packet.pbase.length < headerSize) { break; } // 방어

                if (clientBuffer == null)
                {
                    clientBuffer = new byte[packet.pbase.length];
                    Buffer.BlockCopy(packetBaseBuffer, 0, clientBuffer, 0, headerSize);
                }

                int received = 0;
                try { received = socket.Receive(clientBuffer, offset, packet.pbase.length - offset, SocketFlags.None); }
                catch { break; }

                if (received > 0)
                {
                    offset += received;
                    if (offset < packet.pbase.length) continue;

                    packet.data = UnsafeCode.SubArray(clientBuffer, headerSize, packet.pbase.length - headerSize);
                    synchronizationContext.Post(_ => HandlePacket(packet), null);

                    clientBuffer = null;
                    offset = 0;
                    bBase = false;
                }
                else break;
            }
        }

        Close();
        OnDisconnect?.Invoke();
    }


    public void HandlePacket(Packet packet)
    {
        for (int i = 0; i < packetReceivers.Count; i++)
        {
            packetReceivers[i].OnPacketReceived(packet);
        }
    }

    private void SendData(E_PACKET packetId, byte[] data)
    {
        if (socket == null) return;

        int headerSize = Marshal.SizeOf<PacketBase>(); // 5
        int sz = data.Length + headerSize;

        byte[] buff = new byte[sz];

        // length (ushort)
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)sz), 0, buff, 0, 2);
        // packet_id (ushort)
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)packetId), 0, buff, 2, 2);
        // type (byte) - 서버에서 거의 안 쓰면 0으로
        buff[4] = 0;

        // payload
        Buffer.BlockCopy(data, 0, buff, headerSize, data.Length);

        try
        {
            if (socketProtocol == ProtocolType.Tcp) socket.Send(buff);
            else socket.SendTo(buff, endPoint);
        }
        catch { Close(); }
    }

    public void SendPacket(E_PACKET packetId)
    {
        if (socket != null)
        {
            int sz = sizeof(PacketBase);
            byte[] buff = new byte[sz];
            buff[0] = (byte)packetId;
            byte[] sizeInBytes = BitConverter.GetBytes(sz);
            Buffer.BlockCopy(sizeInBytes, 0, buff, sizeof(byte), sizeInBytes.Length);
            try
            {
                socket.Send(buff);
            }
            catch { Close(); }
        }
    }

    public void SendHitReport(long enemyId, int damage, Vector3 hitPos, uint seq)
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

        SendPacket(E_PACKET.HIT_REPORT, pkt);
    }

    public void SendPacket(E_PACKET packetId, object packet)
    {
        int size = Marshal.SizeOf(packet);
        byte* ptr = stackalloc byte[size];
        IntPtr ptr2 = (IntPtr)ptr;
        Marshal.StructureToPtr(packet, ptr2, false);
        byte[] data = new byte[size];
        Marshal.Copy(ptr2, data, 0, size);
        SendData(packetId, data);
    }

    public void AddPacketReceiver(IPacketReceiver item)
    {
        if (!packetReceivers.Contains(item))
        {
            packetReceivers.Add(item);
        }
    }

    public void RemovePacketReceiver(IPacketReceiver item)
    {
        packetReceivers.Remove(item);
    }

    public void Close()
    {
        if (socket != null)
        {
            socket.Dispose();
            socket = null;
        }
    }
}
