
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using LiteNetLib.Utils;
using UnityEngine;


public abstract class LocalFrame
{
    public float preFrameSeconds;
    public float totalTime;
    protected float _tick;
    public int ReceivedServerFrame;

    public int _clientStageIndex;
    public int _serverStageIndex;

    protected MessageItem? _messageItem;
    protected int _controllerId = 0;
    public int ControllerId => _controllerId;
    public static LocalFrame Instance;
    public bool IsPaused {get; protected set;}
    public bool BattleEnd{get;private set;}
    public bool Win{get;private set;}

    public LocalFrame(){}

    public LocalFrame(float tick, int id)
    {
        Instance = this;
        _tick = tick;
        _controllerId = id;
    }

    public virtual void Update()
    {
    }

    protected Dictionary<int , List<MessageItem>> _allMessage = new Dictionary<int, List<MessageItem>>();

    public void GetFrameInput(int frame, List<MessageItem> listOut)
    {
        if(_allMessage.TryGetValue(frame, out var list))
        {
            listOut.AddRange(list);
            _allMessage.Remove(frame);
        }
    }

    public virtual bool IsClientBattle => true;
    public virtual bool IsNetBattle => false;
    public virtual bool IsPlayback => false;
    public virtual bool IsSyncTest => false;
    
    public abstract bool IsClientNormalBattle{get;}
    public bool CanControl => !(IsPlayback);
    public virtual bool IsInPreloadState => false;
    public virtual bool CanEnterGame => true;

    public int GameFrame { get; internal set; }

    public void Destroy()
    {
        Instance = null;
        OnBattleDestroy();
    }
    internal virtual void OnBattleDestroy(){}

    #region sendMsg
    
    internal virtual void SendExit()
    {
        MakeMessageHead();

        var x = _messageItem.Value;
        // x.messageBit |= MessageBit.ExitGame;
        _messageItem = x; 
    }

    
    internal void SendOpt(int v)
    {
        MakeMessageHead();

        var x = _messageItem.Value;
        x.messageBit |= MessageBit.opt;
        x.id = ControllerId;
        x.opt = v;
        _messageItem = x; 
    }

    internal virtual void SendBattleEnd(bool win)
    {
        BattleEnd = true;
        Win = win;
        SendRoomEnd();
    }
    
    internal virtual void SendRoomEnd()
    {
    }

    internal virtual void SendReady(int stage)
    {
    }
    
    public virtual void SetPauseGame(bool pause)
    {
    }
    
    // public virtual void SendHash(int frame, CheckSumMgr checkSumMgr, List<MessageItem> frameInput)
    // {
    // }
    #endregion

    #region 
    private void MakeMessageHead()
    {
        if(_messageItem == null)
        {
            _messageItem = new MessageItem()
            {
                id = _controllerId,
            };
        }
    }

    // public virtual void SaveRecord(int frame, CheckSumMgr checksumMgr, List<MessageItem> _lstTemp)
    // {
    // }

    public bool NeedCalHash(int frameCount)
    {
        #if DEBUG_1 || DEBUG_2
        return true;
        #else
        return frameCount % 100 == 0;
        #endif
    }
    #endregion

    int _totalRoundTripTime = 0;
    public int SocketRoundTripLogicTime => _totalRoundTripTime;

    public bool IsAutoPopupSkillWinSetting { get; set; }
    public bool DisablePresentation {get;set;} = false;
    public virtual bool IsPendingSkillChooseWhenRoundEnd{get; protected set;} = true;
    public int Speed { get; internal set; } = 16;
    public virtual int LastCanExecuteFrame => ReceivedServerFrame;
}