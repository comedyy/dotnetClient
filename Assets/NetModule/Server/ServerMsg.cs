using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;

public enum MsgType1 : byte
{
    FrameMsg = 2,
    ServerFrameMsg = 3,
    HashMsg = 4,
    ReadyForNextStage = 5,
    ServerReadyForNextStage = 6,
    PauseGame = 7,
    FinishCurrentStage = 9,         // 完成当前的stage小关
    ServerEnterLoading = 10,  // 完成当前的stage小关, 服务器回包
    Unsync = 11,
    ServerReConnect = 12,
    ServerMsgEnd___ = 100, // 服务器消息最后

    CreateRoom = 101,
    JoinRoom = 102,
    StartRequest = 103,
    SyncRoomMemberList = 104,
    GetAllRoomList = 105,   // 获得所有的房间列表，无连接
    SetSpeed = 107,
    RoomStartBattle = 108,
    ServerClose = 109,
    ErrorCode = 110,
    SetUserId = 111,
    KickUser = 112,
    LeaveUser = 113,
    RoomReady = 114,
    GetUserState = 115,    // 查询玩家状态。无连接
    RoomEventSync = 116, // 房间的事件通知
    RoomChangeUserPos = 117,
    GetRoomState = 119,   // 获取房间状态，无连接
    GetRoomStateResponse = 119,   // 获取房间状态，无连接
    // GetUserJoinInfo = 120, 
    // GetUserInfoResponse = 120, 
    UserReloadServerOK = 121, // 客户端恢复加载了。
    UpdateMemberInfo = 122,
    CreateAutoJoinRobert = 123,     // 创建机器人，加入队伍
    CreateAutoCreateRoomRobert = 124,     // 创建机器人去创建房间
    GetUniqueIdInServer = 125, // 获取服务器的唯一id。
    BroadCastMsg = 126,
    ChangeRoomInfo = 127,
}


[Serializable]
public struct FrameHash : INetSerializable
{
    public int frame;
    public int id;
    public int hash;
    internal int hashIndex;

    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.HashMsg);
        writer.Put(frame);
        writer.Put(id);
        writer.Put(hash);
        writer.Put(hashIndex);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        frame = reader.GetInt();
        id = reader.GetInt();
        hash = reader.GetInt();
        hashIndex = reader.GetInt();
    }
}


public struct FinishRoomMsg : INetSerializable
{
    public int stageValue;

    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.FinishCurrentStage);
        writer.Put(stageValue);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        stageValue = reader.GetInt();
    }
}

public struct ReadyStageMsg : INetSerializable
{
    public int stageIndex;

    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.ReadyForNextStage);
        writer.Put(stageIndex);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        stageIndex = reader.GetInt();
    }
}

public enum PauseGameReason
{
    None,
    UserDead,
    UserManualPause,
    AIOpt,
}

public struct PauseGameMsg : INetSerializable
{
    public bool pause;
    public PauseGameReason pauseGameReason;
    public int pauseMaxSecond;

    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.PauseGame);
        writer.Put(pause);
        writer.Put((byte)pauseGameReason);
        writer.Put(pauseMaxSecond);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        pause = reader.GetBool();
        pauseGameReason = (PauseGameReason)reader.GetByte();
        pauseMaxSecond = reader.GetInt();
    }
}

public struct ServerReadyForNextStage : INetSerializable
{
    public int stageIndex;

    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.ServerReadyForNextStage);
        writer.Put(stageIndex);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        stageIndex = reader.GetInt();
    }
}


public struct ServerEnterLoading : INetSerializable
{
    public int frameIndex;
    public int stage;

    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.ServerEnterLoading);
        writer.Put(frameIndex);
        writer.Put(stage);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        frameIndex = reader.GetInt();
        stage = reader.GetInt();
    }
}


public partial struct ServerPackageItem : INetSerializable
{
    public ushort frame;
 
    // server write 
    public FrameMsgBuffer clientFrameMsgList;

    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.ServerFrameMsg);
        writer.Put(frame);
        var count = clientFrameMsgList.Count;
        writer.Put((byte)count);
        clientFrameMsgList.WriterToWriter(writer, frame);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        frame = reader.GetUShort();
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
       OnDeserialize(reader);
#endif
    }
}


public struct ServerCloseMsg : INetSerializable
{
    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.ServerClose);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
    }
}

public struct ServerReconnectMsg : INetSerializable
{
    public int startFrame;
    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.ServerReConnect);
        writer.Put(startFrame);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        startFrame = reader.GetInt();
    }
}


public struct ServerReconnectMsgResponse : INetSerializable
{
    public int startFrame;
    public List<byte[]> bytes;
    public IntPair2[] stageFinishedFrames;
    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.ServerReConnect);
        writer.Put(startFrame);
        writer.Put(bytes.Count);
        for(int i = 0; i < bytes.Count; i++)
        {
            writer.PutBytesWithLength(bytes[i]);
        }

        IntPair2.SerializeArray(writer, stageFinishedFrames);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        startFrame = reader.GetInt();

        var size = reader.GetInt();
        bytes = new List<byte[]>();
        for(int i = 0; i < size; i++)
        {
            bytes.Add(reader.GetBytesWithLength());
        }

        stageFinishedFrames = IntPair2.DeserializeArray(reader);
    }
}

public struct IntPair2 : INetSerializable
{
    public int Item1;
    public int Item2;

    public void Deserialize(NetDataReader reader)
    {
        Item1 = reader.GetInt();
        Item2 = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Item1);
        writer.Put(Item2);
    }

    public static void SerializeArray(NetDataWriter writer, IntPair2[] pairs)
    {
        ushort count = pairs == null ? (ushort)0 : (ushort)pairs.Length;
        writer.Put(count);
        for(int i = 0; i < count; i++)
        {
            writer.Put(pairs[i]);
        }
    }

    public static IntPair2[] DeserializeArray(NetDataReader reader)
    {
        ushort count = reader.GetUShort();
        IntPair2[] intPair2s = new IntPair2[count];
        for(int i = 0; i < count; i++)
        {
            intPair2s[i] = reader.Get<IntPair2>();
        }

        return intPair2s;
    }
}


public struct GetServerUniqueIdMsg : INetSerializable
{
    public int id;
    public byte count;
    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.GetUniqueIdInServer);
        writer.Put(id);
        writer.Put(count);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        id = reader.GetInt();
        count = reader.GetByte();
    }
}


public struct UnSyncMsg : INetSerializable
{
    public ushort unSyncHashIndex;
    public ushort frame;
    public byte errorType;

    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();

        unSyncHashIndex = reader.GetUShort();
        frame = reader.GetUShort();
        errorType = reader.GetByte();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.Unsync);

        writer.Put(unSyncHashIndex);
        writer.Put(frame);
        writer.Put(errorType);
    }
}
