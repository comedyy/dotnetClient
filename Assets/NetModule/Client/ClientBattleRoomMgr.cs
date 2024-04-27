using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Assertions;

public enum TeamRoomEnterFailedReason
{
    OK,
    Level,
    Star,
    SelfVersionTooLow,
    SelfVersionTooHigh,
    RoomNotExist,
    ConnectionFailed,
    JustInsideRoom,
}

public enum TeamRoomState
{
    InSearchRoom,
    InRoom,
    InBattle,
}

public class ClientBattleRoomMgr : MonoBehaviour
{
    int _overrideUserId;
    GameClientSocket _socket;
    public int UserId{
        get{
            return _overrideUserId;
        }
    }
    public HashSet<int> _offListUsers = new HashSet<int>();
    
    public RoomUser[] _userList;
    public UpdateRoomMemberList _updateRoomInfo;
    public bool IsLastBattleQuitMember{get; private set;}
    public RoomInfoMsg[] _roomMsgList;
    public List<RoomInfoMsg> _canShowList = new List<RoomInfoMsg>();
    Dictionary<int, UpdateRoomMemberList> _dicRoomInfo = new Dictionary<int, UpdateRoomMemberList>();
    public int enterRoomId{get; private set;}
    public TeamRoomState _roomState {get; private set;}= TeamRoomState.InSearchRoom;
    public GetUserStateMsg.UserState ServerUserState{get; private set;} = GetUserStateMsg.UserState.None;
    public string _battleGUID = "";

    public Action OnConnect;
    const int MAX_RETRY_COUNT = 2;
    int _reconnectCount = 0;
    TeamConnectParam _connectToServerParam = TeamConnectParam.None;


    public Dictionary<int, int> _dicLoadingProcess = new Dictionary<int, int>();

    public event Action<TeamRoomState, TeamRoomState> OnSwitchState;
    public event Action OnTeamInfoChange;
    public event Action<int> OnTeamRoomInfoChange;
    public event Action<JoinMessage> OnGetUserJoinInfo;
    public event Action OnQueryRoomList;
    public event Action<int> OnUserQuit;
    public event Action<BattleStartMessage, IClientGameSocket> OnBattleStart;

    public static ClientBattleRoomMgr CreateInstance()
    {
        var  instance = new GameObject().AddComponent<ClientBattleRoomMgr>();
        instance.Init();
        return instance;
    }

    public void Init()
    {
        // var ip = "101.132.100.216";
        var ip = "127.0.0.1";
        var port = 10055;
        _socket = new GameClientSocket(ip, port, 0, LogMessage);
        _socket.OnConnected = OnConnected;
        _socket.OnDisConnected = OnDisConnected;
        _socket.OnReceiveMsg += OnReceiveMessage;
    }

    private async void OnDisConnected()
    {
        // try max
        if(_reconnectCount >= MAX_RETRY_COUNT)
        {
            SwitchRoomState(TeamRoomState.InSearchRoom);
            return;
        }

        // check reconnect
        if(_roomState != TeamRoomState.InSearchRoom)
        {
            await Task.Delay(100);
            _reconnectCount ++;
            ReconnectToServer(TeamConnectParam.SyncInfo);
        }
    }

    private void OnConnected()
    {
        LogMessage("onConnected");

        _reconnectCount = 0;
        _socket.SendMessage(new RoomUserIdMsg(){
            userId = UserId, connectParam = _connectToServerParam
        });
        _connectToServerParam = default;
        
        OnConnect?.Invoke();
    }


    private void OnReceiveMessage(NetDataReader reader)
    {
        var msgType = (MsgType1)reader.PeekByte();
        if(msgType < MsgType1.ServerMsgEnd___)
        {
            return;
        }

        LogMessage(UserId + "<<<<<<<<<<<===== " + msgType);

        if(msgType == MsgType1.GetAllRoomList)
        {
            _roomMsgList = reader.Get<RoomListMsg>().roomList;
            foreach(var x in _roomMsgList)
            {
                _dicRoomInfo[x.updateRoomMemberList.roomId] = x.updateRoomMemberList;
            }
            OnQueryRoomList?.Invoke();
        }
        else if(msgType == MsgType1.SyncRoomMemberList)
        {
            var msg = reader.Get<UpdateRoomMemberList>();
            _userList = msg.userList;
            _updateRoomInfo = msg;
            enterRoomId = msg.roomId;

            _dicRoomInfo[enterRoomId] = msg;

            // if(_userList.Length > 0)
            // {
            //     var selfIndex = Array.FindIndex(_userList, m=>m.isOnLine);
            //     if(selfIndex >= 0 && _userList[selfIndex].userId == UserId)
            //     {
            //         for(int i = 0; i < _userList.Length; i++)
            //         {
            //             if(!_userList[i].isOnLine) 
            //             {
            //                 _offListUsers.Add(i);
            //             }
            //         }

            //         if(LocalFrame.Instance != null)
            //         {
            //             LocalFrame.Instance.SetHelpAi(_offListUsers, _offListUsers.Count);
            //         }
            //     }
            // }

            if(_userList.Length == 0)
            {
                SwitchRoomState(TeamRoomState.InSearchRoom);
            }
            else if(_roomState == TeamRoomState.InSearchRoom)
            {
                SwitchRoomState(TeamRoomState.InRoom);
            }

            OnTeamInfoChange?.Invoke();
        }
        else if(msgType == MsgType1.RoomEventSync)
        {
            var msg = reader.Get<SyncRoomOptMsg>();
            if(msg.param == UserId && msg.state == SyncRoomOptMsg.RoomOpt.Kick)
            {
                // Tip.CreateTip(590038).Show();
            }

            var onlyNotice = (msg.state == SyncRoomOptMsg.RoomOpt.Leave || msg.state == SyncRoomOptMsg.RoomOpt.Kick || msg.state == SyncRoomOptMsg.RoomOpt.Join) && msg.param != UserId;
            if(onlyNotice)
            {
                var param = msg.param;
                var user = _userList.FirstOrDefault(m=>m.userId == param);
                if(user.userId != 0)
                {
                    // Tip.CreateTip(msg.state == SyncRoomOptMsg.RoomOpt.Join ? 590014 : 590015, user.name).Show();
                }

                if(msg.state == SyncRoomOptMsg.RoomOpt.Leave || msg.state == SyncRoomOptMsg.RoomOpt.Kick)
                {
                    OnUserQuit?.Invoke(msg.param);
                }
                return;
            }

            if(msg.state == SyncRoomOptMsg.RoomOpt.Leave || msg.state == SyncRoomOptMsg.RoomOpt.Kick || msg.state == SyncRoomOptMsg.RoomOpt.MasterLeaveRoomEnd)
            {
                var teamMaster = isTeamMaster;
                _userList = null;
                _updateRoomInfo = default;
                enterRoomId = 0;

                SwitchRoomState(TeamRoomState.InSearchRoom, false);
            }
        }
        else if(msgType == MsgType1.ErrorCode)
        {
            var msg = reader.Get<RoomErrorCode>();
            LogMessage(msg.roomError.ToString());

            if(msg.roomError == RoomError.RoomFull)
            {
                // Alert.CreateAlert(590004, null, false).Show();
            }
            else if(msg.roomError == RoomError.RoomNotExist)
            {
                // Alert.CreateAlert(590042, null, false).Show();
            }
            else if(msg.roomError == RoomError.JoinRoomErrorInsideRoom)
            {
                // Alert.CreateAlert(590045, null, false).Show();
            }
            else
            {
                // Alert.CreateAlert(msg.roomError.ToString());
            }

            if(msg.roomError == RoomError.RoomFull || msg.roomError == RoomError.RoomNotExist)
            {
                QueryRoomList();
            }
        }
        else if(msgType == MsgType1.GetUserState)
        {
            var msg = reader.Get<GetUserStateMsg>();
            LogMessage(msg.state.ToString());
            ServerUserState = msg.state;
            Debug.LogError(ServerUserState);
        }
        else if(msgType == MsgType1.RoomStartBattle)
        {
            // IsLastBattleQuitMember = false;

            var roomStartBattle = reader.Get<RoomStartBattleMsg>();
            BattleStartMessage startMessage = NetUtils.ReadObj<BattleStartMessage>(roomStartBattle.StartMsg);

            startMessage.joins = new JoinMessage[roomStartBattle.joinMessages.Count];
            for(int i = 0; i < roomStartBattle.joinMessages.Count; i++)
            {
                startMessage.joins[i] = NetUtils.ReadObj<JoinMessage>(roomStartBattle.joinMessages[i]);
            }

            OnBattleStart.Invoke(startMessage, _socket);
            // NewBehaviourScript.OnBattleStart(startMessage, _socket);

            SwitchRoomState(TeamRoomState.InBattle);

            _battleGUID = startMessage.guid;
        }
    }

    public void OnMemberLeaveBattle()
    {
        SwitchRoomState(TeamRoomState.InSearchRoom, false, false);
        enterRoomId = 0;
        _updateRoomInfo = default;
        IsLastBattleQuitMember = true;
    }

    void SwitchRoomState(TeamRoomState state, bool notifyRoomEnd = true, bool updateRoomState = true)
    {
        if(_roomState == state) return;

        var fromState = _roomState;
        _roomState = state;
        if(_roomState == TeamRoomState.InSearchRoom)
        {
            // 断开连接
            _socket.DisConnect();

            // if(notifyRoomEnd && LocalFrame.Instance is LocalFrameNetGame netGame)
            // {
            //     netGame.OnTeamRoomEnd(590041);
            // }

            // 请求roomList
            QueryRoomList();
        }
        else if(_roomState == TeamRoomState.InBattle)
        {
            _dicLoadingProcess.Clear();
            _offListUsers.Clear();
        }
        else if(_roomState == TeamRoomState.InRoom)
        {
        }

        OnSwitchState?.Invoke(fromState, _roomState);
    }

    void Update()
    {
        _socket.Update(0);
    }

    void OnDestroy()
    {
        _socket?.OnDestroy();
    }

    public bool GetRoomInfo(int id, out UpdateRoomMemberList msg)
    {
        return _dicRoomInfo.TryGetValue(id, out msg);
    }

    public void QueryRoomInfo(int id)
    {
        if(_dicRoomInfo.TryGetValue(id, out var roomInfo) && roomInfo.roomId == 0)
        {
            OnTeamRoomInfoChange?.Invoke(id);
            return; // 房间已经解散
        }

        _socket.SendUnConnectedMessage(new GetRoomStateMsg(){idRoom = id});
    }

    public void QueryRoomList()
    {
        _socket.SendUnConnectedMessage(new RoomListMsgRequest());
    }


    public void QueryRoomListAsync()
    {
        _socket.SendUnConnectedMessage(new RoomListMsgRequest());
    }
    
    public async Task<TeamRoomEnterFailedReason> JoinRoom(int enterRoomId, byte[] joinBytes, byte[] joinShowInfo)
    {
        var reason = CheckJoinCondition(enterRoomId);
        if(reason != TeamRoomEnterFailedReason.OK)
        {
            return reason;
        }

        if(!await ConnectToServerInner())
        {
            return TeamRoomEnterFailedReason.ConnectionFailed;
        }

        _socket.SendMessage(new JoinRoomMsg(){
            roomId = enterRoomId, 
            joinMessage = joinBytes,
            joinShowInfo = joinShowInfo
        });

        return TeamRoomEnterFailedReason.OK;
    }

    private TeamRoomEnterFailedReason CheckJoinCondition(int enterRoomId)
    {
        if(!_dicRoomInfo.TryGetValue(enterRoomId, out var room) || room.roomId == 0)
        {
            return TeamRoomEnterFailedReason.RoomNotExist;
        }

        // if(!UI_IF_StagePassDetail.CheckVersionOK(room.version))
        // {
        //     var versionSelf = Core.VersionUtil.GetVersionStr();
        //     var isLargerVersion = CompareVersion(room.version, versionSelf);

        //     if(isLargerVersion)
        //     {
        //         return TeamRoomEnterFailedReason.SelfVersionTooLow;
        //     }
        //     else
        //     {
        //         return TeamRoomEnterFailedReason.SelfVersionTooHigh;
        //     }
        // }


        return TeamRoomEnterFailedReason.OK;
    }

    public async void ReconnectToServer(TeamConnectParam syncRoomInfo)
    {
        if(_socket.connectResult == ConnectResult.Connnected)
        {
            _socket.SendMessage(new RoomUserIdMsg(){
                userId = UserId, connectParam = syncRoomInfo
            });
            return;
        }

        _connectToServerParam = syncRoomInfo;
        await ConnectToServerInner();
    }

    private async Task<bool> ConnectToServerInner()
    {
        _socket.Connect();

        while(_socket.connectResult == ConnectResult.Connecting)
        {
            await Task.Delay(100);
        }

        return _socket.connectResult == ConnectResult.Connnected;
    }

    public async void CreateRoom( byte[] startBytes, byte[] roomShowInfo, byte[] joins, byte[] joinShowInfo)
    {
        if(!await ConnectToServerInner())
        {
            return;
        }

        var setting = GetServerSetting();

        _socket.SendMessage(new CreateRoomMsg()
        {
            startBattleMsg = startBytes,
            join = joins,
            joinShowInfo = joinShowInfo,
            roomShowInfo = roomShowInfo,
            setting = setting,
        });
    }

    public async void LeaveRoom()
    {
        if(!await ConnectToServerInner())
        {
            return;
        }

        _socket.SendMessage(new UserLeaveRoomMsg());
    }

    public async void KickUser(int kickedUser)
    {
        if(!await ConnectToServerInner())
        {
            return;
        }

        _socket.SendMessage(new KickUserMsg(){
            userId = kickedUser
        });
    }

    public async void ReadyRoom(bool isReady)
    {
        if(!await ConnectToServerInner())
        {
            return;
        }

        _socket.SendMessage(new RoomReadyMsg(){ isReady = isReady});
    }

    public async void StartRoom()
    {
        if(!await ConnectToServerInner())
        {
            return;
        }

        _socket.SendMessage(new StartBattleRequest());
    }

    public void DEBUG_Disconnect()
    {
        if(_socket != null)
        {
            _socket.DisConnect();
        }
    }

    public void ChangeIp(string ip, int port)
    {
        _socket.SetIp(ip, port);
    }

    public void SetUserId(int userId)
    {
        _overrideUserId = userId;
    }
    
    internal bool enableLog{
        get{
            return PlayerPrefs.GetInt("enableRoomLog", 0) != 0;
        }
        set
        {
            PlayerPrefs.SetInt("enableRoomLog", value ? 1 : 0);
        }
    }

    public bool isTeamMaster
    {
        get
        {
            return enterRoomId > 0 && _userList[0].userId == UserId;
        }
    }

    public bool AllReady { 
        get
        {
            for(int i = 1; i < _userList.Length; i++)
            {
                if(!_userList[i].isReady) return false;
            }

            return true;
        }
     }


    private ServerSetting GetServerSetting()
    {
        return new ServerSetting()
        {
            tick = 0.5f, masterLeaveOpt = RoomMasterLeaveOpt.ChangeRoomMater, 
                maxCount = 3, keepRoomAfterBattle = true, maxSec = 20 * 60, whoCanLeaveRoomInBattle = WhoCanLeaveRoomInBattle.All
        };
    }

    public void LogMessage(string context)
    {
        if(enableLog)
        {
            Debug.LogError(context);
        }
    }

    
    public async Task<GetUserStateMsg.UserState> CheckRoomState()
    {
        if(_roomState == TeamRoomState.InBattle) return GetUserStateMsg.UserState.HasBattle;
        if(_roomState == TeamRoomState.InRoom) return GetUserStateMsg.UserState.HasRoom;

        ServerUserState = GetUserStateMsg.UserState.Querying;
        _socket.SendUnConnectedMessage(new GetUserStateMsg(){userId = UserId});
        for(int i = 0; i < 50; i++)
        {
            await Task.Delay(100);
            if(ServerUserState != GetUserStateMsg.UserState.Querying){
                return ServerUserState;
            }
        }

        return ServerUserState;
    }
}