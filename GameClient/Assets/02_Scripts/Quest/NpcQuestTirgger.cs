using UnityEngine;

public class NpcQuestTrigger : MonoBehaviour
{
    public int npcId = 1;

    [SerializeField] private QuestUIController ui;
    private bool inRange;

    private void Start()
    {
        if (ui == null)
            Debug.LogError("[NpcQuestTrigger] QuestUIController가 연결되지 않았습니다. Inspector에서 연결하세요.");
    }

    private void OnTriggerEnter(Collider other)
    {
        Player p = other.GetComponent<Player>();
        if (p != null && p.IsLocal) inRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        Player p = other.GetComponent<Player>();
        if (p != null && p.IsLocal) inRange = false;
    }

    private void Update()
    {
        if (!inRange || ui == null) return;
        if (ui.IsBigOpen) return;

        if (Input.GetKeyDown(KeyCode.Space))
            ui.RequestTalkToNpc(npcId);
    }
}
