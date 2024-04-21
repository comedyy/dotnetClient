using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameLogicGUI : MonoBehaviour
{
    public string UserName = "defaultName";
    public int pen = 1;

    LocalFrameNetGame _netGame;
    int _currentValue = 0;
    public static List<MessageItem> _lstTemp = new List<MessageItem>();
    int _gameFrame = 0;
    int[] _penValue;

    void Start()
    {
        ClientBattleRoomMgr.Instance().OnBattleStart += OnBattleStart;
    }

    private void OnBattleStart(BattleStartMessage message, IClientGameSocket socket)
    {
        _netGame = new LocalFrameNetGame(0.5f, socket, 0, message, false);
        _currentValue = message.initNum;
        _penValue = message.joins.Select(m=>m.pen).ToArray();

        _netGame.SendReady(1);
    }

    internal void Init(RoomGUI roomGUI, int v)
    {
        roomGUI.GetJoinMessage = GetJoinMessage;
        roomGUI.GetStartMessage = GetStartMessage;
    }

    private (BattleStartMessage, BattleStartShowInfo) GetStartMessage()
    {
        return (new BattleStartMessage(){ initNum = 1}, new BattleStartShowInfo(){});
    }

    private (JoinMessage, JoinMessageShowInfo) GetJoinMessage()
    {
        var join = new JoinMessage(){ pen = pen, name = UserName};
        return (join, join.GetInfo());
    }

    void OnGUI()
    {
        if(ClientBattleRoomMgr.Instance()._roomState == TeamRoomState.InSearchRoom)
        {
            UserName = GUI.TextField(new Rect(0, 0, 100, 100), UserName);
            pen = int.Parse(GUI.TextField(new Rect(100, 0, 100, 100), pen.ToString()));
        }
        else if(ClientBattleRoomMgr.Instance()._roomState == TeamRoomState.InBattle)
        {
            if(GUI.Button(new Rect(0, 0, 100, 100), "add"))
            {
                _netGame.SendOpt(0);
            }
            if(GUI.Button(new Rect(100, 0, 100, 100), "remove"))
            {
                _netGame.SendOpt(1);
            }

            GUI.Label(new Rect(200, 0, 100, 100), _currentValue.ToString());
            
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
