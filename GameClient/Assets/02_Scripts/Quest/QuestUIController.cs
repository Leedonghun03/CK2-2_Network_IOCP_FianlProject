using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestUIController : MonoBehaviour, IPacketReceiver
{
    [Header("Big Quest UI (QuestPanel)")]
    public GameObject questPanel;   // QuestPanel
    public TMP_Text titleText;          // TitleText
    public TMP_Text descText;           // DescText
    public TMP_Text rewardText;         // RewardText
    public Button acceptButton;     // AcceptButton
    public Button closeButton;      // CloseButton

    [Header("Small Quest UI (PopUpWindow)")]
    public GameObject popUpWindow;  // PopUpWindow
    public TMP_Text popTitleText;       // PopUi_TitleText
    public TMP_Text popDescText;        // PopUp_DescText
    public TMP_Text progressText;       // Progress

    // 인벤 담당 친구가 여기 구독해서 커서/마우스 처리하면 됨
    // true = 큰 퀘스트 UI 열림, false = 닫힘
    public event Action<bool> OnQuestPanelActiveChanged;

    private int currentNpcId = -1;
    private int questId = 1;

    private QUEST_STATE state = QUEST_STATE.NONE;
    private ushort current = 0;
    private ushort required = 1;

    private void Awake()
    {
        if (acceptButton) acceptButton.onClick.AddListener(OnClickAccept);
        if (closeButton) closeButton.onClick.AddListener(OnClickClose);
    }

    private void Start()
    {
        Client.TCP.AddPacketReceiver(this);
        HideBig();
        HidePop();
    }

    private void OnDestroy()
    {
        Client.TCP.RemovePacketReceiver(this);
    }

    public bool IsBigOpen => questPanel != null && questPanel.activeSelf;

    public void RequestTalkToNpc(int npcId)
    {
        currentNpcId = npcId;

        P_QuestTalkRequest req = new P_QuestTalkRequest { npc_id = npcId };
        Client.TCP.SendPacket(E_PACKET.QUEST_TALK_REQUEST, req);
    }

    private void ShowBig(string title, string desc, ushort rewardQty)
    {
        titleText.text = title;
        descText.text = desc;
        rewardText.text = $"Reward : Potion x {rewardQty}";

        questPanel.SetActive(true);
        OnQuestPanelActiveChanged?.Invoke(true); // “퀘스트 UI 열림” 신호만
    }

    private void HideBig()
    {
        if (!questPanel) return;
        if (!questPanel.activeSelf) return;

        questPanel.SetActive(false);
        OnQuestPanelActiveChanged?.Invoke(false); // “퀘스트 UI 닫힘” 신호만
    }

    private void ShowPop()
    {
        popTitleText.text = titleText.text;
        popDescText.text = descText.text;
        UpdateProgressText();

        popUpWindow.SetActive(true);
    }

    private void HidePop()
    {
        if (popUpWindow) popUpWindow.SetActive(false);
    }

    private void UpdateProgressText()
    {
        if (progressText)
            progressText.text = $"Monster : {current} / {required}";
    }

    private void OnClickClose()
    {
        // Close: 그냥 큰 UI 닫기. (수락 X)
        // 다시 NPC에서 SPACE 누르면 또 뜨게 됨.
        HideBig();

        if (state == QUEST_STATE.NONE)
            HidePop();
    }

    private void OnClickAccept()
    {
        // 이미 진행중/완료면 중복 수락 방지
        if (state != QUEST_STATE.NONE)
        {
            HideBig();
            ShowPop();
            return;
        }

        state = QUEST_STATE.IN_PROGRESS;
        current = 0;
        required = 1;

        HideBig();
        ShowPop();

        P_QuestAcceptRequest req = new P_QuestAcceptRequest
        {
            npc_id = currentNpcId,
            quest_id = questId
        };

        Client.TCP.SendPacket(E_PACKET.QUEST_ACCEPT_REQUEST, req);
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        switch ((E_PACKET)packet.pbase.packet_id)
        {
            case E_PACKET.QUEST_TALK_RESPONSE:
                {
                    P_QuestTalkResponse res =
                        UnsafeCode.ByteArrayToStructure<P_QuestTalkResponse>(packet.data);

                    questId = res.quest_id;
                    state = res.state;
                    current = res.current;
                    required = res.required;

                    // 1) 아직 안 받음 → 큰 UI 띄워서 Accept/Close
                    if (state == QUEST_STATE.NONE)
                    {
                        ShowBig(res.title, res.desc, res.rewardQty);
                    }
                    // 2) 이미 진행중/완료 → 큰 UI를 띄울 수도 있지만,
                    //    네 요구는 “받고 있으면 작은 UI가 떠야 함”이니까 작은 UI 보여줌.
                    else
                    {
                        // 작은 UI 세팅용으로 텍스트 채우고
                        ShowBig(res.title, res.desc, res.rewardQty);
                        HideBig();

                        ShowPop();
                    }
                    break;
                }

            case E_PACKET.QUEST_ACCEPT_RESPONSE:
                {
                    var res = UnsafeCode.ByteArrayToStructure<P_QuestAcceptResponse>(packet.data);

                    if (res.result == 1)
                    {
                        state = res.state;
                        current = res.current;
                        required = res.required;

                        UpdateProgressText();
                    }
                    else
                    {

                    }
                        break;
                }

            case E_PACKET.QUEST_PROGRESS_NOTIFY:
                {
                    P_QuestProgressNotify n =
                        UnsafeCode.ByteArrayToStructure<P_QuestProgressNotify>(packet.data);

                    if (n.quest_id == questId)
                    {
                        state = n.state;
                        current = n.current;
                        required = n.required;

                        UpdateProgressText(); // Monster : 0/1 → 1/1
                    }
                    break;
                }
        }
    }
}
