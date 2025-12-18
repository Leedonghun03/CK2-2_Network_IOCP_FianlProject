using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public unsafe class Match : MonoBehaviour, IPacketReceiver
{
    public static Match Current;
    public Dictionary<long, Player> Players;

    [Header("Player Settings")]
    public GameObject PlayerPrefab;  // Inspector에서 할당
    public Transform SpawnZone;      // SpawnZone Transform (Inspector에서 할당)

    // 스폰존 크기 (스폰존이 Box Collider나 특정 영역이라면)
    public Vector3 SpawnZoneSize = new Vector3(10f, 0f, 10f);

    void Awake()
    {
        Debug.Log("Match started");
        Current = this;
        Players = new Dictionary<long, Player>();
        Client.TCP.AddPacketReceiver(this);
        Client.UDP.AddPacketReceiver(this);
    }

    void Start()
    {
        // 로컬 플레이어 생성
        Vector3 spawnPos = GetRandomSpawnPosition();
        AddPlayer(LocalPlayerInfo.ID, LocalPlayerInfo.Name, spawnPos);

        P_PlayerJoined playerJoined = new P_PlayerJoined()
        {
            id = LocalPlayerInfo.ID,
            name = LocalPlayerInfo.Name
        };
        Client.TCP.SendPacket(E_PACKET.PLAYER_JOINED, playerJoined);
    }

    // 스폰존 내 랜덤 위치 반환
    private Vector3 GetRandomSpawnPosition()
    {
        if (SpawnZone != null)
        {
            // SpawnZone의 위치를 중심으로 SpawnZoneSize 범위 내에서 랜덤 생성
            float randomX = Random.Range(-SpawnZoneSize.x / 2f, SpawnZoneSize.x / 2f);
            float randomZ = Random.Range(-SpawnZoneSize.z / 2f, SpawnZoneSize.z / 2f);

            Vector3 spawnPos = SpawnZone.position + new Vector3(randomX, 0, randomZ);

            // 지형 높이에 맞추기 (옵션)
            if (Physics.Raycast(spawnPos + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f))
            {
                spawnPos.y = hit.point.y;
            }

            return spawnPos;
        }
        else
        {
            // SpawnZone이 없으면 기본 위치
            Debug.LogWarning("SpawnZone이 설정되지 않았습니다. 기본 위치를 사용합니다.");
            return new Vector3(0, 1, 0);
        }
    }

    private void OnGUI()
    {
        foreach (Player player in Players.Values)
        {
            if (player.ID != LocalPlayerInfo.ID)
            {
                Vector3 scpos = GameObject.Find("Player Camera").GetComponent<Camera>().WorldToScreenPoint(player.transform.position);
                if (scpos.z > 0)
                {
                    GUI.contentColor = Color.cyan;
                    GUI.Label(new Rect(scpos.x, Screen.height - scpos.y, 100, 25), player.Name);
                }
            }
        }
    }

    void OnDestroy()
    {
        Client.TCP.RemovePacketReceiver(this);
        Client.UDP.RemovePacketReceiver(this);
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        byte packetId = packet.pbase.packet_id;
        switch ((E_PACKET)packetId)
        {
            case E_PACKET.PLAYER_JOINED:
                P_PlayerJoined playerJoined = UnsafeCode.ByteArrayToStructure<P_PlayerJoined>(packet.data);
                // 다른 플레이어는 기본 위치에 생성 (서버에서 위치 받을 예정)
                AddPlayer(playerJoined.id, playerJoined.name, Vector3.zero);
                Debug.Log($"Player {playerJoined.name} has joined");
                break;

            case E_PACKET.CREATE_MATCH_PLAYER:
                P_CreateMatchPlayer matchPlayer = UnsafeCode.ByteArrayToStructure<P_CreateMatchPlayer>(packet.data);
                Player newPlayer = AddPlayer(matchPlayer.id, matchPlayer.name, matchPlayer.position);
                if (newPlayer != null)
                {
                    newPlayer.transform.position = matchPlayer.position;
                    newPlayer.transform.rotation = matchPlayer.rotation;
                }
                break;

            case E_PACKET.PLAYER_LEFT:
                P_PlayerLeft playerLeft = UnsafeCode.ByteArrayToStructure<P_PlayerLeft>(packet.data);
                RemovePlayer(playerLeft.id);
                break;

            case E_PACKET.UPDATE_PLAYER_MOVEMENT:
                P_UpdatePlayerMovement updateMovement = UnsafeCode.ByteArrayToStructure<P_UpdatePlayerMovement>(packet.data);

                // 로컬 플레이어는 서버 업데이트 무시
                if (updateMovement.player_id == LocalPlayerInfo.ID)
                {
                    // 로컬 플레이어는 이미 클라이언트에서 이동했으므로 무시
                    break;
                }

                // 다른 플레이어만 서버 업데이트 적용
                if (Players.TryGetValue(updateMovement.player_id, out Player player) && player != null)
                {
                    player.Movement.Move(updateMovement.motion);
                    // 회전도 업데이트
                    player.transform.rotation = updateMovement.rotation;
                }
                break;

            default:
                break;

        }
    }

    // 수정: position 파라미터 추가
    private Player AddPlayer(long id, string playerName, Vector3 position)
    {
        if (Players == null || Players.ContainsKey(id))
            return null;

        bool local = LocalPlayerInfo.ID == id;

        // Sphere 대신 프리팹 사용
        GameObject playerObj;

        if (PlayerPrefab != null)
        {
            // 프리팹 인스턴스화
            playerObj = Instantiate(PlayerPrefab, position, Quaternion.identity);
            playerObj.name = playerName;  // 이름 설정
        }
        else
        {
            // 프리팹이 없으면 기본 Sphere 사용 (백업)
            Debug.LogWarning("PlayerPrefab이 설정되지 않았습니다. 기본 Sphere를 사용합니다.");
            playerObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playerObj.name = playerName;
            playerObj.transform.position = position;
        }

        // PlayerMovement 컴포넌트 추가/가져오기
        PlayerMovement playerMovement = playerObj.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            playerMovement = playerObj.AddComponent<PlayerMovement>();
        }

        // CharacterController 추가/가져오기
        CharacterController controller = playerObj.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = playerObj.AddComponent<CharacterController>();
            // CharacterController 설정 (프리팹에 맞게 조정)
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1f, 0);
        }
        playerMovement.Controller = controller;

        // 로컬 플레이어면 카메라 추가
        if (local)
        {
            GameObject cameraObject = new GameObject($"Player Camera");
            Camera playerCamera = cameraObject.AddComponent<Camera>();

            // MouseLook 대신 ThirdPersonCamera 추가
            ThirdPersonCamera thirdPersonCam = cameraObject.AddComponent<ThirdPersonCamera>();

            // 카메라 타겟 설정
            thirdPersonCam.target = playerObj.transform;

            // 카메라 설정 (Inspector에서도 조정 가능)
            thirdPersonCam.distance = 5.0f;
            thirdPersonCam.height = 2.0f;
            thirdPersonCam.smoothSpeed = 10.0f;
            thirdPersonCam.mouseSensitivity = 500f;
            thirdPersonCam.checkCollision = true;

            // 카메라 초기 위치 설정
            cameraObject.transform.position = playerObj.transform.position + new Vector3(0, 2, -5);

            // PlayerMovement에 카메라 참조 전달
            playerMovement.SetCamera(thirdPersonCam);

            // 전투 컴포넌트 추가
            PlayerCombat combat = playerObj.GetComponent<PlayerCombat>();
            if (combat == null)
            {
                combat = playerObj.AddComponent<PlayerCombat>();

                // 설정 (Inspector에서 보이는 값들)
                combat.attackRange = 2.0f;
                combat.attackWidth = 1.5f;
                combat.attackHeight = 2.0f;
                combat.attackDamage = 25;
                combat.attackCooldown = 0.5f;
                combat.hitboxDuration = 0.2f;
                combat.showDebugBox = true;
            }
        }

        // Player 컴포넌트 추가/가져오기
        Player player = playerObj.GetComponent<Player>();
        if (player == null)
        {
            player = playerObj.AddComponent<Player>();
        }
        player.ID = id;
        player.Name = playerName;
        player.Movement = playerMovement;
        player.IsLocal = local;

        Players.Add(id, player);
        return player;
    }

    private void RemovePlayer(long id)
    {
        if (Players != null && Players.TryGetValue(id, out Player player) && player != null)
        {
            Destroy(player.gameObject);
            Players.Remove(id);
        }
    }
}
