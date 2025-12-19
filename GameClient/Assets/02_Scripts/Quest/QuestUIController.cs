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

    [Header("Reward Quest UI (RewardQuestPanel)")]
    public GameObject rewardQuestPanel;      // RewardQuestPanel
    public TMP_Text rewardTitleText;         // (RewardQuestPanel) TitleText
    public TMP_Text rewardDescText;          // (RewardQuestPanel) DescText
    public TMP_Text rewardRewardText;        // (RewardQuestPanel) RewardText
    public Button rewardAcceptButton;        // (RewardQuestPanel) AcceptButton

    // 인벤 담당 친구가 여기 구독해서 커서/마우스 처리하면 됨
    // true = 큰 퀘스트 UI 열림, false = 닫힘
    public event Action<bool> OnQuestPanelActiveChanged;

    private int currentNpcId = -1;
    private int questId = 1;

    private QUEST_STATE state = QUEST_STATE.NONE;
    private ushort current = 0;
    private ushort required = 1;

    private uint rewardItemID = 0;
    private ushort rewardQty = 0;

    private void Awake()
    {
        if (acceptButton) acceptButton.onClick.AddListener(OnClickAccept);
        if (closeButton) closeButton.onClick.AddListener(OnClickClose);
        if (rewardAcceptButton) rewardAcceptButton.onClick.AddListener(OnClickRewardAccept);

    }

    private void Start()
    {
        Client.TCP.AddPacketReceiver(this);
        HideBig();
        HidePop();
        HideRewardBig();
    }

    private void OnDestroy()
    {
        Client.TCP.RemovePacketReceiver(this);
    }

    public bool IsBigOpen =>
    (questPanel != null && questPanel.activeSelf) ||
    (rewardQuestPanel != null && rewardQuestPanel.activeSelf);

    public void RequestTalkToNpc(int npcId)
    {
        currentNpcId = npcId;

        P_QuestTalkRequest req = new P_QuestTalkRequest { npc_id = npcId };
        Debug.Log("[NPC] Space pressed in range");
        Client.TCP.SendPacket(E_PACKET.QUEST_TALK_REQUEST, req);
        Debug.Log("[NPC] Sent QUEST_TALK_REQUEST(501)");
    }

    private void ShowBig(string title, string desc, ushort rewardQty)
    {
        titleText.text = title;
        descText.text = desc;
        rewardText.text = $"Reward : Potion x {rewardQty}";

        questPanel.SetActive(true);
        OnQuestPanelActiveChanged?.Invoke(true);
    }

    private void HideBig()
    {
        if (!questPanel) return;
        if (!questPanel.activeSelf) return;

        questPanel.SetActive(false);
        OnQuestPanelActiveChanged?.Invoke(false);
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

    private void ShowRewardBig(string title, string desc, ushort qty)
    {
        rewardTitleText.text = title;
        rewardDescText.text = desc;
        rewardRewardText.text = $"Reward : Potion x {qty}";

        rewardQuestPanel.SetActive(true);
        OnQuestPanelActiveChanged?.Invoke(true);
    }

    private void HideRewardBig()
    {
        if (!rewardQuestPanel) return;
        if (!rewardQuestPanel.activeSelf) return;

        rewardQuestPanel.SetActive(false);
        OnQuestPanelActiveChanged?.Invoke(false);
    }

    private void UpdateProgressText()
    {
        if (progressText)
            progressText.text = $"Monster : {current} / {required}";
    }

    private void OnClickClose()
    {
        // 다시 NPC에서 SPACE 누르면 또 뜨게 됨
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

    private void OnClickRewardAccept()
    {
        // 완료 상태가 아니면 무시
        if (state != QUEST_STATE.COMPLETED)
        {
            Debug.LogWarning("[QuestReward] Not completed yet.");
            return;
        }

        P_QuestCompleteRequest req = new P_QuestCompleteRequest
        {
            QuestId = questId
        };
        Client.TCP.SendPacket(E_PACKET.QUEST_COMPLETE_REQUEST, req);

        if (rewardAcceptButton) rewardAcceptButton.interactable = false;
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        switch ((E_PACKET)packet.pbase.packet_id)
        {
            case E_PACKET.QUEST_TALK_RESPONSE:
                {
                    P_QuestTalkResponse res =
                        UnsafeCode.ByteArrayToStructure<P_QuestTalkResponse>(packet.data);

                    var effectiveState = (state == QUEST_STATE.COMPLETED) ? QUEST_STATE.COMPLETED : res.state;

                    questId = res.quest_id;
                    state = effectiveState;
                    current = res.current;
                    required = res.required;
                    rewardItemID = res.rewardItemID;
                    rewardQty = res.rewardQty;

                    if (state == QUEST_STATE.NONE)
                    {
                        HideRewardBig();
                        HidePop();
                        ShowBig(res.title, res.desc, res.rewardQty);
                    }
                    else if (state == QUEST_STATE.IN_PROGRESS)
                    {
                        HideRewardBig();
                        ShowBig(res.title, res.desc, res.rewardQty);
                        HideBig();
                        ShowPop();
                    }
                    else // COMPLETED
                    {
                        HideBig();
                        HidePop();
                        ShowRewardBig(res.title, res.desc, res.rewardQty);
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

            case E_PACKET.QUEST_COMPLETE_RESPONSE:
                {
                    var res = UnsafeCode.ByteArrayToStructure<P_QuestCompleteResponse>(packet.data);

                    // 버튼 원복
                    if (rewardAcceptButton) rewardAcceptButton.interactable = true;

                    if (res.Result == 0) // ERROR_CODE::NONE
                    {
                        Debug.Log("[QuestComplete] Success");

                        // 2) 아이템 지급(현재 서버가 QuestComplete에서 아이템을 안 주므로, ItemAdd로 지급)
                        if (rewardItemID != 0 && rewardQty > 0)
                        {
                            if (InventoryManager.Instance != null)
                                InventoryManager.Instance.RequestAddItem(rewardItemID, rewardQty);
                            else
                                Debug.LogWarning("[QuestReward] InventoryManager.Instance is null");
                        }

                        HideRewardBig();
                        // 완료 후 팝업 유지/숨김은 취향인데, 일단 닫아두는 게 깔끔
                        HidePop();
                    }
                    else
                    {
                        Debug.LogWarning($"[QuestComplete] Failed. Result={res.Result}");
                        // 실패해도 RewardPanel은 유지해도 됨(재시도 가능)
                    }
                    break;
                }
        }
    }
}
