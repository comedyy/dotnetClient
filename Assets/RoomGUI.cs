using System.Collections;
using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

enum State
{
    NotCreate,
    Joined,
    Battle,
}

public class RoomGUI : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    RoomInfoMsg[] roomList;
    public RoomUser[] _userList => ClientBattleRoomMgr.Instance()._userList;
    bool _canQuery = true;

    public Func<(BattleStartMessage, BattleStartShowInfo)> GetStartMessage;
    public Func<(JoinMessage, JoinMessageShowInfo)> GetJoinMessage;
    int userId 
    {
        get
        {
            var y = PlayerPrefs.GetInt(Application.dataPath +"netPlayerId");
            if(y == 0)
            {
                y = UnityEngine.Random.Range(1000, 10000000);
                PlayerPrefs.SetInt(Application.dataPath + "netPlayerId", y);
            }
            return y;
        }
    }

    void Start()
    {
        var mono = gameObject.AddComponent<LocalServerMono>();
        // if(AutoCreateLocalServer)
        // {
        //     mono.StartServer();
        //     ip = "127.0.0.1";
        // }


        gameObject.AddComponent<GameLogicGUI>().Init(this, userId);
        ClientBattleRoomMgr.Instance().SetUserId(userId);
        ClientBattleRoomMgr.Instance().enableLog = true;
    }


    public static string ip;
    public static int port;
    string _ipMac;
    void OnGUI()
    {
        if(ClientBattleRoomMgr.Instance()._roomState == TeamRoomState.InSearchRoom)
        {
            DrawOutsideRoom();
        }
        else if(ClientBattleRoomMgr.Instance()._roomState == TeamRoomState.InRoom)
        {
            DrawInsideRoom();
        }
        else if(ClientBattleRoomMgr.Instance()._roomState == TeamRoomState.InBattle)
        {
            // if(LocalFrame.Instance != null && LocalFrame.Instance._clientStageIndex < 1)
            // {
            //     GUI.color = Color.red;
            //     for(int i = 0; i < _userList.Length; i++)
            //     {
            //         int widthIndex = 0;
            //         GUI.Label(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), _userList[i].name);
            //         GUI.Label(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), $"{_userList[i].HeroId};{_userList[i].heroLevel};{_userList[i].heroStar}");

            //         ClientBattleRoomMgr.Instance()._dicLoadingProcess.TryGetValue((int)_userList[i].userId, out var process);
            //         GUI.Label(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), $"process:{process}");
            //     }
            // }
            // else if(LocalFrame.Instance != null && LocalFrame.Instance is LocalFrameNetGame netPing)
            // {
            //     GUI.Label(new Rect(Screen.width - 300, 0, 100, 50), $"ping:{netPing.SocketRoundTripTime}");

            //     var logicPing = netPing.SocketRoundTripLogicTime;
            //     var processPerSec = netPing.FrameProcessPerSec;
            //     var receivePerSec = netPing.ReceiveFramePerSec;
            //     GUI.Label(new Rect(Screen.width - 200, 0, 200, 50), $"L:{logicPing} P:{processPerSec} R:{receivePerSec}");
            // }
        }
        
        // BattleCore.OnBattleEnd += OnBattleEnd;
    }

    void DrawOutsideRoom()
    {
        // show all Rooms
        if(roomList != null)
        {
            GUI.color = Color.red;
            var roomCount = roomList.Length;
            if(roomCount == 0)
            {
                GUI.Label(new Rect(0, 50, 300, 50), $"暂无房间");
            }
            else
            {
                for(int i = 0; i < roomCount; i++)
                {
                    GUI.Label(new Rect(0, i * 50, 300, 50), $"ID: {roomList[i].updateRoomMemberList.roomId} userCount: {roomList[i].updateRoomMemberList.userList.Length}");
                    if(GUI.Button(new Rect(400, i * 50, 100, 50), "加入"))
                    {
                        // OnClickJoin(false, roomList[i].roomId, delay);
                        (var joinMessage, var joinShowInfo) = GetJoinMessage();
                        JoinAsync(roomList[i].updateRoomMemberList.roomId, NetUtils.GetBytes(joinMessage), NetUtils.GetBytes(joinShowInfo));
                    }
                }
            }

            GUI.color = Color.white;
        }
        
        if(GUI.Button(new Rect(0, 150, 100, 50), "创建"))
        {
            (var startMessage, var startShowInfo) = GetStartMessage();

            (var joinMessage, var joinShowInfo) = GetJoinMessage();
            
            ClientBattleRoomMgr.Instance().CreateRoom(NetUtils.GetBytes(startMessage), NetUtils.GetBytes(startShowInfo), 
                NetUtils.GetBytes(joinMessage), NetUtils.GetBytes(joinShowInfo));
        }

        ip = GUI.TextField(new Rect(100, 150, 100, 50), ip);
        if(_canQuery && GUI.Button(new Rect(200, 150, 100, 50), "查询房间"))
        {
            QueryAvailableRooms();
        }


        if( LocalServerMono.Instance != null && !LocalServerMono.Instance.isStartBattle)
        {
            if(GUI.Button(new Rect(300, 150, 100, 50), "启用本地服务器"))
            {
                LocalServerMono.Instance.StartServer();
                ip = "127.0.0.1";
                ClientBattleRoomMgr.Instance().ChangeIp(ip, 10055);
            }
            // else if(GUI.Button(new Rect(400, 150, 100, 50), "MAC服务器"))
            // {
            //     ip = _ipMac;
            // }

            // else if(GUI.Button(new Rect(500, 150, 100, 50), "外网服务器"))
            // {
            //     ip = "106.75.214.130";
                
            // }
            if(ClientBattleRoomMgr.Instance().ServerUserState == GetUserStateMsg.UserState.HasRoom 
                ||  ClientBattleRoomMgr.Instance().ServerUserState == GetUserStateMsg.UserState.HasBattle 
                && GUI.Button(new Rect(700, 150, 100, 50), "战斗")) 
            {
                // OnClickReconnect();
                ClientBattleRoomMgr.Instance().ReconnectToServer(TeamConnectParam.SyncInfo);
            }
            else if(GUI.Button(new Rect(800, 150, 100, 50), "检测状态")) 
            {
                ClientBattleRoomMgr.Instance().CheckRoomState();
            }
        }
        else
        {
            GUI.Label(new Rect(300 , 150, 100, 50), "本地服务器已经开启");
        }
    }

    private async void JoinAsync(int roomId, byte[] bytes, byte[] showInfo)
    {
        var ret = await ClientBattleRoomMgr.Instance().JoinRoom(roomId, bytes, showInfo);
        if(ret != TeamRoomEnterFailedReason.OK)
        {
            Debug.LogError("join failed" + ret);
        }
    }

    private void DrawInsideRoom()
    {
        if(_userList != null)
        {
            GUI.color = Color.red;
            var iAmRoomMaster = _userList[0].userId == userId;

            for(int i = 0; i < _userList.Length; i++)
            {
                int widthIndex = 0;
                GUI.Label(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), _userList[i].name);
                GUI.Label(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), $"{_userList[i].pen}");
                GUI.Label(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), _userList[i].userId.ToString());
                GUI.Label(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), $"在线：{_userList[i].isOnLine}");
                GUI.Label(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), $"准备：{_userList[i].isReady}");

                var isSelf = _userList[i].userId == userId;
                var currentIsRoomMaster = i == 0;
                if(isSelf)  // 自己的操作。
                {
                    if(GUI.Button(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), "退出"))
                    {
                        ClientBattleRoomMgr.Instance().LeaveRoom();
                    }

                    if(GUI.Button(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), "断线"))
                    {
                        ClientBattleRoomMgr.Instance().DEBUG_Disconnect();
                    }

                    if(currentIsRoomMaster)
                    {
                        if(iAmRoomMaster && GUI.Button(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), "开始") )
                        {
                            ClientBattleRoomMgr.Instance().StartRoom();
                        }
                    }
                    else
                    {
                        if(GUI.Button(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), $"ready：{_userList[i].isReady}"))
                        {
                            ClientBattleRoomMgr.Instance().ReadyRoom(!_userList[i].isReady);
                        }
                    }
                }
                else if(iAmRoomMaster) // 群主对别人的操作。
                {
                    if(GUI.Button(new Rect((widthIndex++) * 100, i * 50 + 400, 100, 50), "踢出"))
                    {
                        ClientBattleRoomMgr.Instance().KickUser((int)_userList[i].userId);
                    }
                }
            }

            GUI.color = Color.white;
        }
    }

    private async void QueryAvailableRooms()
    {
        _canQuery = false;
        roomList = await ClientBattleRoomMgr.Instance().QueryRoomListAsync();
        _canQuery = true;
    }
}
