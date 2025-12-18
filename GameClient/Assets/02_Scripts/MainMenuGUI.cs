using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuGUI : MonoBehaviour, IPacketReceiver
{
    void Awake()
    {
        Debug.Log("=== [MainMenuGUI] Awake 시작 ===");  // ★ 이 로그가 나오나요?

        try
        {
            Client.Start();
            Debug.Log("[MainMenuGUI] Client.Start() 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MainMenuGUI] 에러: {e}");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Client.TCP.AddPacketReceiver(this);
    }

    private string pendingName;

    public unsafe void OnPacketReceived(Packet packet)
    {
        switch ((E_PACKET)packet.pbase.packet_id)
        {
            case E_PACKET.PLAYER_NAME_SUCCESS:
                {
                    var res = UnsafeCode.ByteArrayToStructure<P_LoginResponse>(packet.data);
                    LocalPlayerInfo.ID = res.Result;
                    LocalPlayerInfo.Name = pendingName;

                    SceneManager.LoadSceneAsync("01_Scenes/MatchScene", LoadSceneMode.Single);
                    break;
                }
        }
    }

    public void OnDestroy()
    {
        Client.TCP.RemovePacketReceiver(this);
    }

    public void OnJoinButtonClick()
    {
        string inputName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        pendingName = inputName;

        P_PlayerName playerName = default;
        playerName.name = inputName;

        Client.TCP.SendPacket(E_PACKET.PLAYER_NAME, playerName);
    }
}
