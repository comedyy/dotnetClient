using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;

enum GameState
{
    NotBegin,
    Running,
    End,
}

struct PlayerInfo
{
    public int id;
    public int finishedStageValue;
    public double finishStageTime;

    public int readyStageValue;
    public double readyStageTime;
    public bool isOnLine;
}

public class Server
{
    public ushort _frame;
    public float _totalSeconds;
    public float preFrameSeconds;
    float _tick;
    int _maxKeepSec;

    IServerGameSocket _socket;
    private List<int> _netPeers;
    HashChecker _hashChecker;
    public int _pauseFrame = 0;
    double _pauseTime;
    
    GameState _gameState = GameState.NotBegin;
    private int _stageIndex;
    PlayerInfo[] _playerInfos;
    public Dictionary<int, int> _finishStageFrames = new Dictionary<int, int>();

    FrameMsgBuffer _frameMsgBuffer = new FrameMsgBuffer();
    public RoomStartBattleMsg _startMessage;
    float _waitFinishStageTime = 0;
    float _waitReadyStageTime = 0;
    double _roomTime;
    double _roomStartTime;
    int _MaxManualPauseSec;

    public Server(ServerSetting serverSetting, IServerGameSocket socket, List<int> netPeers, double roomTime)
    {
        _frame = 0;
        _totalSeconds = 0;
        preFrameSeconds = 0;
        _tick = serverSetting.tick;
        _socket = socket;
        _netPeers = netPeers;
        _maxKeepSec = serverSetting.maxSec == 0 ? 60 * 20 : serverSetting.maxSec;

        _waitFinishStageTime = serverSetting.waitFinishStageTimeMs == 0 ? 10 : serverSetting.waitFinishStageTimeMs / 1000f;
        _waitReadyStageTime = serverSetting.waitReadyStageTimeMs == 0 ? 10 : serverSetting.waitReadyStageTimeMs / 1000f;
        _MaxManualPauseSec = Math.Min(serverSetting.pauseMaxSecond, 30);
        _roomStartTime = roomTime;

        _pauseFrame = 0;
        _pauseTime = float.MaxValue;
    }

    public void Update(float deltaTime, double roomTime)
    {
        _roomTime = roomTime;
        if(_gameState != GameState.Running) return;
        
        if((_roomTime - _roomStartTime) >= _maxKeepSec) // timeout
        {
            _gameState = GameState.End;
            _socket.SendMessage(_netPeers, new ServerCloseMsg());
            return;
        }

        UpdateReadyNextStageRoom();

        if(_pauseFrame <= _frame) // pause
        {
            UpdatePauseWhenPause();// 用户手动暂停
            return; 
        }

        UpdateFinishRoom();

        _totalSeconds += deltaTime;
        if(preFrameSeconds + _tick > _totalSeconds)
        {
            return;
        }

        preFrameSeconds += _tick;

        _frame++;
        BroadCastMsg();
    }

    private void UpdatePauseWhenPause()
    {
        if(_pauseTime < _roomTime)
        {
            ResumeGame();
            _socket.SendMessage(_netPeers, new PauseGameMsg(){pause = false, pauseMaxSecond = _MaxManualPauseSec});
        }
    }

    public void ResumeGame()
    {
        _pauseFrame = int.MaxValue;
        _pauseTime = 0;
    }

    private void SetPauseGame(float waitTime)
    {
        _pauseFrame = _frame + 1;

        if(waitTime < 0)
        {
            _pauseTime = float.MaxValue;
        }
        else
        {
            _pauseTime = _roomTime + waitTime;
        }
    }

    public void AddMessage(int peer, NetDataReader reader)
    {
        var msgType = reader.PeekByte();
        if(msgType == (byte)MsgType1.HashMsg)
        {
            FrameHash hash = reader.Get<FrameHash>();
            var errorType = _hashChecker.AddHash(hash);
            if(errorType != FrameCheckErrorType.None)
            {
                if(_gameState == GameState.End) return;

                _socket.SendMessage(_netPeers, new UnSyncMsg()
                {
                    unSyncHashIndex = (ushort)hash.hashIndex,
                    frame = (ushort)hash.frame,
                    errorType = (byte)errorType
                });

                _gameState = GameState.End;
                // _socket.SendMessage(_netPeers, new ServerCloseMsg());
            }

            return;
        }
        else if(msgType == (byte)MsgType1.ReadyForNextStage)
        {
            ReadyStageMsg ready = reader.Get<ReadyStageMsg>();
            var readyStageValue = ready.stageIndex;

            if(readyStageValue <= _stageIndex)
            {
                _socket.SendMessage(_netPeers, new ServerReadyForNextStage(){
                    stageIndex = readyStageValue,
                });
                return;
            }

            var index = Array.FindIndex(_playerInfos, m=>m.id == peer);
            if(index < 0) return;
            
            if(_playerInfos[index].readyStageValue == readyStageValue)   // 已经确认过了
            {
                return;
            }

            _playerInfos[index].readyStageValue = readyStageValue;
            _playerInfos[index].readyStageTime = _roomTime;

            UpdateReadyNextStageRoom();
            
            return;
        }
        else if(msgType == (byte)MsgType1.FinishCurrentStage)
        {
            FinishRoomMsg ready = reader.Get<FinishRoomMsg>();
            var finishedStageValue = ready.stageValue;
            if(finishedStageValue < _stageIndex)    // 断线情况
            {
                _socket.SendMessage(peer, new ServerEnterLoading(){
                    frameIndex = _finishStageFrames[finishedStageValue]
                });
                return;
            }

            var index = Array.FindIndex(_playerInfos, m=>m.id == peer);
            if(index < 0) return;

            if(_playerInfos[index].finishedStageValue == finishedStageValue)   // 已经确认过了
            {
                return;
            }

            _playerInfos[index].finishedStageValue = finishedStageValue;
            _playerInfos[index].finishStageTime = _roomTime;

            UpdateFinishRoom();
            
            return;
        }
        else if(msgType == (byte)MsgType1.ServerReConnect)
        {
            ServerReconnectMsg ready = reader.Get<ServerReconnectMsg>();
            _socket.SendMessage(peer, _frameMsgBuffer.GetReconnectMsg(ready.startFrame, _finishStageFrames));
            return;
        }
        else if(msgType == (byte)MsgType1.PauseGame)
        {
            PauseGameMsg pause = reader.Get<PauseGameMsg>();

            if(pause.pause)
            {
                SetPauseGame(_MaxManualPauseSec);
                pause.pauseMaxSecond = _MaxManualPauseSec;
            }
            else
            {
                ResumeGame();
            }

            _socket.SendMessage(_netPeers, pause);
            return;
        }

        reader.GetByte(); // reader去掉msgType
        _frameMsgBuffer.AddFromReader(reader);
    }

    int GetMaxReadyStageValue()
    {
        int max = -1;
        for(int i = 0; i < _playerInfos.Length; i++)
        {
            max = Math.Max(max, _playerInfos[i].readyStageValue);
        }
        return max;
    }
    private void UpdateReadyNextStageRoom()
    {
        var maxReadyStageValue = GetMaxReadyStageValue();
        if(maxReadyStageValue <= _stageIndex) return;// 都在当前stage

        bool timeout = false;       // 有一个人完成了，倒计时10秒也要进入
        var minReadyStageValue = maxReadyStageValue;
        for(int i = 0; i < _playerInfos.Length; i++)
        {
            if(_playerInfos[i].readyStageValue == maxReadyStageValue)
            {
                var diff = _roomTime - _playerInfos[i].readyStageTime;
                var isOK = diff > _waitReadyStageTime;
                timeout |= isOK;
            }

            var stageValue = _playerInfos[i].isOnLine ? _playerInfos[i].readyStageValue : maxReadyStageValue;
            minReadyStageValue = Math.Min(stageValue, minReadyStageValue);
        }

        var condition = timeout || minReadyStageValue == maxReadyStageValue;
        if(condition)
        {
            _stageIndex = maxReadyStageValue;
            _socket.SendMessage(_netPeers, new ServerReadyForNextStage(){
                stageIndex = _stageIndex,
            });

            ResumeGame();
        }
    }

    int GetMaxFinishedStageValue()
    {
        int max = -1;
        for(int i = 0; i < _playerInfos.Length; i++)
        {
            max = Math.Max(max, _playerInfos[i].finishedStageValue);
        }
        return max;
    }

    private void UpdateFinishRoom()
    {
        if(_stageIndex == 0) return;

        var maxFinishedStageValue = GetMaxFinishedStageValue();
        if(maxFinishedStageValue < _stageIndex) return; // 都在当前stage

        bool timeout = false;       // 有一个人完成了，倒计时10秒也要进入
        bool allFinishOrOffLine = true;
        for(int i = 0; i < _playerInfos.Length; i++)
        {
            var isFinish = _playerInfos[i].finishedStageValue == maxFinishedStageValue;
            if(isFinish)
            {
                var diff = _roomTime - _playerInfos[i].finishStageTime;
                var isOK = diff > _waitFinishStageTime;
                timeout |= isOK;
            }

            allFinishOrOffLine &= (isFinish || !_playerInfos[i].isOnLine);
        }

        var condition = timeout || allFinishOrOffLine;
        if(condition)
        {
            SetPauseGame(-1);

            if(maxFinishedStageValue == 999)
            {
                // End battle
                _gameState = GameState.End;
                _socket.SendMessage(_netPeers, new ServerCloseMsg());
            }
            else
            {
                var stopFrame = _frame + 1;
                _socket.SendMessage(_netPeers, new ServerEnterLoading(){
                    frameIndex = stopFrame,
                    stage = maxFinishedStageValue
                });
                _finishStageFrames[maxFinishedStageValue] = stopFrame;
            }
        }
    }

    private void BroadCastMsg()
    {
        _socket.SendMessage(_netPeers, new ServerPackageItem(){
            frame = (ushort)_frame, clientFrameMsgList = _frameMsgBuffer
        });
    }

    public void StartBattle(RoomStartBattleMsg startMessage)
    {
        _hashChecker = new HashChecker();
        _playerInfos = new PlayerInfo[_netPeers.Count];
        for(int i = 0; i < _playerInfos.Length; i++)
        {
            _playerInfos[i].id = _netPeers[i];
        }

        _gameState = GameState.Running;
        
        _socket.SendMessage(_netPeers, startMessage);
        _startMessage = startMessage;
    }

    public void RemovePeer(int peer)
    {
        _netPeers.Remove(peer);
        var index = Array.FindIndex(_playerInfos, m=>m.id == peer);

        if(index < 0) return;

        _playerInfos[index].isOnLine = false;
    }

    public void Destroy()
    {
    }

    internal void SetOnlineState(int peer, bool isOnLine)
    {
        var index = Array.FindIndex(_playerInfos, m=>m.id == peer);

        if(index < 0) return;

        _playerInfos[index].isOnLine = isOnLine;
    }

    public bool IsBattleEnd => _gameState == GameState.End;
}