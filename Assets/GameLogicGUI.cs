using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameLogicGUI : MonoBehaviour
{
    int _userId;
    string UserName = "defaultName";
    int pen = 1;

    LocalFrameNetGame _netGame;
    int _currentValue = 0;
    public static List<MessageItem> _lstTemp = new List<MessageItem>();
    int _gameFrame = 0;
    int[] _penValue;
    ClientBattleRoomMgr _clientBattleRoomMgr;
    int GetXOffset => _userId == 1 ? 0 : 600;

    void Start()
    {
    }

    private void OnBattleStart(BattleStartMessage message, IClientGameSocket socket)
    {
        var index = Array.FindIndex(message.joins, m=>m.userId == _userId);
        _netGame = new LocalFrameNetGame(0.5f, socket, index, message, false);
        _currentValue = message.initNum;
        _penValue = message.joins.Select(m=>m.pen).ToArray();

        _netGame.SendReady(1);
    }

    internal void Init(RoomGUI roomGUI, int userId, ClientBattleRoomMgr clientBattleRoomMgr)
    {
        _userId = userId;
        _clientBattleRoomMgr = clientBattleRoomMgr;
        clientBattleRoomMgr.OnBattleStart += OnBattleStart;
        roomGUI.GetJoinMessage = GetJoinMessage;
        roomGUI.GetStartMessage = GetStartMessage;
    }

    private (BattleStartMessage, BattleStartShowInfo) GetStartMessage()
    {
        return (new BattleStartMessage(){ initNum = 1}, new BattleStartShowInfo(){});
    }

    private (JoinMessage, JoinMessageShowInfo) GetJoinMessage()
    {
        var join = new JoinMessage(){ pen = pen, name = UserName, userId = _userId};
        return (join, join.GetInfo());
    }

    void OnGUI()
    {
        if(_clientBattleRoomMgr._roomState == TeamRoomState.InSearchRoom)
        {
            UserName = GUI.TextField(new Rect(GetXOffset+ 0, 0, 100, 100), UserName);
            pen = int.Parse(GUI.TextField(new Rect(GetXOffset+ 100, 0, 100, 100), pen.ToString()));
        }
        else if(_clientBattleRoomMgr._roomState == TeamRoomState.InBattle)
        {
            if(GUI.Button(new Rect(GetXOffset+ 0, 0, 100, 100), "add"))
            {
                _netGame.SendOpt(0);
            }
            if(GUI.Button(new Rect(GetXOffset+ 100, 0, 100, 100), "remove"))
            {
                _netGame.SendOpt(1);
            }

            GUI.Label(new Rect(GetXOffset+ 200, 0, 100, 100), _currentValue.ToString());
            
            while(_gameFrame < _netGame.ReceivedServerFrame)
            {
                _gameFrame++;
                _lstTemp.Clear();
                _netGame.GetFrameInput(_gameFrame, _lstTemp);

                Process(_lstTemp);
            }
        }
    }

    private void Process(List<MessageItem> lstTemp)
    {
        foreach(var x in lstTemp)
        {
            Debug.LogError(x.id);
            if(x.opt == 0)
            {
                _currentValue += _penValue[x.id];
            }
            else
            {
                _currentValue -= _penValue[x.id];
            }
        }
    }

    void Update()
    {
        _netGame?.Update();
    }
}
