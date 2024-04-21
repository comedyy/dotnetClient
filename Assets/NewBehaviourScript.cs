using System;
using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    public Text _textCurrent;
    static NewBehaviourScript _instance;
    int _currentNum = 0;
    int selfAdd = 0;
    int _selfId = -1;
    GameClientSocket _socket;

    // GameClientSocket _socket = new GameClientSocket("101.132.100.216", 0);
    // Start is called before the first frame update
    void Start()
    {
        _instance = this;
        ClientBattleRoomMgr.Instance().enableLog = true;

        // _socket.Start();

    }

    private void OnGUI() {
        if(ClientBattleRoomMgr.Instance()._roomState == TeamRoomState.InSearchRoom)
        {
            if(GUI.Button(new Rect(0, 0, 100, 100), "create"))
            {
                _selfId = 1;
                ClientBattleRoomMgr.Instance().SetUserId(_selfId);
                BattleStartMessage _start = new BattleStartMessage(){ initNum = 1};
                BattleStartShowInfo roomShowInfo = new BattleStartShowInfo(){};
                JoinMessage join = new JoinMessage(){ pen = 2, name = "kkk"};
                JoinMessageShowInfo clientUserJoinShowInfo = new JoinMessageShowInfo(){ name = "kkk", pen = 2};
                ClientBattleRoomMgr.Instance().CreateRoom(NetUtils.GetBytes(_start), NetUtils.GetBytes(roomShowInfo), NetUtils.GetBytes(join), NetUtils.GetBytes(clientUserJoinShowInfo));
            }
        }
        else if(ClientBattleRoomMgr.Instance()._roomState == TeamRoomState.InBattle)
        {
            if(GUI.Button(new Rect(0, 0, 100, 100), "add"))
            {
                _socket.SendMessage(new PackageItem(){
                    messageItem = new MessageItem(){ id = _selfId, opt = 0}
                });
            }
            if(GUI.Button(new Rect(0, 0, 100, 100), "remove"))
            {
                _socket.SendMessage(new PackageItem(){
                    messageItem = new MessageItem(){ id = _selfId, opt = 1}
                });
            }
        }
        else
        {
            if(GUI.Button(new Rect(0, 0, 100, 100), "start"))
            {
                ClientBattleRoomMgr.Instance().StartRoom();
            }
        }

        // if(GUI.Button(new Rect(100, 0, 100, 100), "join"))
        // {
        //     ClientBattleRoomMgr.Instance().SetUserId(2);
        //     JoinMessage join = new JoinMessage(){ pen = 1, name = "kkk"};
        //     JoinMessageShowInfo clientUserJoinShowInfo = new JoinMessageShowInfo(){ name = "kkk", pen = 1};
        //     ClientBattleRoomMgr.Instance().JoinRoom(NetUtils.GetBytes(join), NetUtils.GetBytes(clientUserJoinShowInfo));
        // }
    }

    public static void OnBattleStart(BattleStartMessage startMessage, GameClientSocket socket)
    {
        _instance?.StartBattle(startMessage, socket);
    }

    private void StartBattle(BattleStartMessage startMessage, GameClientSocket socket)
    {
        _currentNum = startMessage.initNum;
        _textCurrent.text = startMessage.initNum.ToString();

        var index = Array.FindIndex(ClientBattleRoomMgr.Instance()._updateRoomInfo.userList, m=>m.userId == _selfId);

        selfAdd = startMessage.joins[index].pen;
        socket.OnReceiveMsg += OnReive;
        _socket = socket;
    }

    private void OnReive(NetDataReader reader)
    {
        var x = (MsgType1)reader.PeekByte();

        if(x == MsgType1.ServerFrameMsg)
        {
            var xx = reader.Get<ServerPackageItem>();
            foreach(var opt in xx.list)
            {
                if(opt.opt == 0)
                {
                    _currentNum += selfAdd;
                }
                else
                {
                    _currentNum -= selfAdd;
                }
            }

            _textCurrent.text = _currentNum.ToString();
        }
    }

    void OnDestroy()
    {
        // _socket.OnDestroy();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
