using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System;

public interface IClientGameSocket : ILifeCircle
{
    ConnectResult connectResult{get;}
    int RoundTripTime{get;}
    void SendMessage<T>( T t) where T : INetSerializable;
    void SendMessageNotReliable<T>( T t) where T : INetSerializable;
    void SendUnConnectedMessage<T>( T t) where T : INetSerializable;
    Action<NetDataReader> OnReceiveMsg{get;set;}
    Action OnConnected{get;set;}
    Action OnDisConnected{get;set;}
    void Connect();
    void DisConnect();
}

public class GameClientSocket : IClientGameSocket, INetEventListener, INetLogger
{
    private NetManager _netClient;
    private NetDataWriter _dataWriter;

    public int RoundTripTime => _netClient.FirstPeer == null ? -1 : _netClient.FirstPeer.RoundTripTime;

    string _targetIp = null;
    Action<string> _logCallback;
    IPEndPoint _endPoint;

    public void SetIp(string ip, int port)
    {
        if(ip != _targetIp)
        {
            _targetIp = ip;
            _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }
    }

    public GameClientSocket(string targetIp, int port, int delay, Action<string> logCallback)
    {
        _logCallback = logCallback;
        NetDebug.Logger = this;
        
        if(string.IsNullOrEmpty(targetIp))
        {
            throw new Exception("targetip 为空");
        }

        SetIp(targetIp, port);

        Debug.LogError(targetIp + " " + port);
        _netClient = new NetManager(this, new LiteNetLib.Layers.Crc32cLayer());
        _dataWriter = new NetDataWriter();
        _netClient.UnconnectedMessagesEnabled = true;
        _netClient.AllowPeerAddressChange = true;  // 玩家切网络自动回连
        _netClient.AutoRecycle = true;
        _netClient.UpdateTime = 15;
        _netClient.Start();
    }

#region ILifeCircle
    public void Start()
    {
    }

    public void Connect()
    {
        if(connectResult == ConnectResult.Connecting || connectResult == ConnectResult.Connnected)
        {
            return;
        }

        connectResult = ConnectResult.Connecting;
        _dataWriter.Reset();
        _dataWriter.Put("wsa_game");
        _dataWriter.Put(RoomMsgVersion.version);
        _netClient.Connect(_endPoint, _dataWriter);

        _logCallback("connect " + _endPoint);
    }

    public void DisConnect()
    {
        _netClient.DisconnectAll();
    }

    public void Update(float x)
    {
        _netClient.PollEvents();
    }

    public void OnDestroy()
    {
        if (_netClient != null)
        {
            _netClient.Stop();
            _netClient = null;
        }
    }
#endregion

#region IMessageSendReceive
    public Action<NetDataReader> OnReceiveMsg{get;set;}

    public ConnectResult connectResult{get; private set;} = ConnectResult.NotConnect;
    public Action OnConnected { get;  set; }
    public Action OnDisConnected { get;  set; }

    public void SendMessage<T>(T t) where T : INetSerializable
    {
        // Debug.LogError("===>>>>>>> " + typeof(T));
        UnityEngine.Profiling.Profiler.BeginSample("NETBATTLE_GameClientSocket.SendMessage");

        var peer = _netClient.FirstPeer;
        if (peer != null && peer.ConnectionState == ConnectionState.Connected)
        {
            _dataWriter.Reset();
            _dataWriter.Put(t);
            peer.Send(_dataWriter, DeliveryMethod.ReliableOrdered);
        }

        UnityEngine.Profiling.Profiler.EndSample();
    }

    public void SendMessageNotReliable<T>(T t) where T : INetSerializable
    {
        UnityEngine.Profiling.Profiler.BeginSample("NETBATTLE_GameClientSocket.SendMessageNotReliable");
        var peer = _netClient.FirstPeer;
        if (peer != null && peer.ConnectionState == ConnectionState.Connected)
        {
            _dataWriter.Reset();
            _dataWriter.Put(t);
            peer.Send(_dataWriter, DeliveryMethod.Unreliable);
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }

    public void SendUnConnectedMessage<T>(T t) where T : INetSerializable
    {
        _dataWriter.Reset();
        _dataWriter.Put(t);
        _netClient.SendUnconnectedMessage(_dataWriter, _endPoint);
    }
#endregion

#region INetEventListener
    public void OnPeerConnected(NetPeer peer)
    {
        // Debug.LogError("[CLIENT] We connected to " + peer.EndPoint);
        connectResult = ConnectResult.Connnected;

        OnConnected?.Invoke();
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        Debug.LogError("[CLIENT] We received error " + socketErrorCode);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        OnReceiveMsg(reader);
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if(!_endPoint.Address.Equals(remoteEndPoint.Address) || _endPoint.Port != remoteEndPoint.Port) return;

        OnReceiveMsg(reader);
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        Debug.LogError("OnConnectionRequest 不应该走到");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logCallback("[CLIENT] We disconnected because " + disconnectInfo.Reason);
        connectResult = ConnectResult.Disconnect;
        OnDisConnected?.Invoke();
    }
    #endregion

    public void WriteNet(NetLogLevel level, string str, params object[] args)
    {
            UnityEngine.Debug.LogError($"{str} {string.Join(",", args)}");

        if(level == NetLogLevel.Error)
        {
            #if UNITY_EDITOR
            UnityEngine.Debug.LogError($"{str} {string.Join(",", args)}");
            #else
            Console.WriteLine($"{str} {string.Join(",", args)}");
            #endif
        }
        else
        {
            // ignore
        }
    }
}
