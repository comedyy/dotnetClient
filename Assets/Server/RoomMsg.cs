using System.Collections.Generic;
using LiteNetLib.Utils;

public class RoomMsgVersion
{
    #if !UNITY_EDITOR && UNITY_ANDROID
    public const int version = 0;
    #else
    public const int version = 7;
    #endif
}

public enum TeamConnectParam
{
    None,
    SyncInfo,
}

public enum RoomMasterLeaveOpt
{
    RemoveRoomAndBattle,
    ChangeRoomMater,
}

public enum WhoCanLeaveRoomInBattle
{
    All,
    OnlyMaster
}

public struct ServerSetting : INetSerializable
{
    public float tick;
    public ushort maxSec;
    public RoomMasterLeaveOpt masterLeaveOpt;
    public WhoCanLeaveRoomInBattle whoCanLeaveRoomInBattle;
    public byte maxCount;
    internal byte waitReadyStageTimeMs;
    internal byte waitFinishStageTimeMs;
    public bool keepRoomAfterBattle;
    public int pauseMaxSecond;


    public void Deserialize(NetDataReader reader)
    {
        tick = reader.GetFloat();
        maxSec = reader.GetUShort();
        masterLeaveOpt = (RoomMasterLeaveOpt)reader.GetByte();
        maxCount = reader.GetByte();

        waitReadyStageTimeMs = reader.GetByte();
        waitFinishStageTimeMs = reader.GetByte();
        keepRoomAfterBattle = reader.GetBool();
        pauseMaxSecond = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(tick);
        writer.Put(maxSec);
        writer.Put((byte)masterLeaveOpt);
        writer.Put(maxCount);
        writer.Put(waitReadyStageTimeMs);
        writer.Put(waitFinishStageTimeMs);
        writer.Put(keepRoomAfterBattle);
        writer.Put(pauseMaxSecond);
    }
}


public struct CreateRoomMsg : INetSerializable
{
    public byte[] startBattleMsg;
    public byte[] join;
    public ServerSetting setting;
    public byte[] joinShowInfo;
    public byte[] roomShowInfo;


    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte(); // msgHeader

        startBattleMsg = reader.GetBytesWithLength();
        join = reader.GetBytesWithLength();
        
        setting = reader.Get<ServerSetting>();

        joinShowInfo = reader.GetBytesWithLength();
        roomShowInfo = reader.GetBytesWithLength();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.CreateRoom);
        writer.PutBytesWithLength(startBattleMsg);
        writer.PutBytesWithLength(join);

        writer.Put(setting);

        writer.PutBytesWithLength(joinShowInfo);
        writer.PutBytesWithLength(roomShowInfo);
    }
}


public struct JoinRoomMsg : INetSerializable
{
    public int roomId;
    public byte[] joinMessage;
    public byte[] joinShowInfo;

    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        roomId = reader.GetInt();
        joinMessage = reader.GetBytesWithLength();
        joinShowInfo = reader.GetBytesWithLength();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.JoinRoom);
        writer.Put(roomId);
        writer.PutBytesWithLength(joinMessage);
        writer.PutBytesWithLength(joinShowInfo);
    }
}


public struct UpdateMemberInfoMsg : INetSerializable
{
    public byte[] joinMessage;
    public byte[] joinShowInfo;

    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        joinMessage = reader.GetBytesWithLength();
        joinShowInfo = reader.GetBytesWithLength();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.UpdateMemberInfo);
        writer.PutBytesWithLength(joinMessage);
        writer.PutBytesWithLength(joinShowInfo);
    }
}

public struct StartBattleRequest : INetSerializable
{
    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.StartRequest);
    }
}

public struct RoomStartBattleMsg : INetSerializable
{
    public byte[] StartMsg;
    public List<byte[]> joinMessages;
    public bool isReconnect;
    public byte[] roomShowInfo;

    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();

        StartMsg = reader.GetBytesWithLength();
        var MemberCount = reader.GetByte();
        joinMessages = new List<byte[]>();
        for(int i = 0; i < MemberCount; i++)
        {
            joinMessages.Add(reader.GetBytesWithLength());
        }
        isReconnect = reader.GetBool();
        roomShowInfo = reader.GetBytesWithLength();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.RoomStartBattle);

        writer.PutBytesWithLength(StartMsg);
        writer.Put((byte)joinMessages.Count);
        for(int i = 0; i < joinMessages.Count; i++)
        {
            writer.PutBytesWithLength(joinMessages[i]);
        }
        writer.Put(isReconnect);
        writer.PutBytesWithLength(roomShowInfo);
    }
}

public partial struct RoomUser : INetSerializable
{
    public byte[] userInfo;
    public bool isOnLine;
    public bool isReady;
    public uint userId;
    public bool needAiHelp;
    public bool isRobert;
    
    public void Deserialize(NetDataReader reader)
    {
        isOnLine = reader.GetBool();
        isReady = reader.GetBool();
        userId = reader.GetUInt();
        userInfo = reader.GetBytesWithLength();
        needAiHelp = reader.GetBool();
        isRobert = reader.GetBool();

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
        OnDeserialize(reader);
#endif
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(isOnLine);
        writer.Put(isReady);
        writer.Put(userId);
        writer.PutBytesWithLength(userInfo);
        writer.Put(needAiHelp);
        writer.Put(isRobert);
    }
}

public partial struct UpdateRoomMemberList : INetSerializable
{
    public RoomUser[] userList;
    public int roomId;
    public byte[] roomShowInfo;
    public byte AIHelperIndex;

    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        roomId = reader.GetInt();
        var count = reader.GetInt();
        userList = new RoomUser[count];
        for(int i = 0; i < count; i++)
        {
            userList[i] = reader.Get<RoomUser>();
        }

        roomShowInfo = reader.GetBytesWithLength();
        AIHelperIndex = reader.GetByte();

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
        OnDeserialize(reader);
#endif
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.SyncRoomMemberList);

        writer.Put(roomId);
        var size = userList == null ? 0 : userList.Length;
        writer.Put(size);
        for(int i = 0; i < size; i++)
        {
            writer.Put(userList[i]);
        }

        writer.PutBytesWithLength(roomShowInfo);
        writer.Put(AIHelperIndex);
    }
}


public struct RoomInfoMsg : INetSerializable
{
    public UpdateRoomMemberList updateRoomMemberList;

    public void Deserialize(NetDataReader reader)
    {
        updateRoomMemberList = reader.Get<UpdateRoomMemberList>();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(updateRoomMemberList);
    }
}


public struct RoomListMsgRequest : INetSerializable
{
    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.GetAllRoomList);
    }
}


public struct RoomListMsg : INetSerializable
{
    public RoomInfoMsg[] roomList;
    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        var count = reader.GetInt();
        roomList = new RoomInfoMsg[count];
        for(int i = 0; i < count; i++)
        {
            roomList[i] = reader.Get<RoomInfoMsg>();
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.GetAllRoomList);

        writer.Put(roomList.Length);
        for(int i = 0; i < roomList.Length; i++)
        {
            writer.Put(roomList[i]);
        }
    }
}

public struct SetServerSpeedMsg : INetSerializable
{
    public int speed;
    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        speed = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.SetSpeed);
        writer.Put(speed);
    }
}

public enum RoomError : byte
{
    RoomFull = 1,
    JoinRoomErrorHasRoom = 2,
    CreateRoomErrorHasRoom = 3,
    BattleNotExit = 4,
    AuthError = 5,
    ChangeErrorOutOfIndex = 6,
    RoomNotExist = 7,
    EnterRoomButBattleExist = 8,
    LeaveErrorInBattle = 9,
    JoinRoomErrorInsideRoom = 10,
    UpdatFailedMemberNotExist = 11,
}

public struct RoomErrorCode : INetSerializable
{
    public RoomError roomError;

    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        roomError = (RoomError)reader.GetByte();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.ErrorCode);
        writer.Put((byte)roomError);
    }
}

public struct RoomUserIdMsg : INetSerializable
{
    public int userId;
    public TeamConnectParam connectParam;

    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        userId = reader.GetInt();
        connectParam = (TeamConnectParam)reader.GetByte();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.SetUserId);
        writer.Put(userId);
        writer.Put((byte)connectParam);
    }
}

public struct KickUserMsg : INetSerializable
{
    public int userId;

    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        userId = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.KickUser);
        writer.Put(userId);
    }
}

public struct UserLeaveRoomMsg : INetSerializable
{

    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.LeaveUser);
    }
}


public struct RoomReadyMsg : INetSerializable
{
    public bool isReady;
    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        isReady = reader.GetBool();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.RoomReady);
        writer.Put(isReady);
    }
}

public struct GetUserStateMsg : INetSerializable
{
    public enum UserState
    {
        None, HasRoom, HasBattle, Querying,
    }

    public UserState state;
    public int userId;
    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        state = (UserState)reader.GetByte();
        userId = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.GetUserState);
        writer.Put((byte)state);
        writer.Put(userId);
    }
}

public struct SyncRoomOptMsg : INetSerializable
{
    public enum RoomOpt
    {
        None, Kick, Leave, MasterLeaveRoomEnd, RoomEnd, Join
    }

    public RoomOpt state;
    public int param;
    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        state = (RoomOpt)reader.GetByte();
        param = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.RoomEventSync);
        writer.Put((byte)state);
        writer.Put(param);
    }
}


public struct RoomChangeUserPosMsg : INetSerializable
{
    public byte fromIndex;
    public byte toIndex;
    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        fromIndex = reader.GetByte();
        toIndex = reader.GetByte();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.RoomChangeUserPos);
        writer.Put(fromIndex);
        writer.Put(toIndex);
    }
}

public struct GetRoomStateMsg : INetSerializable
{
    public int idRoom;
    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.GetRoomState);
        writer.Put(idRoom);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        idRoom = reader.GetInt();
    }
}

public struct GetRoomStateResponse : INetSerializable
{
    public RoomInfoMsg infoMsg;
    public int roomId;
    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.GetRoomStateResponse);
        writer.Put(roomId);
        writer.Put(infoMsg);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        roomId = reader.GetInt();
        infoMsg = reader.Get<RoomInfoMsg>();
    }
}

public struct UserReloadServerOKMsg : INetSerializable
{
    void INetSerializable.Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.UserReloadServerOK);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
    }
}

public struct CreateAutoJoinRobertMsg : INetSerializable
{
    public JoinRoomMsg joinRoomMsg;
    internal int idRobert;
    internal int readyDelay;

    public void Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        joinRoomMsg = reader.Get<JoinRoomMsg>();
        idRobert = reader.GetInt();
        readyDelay = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.CreateAutoJoinRobert);
        writer.Put(joinRoomMsg);
        writer.Put(idRobert);
        writer.Put(readyDelay);
    }
}

public struct CreateAutoCreateRoomRobertMsg : INetSerializable
{
    public CreateRoomMsg createRoomMsg;
    internal int idRobert;
    public byte[] joinUser;
    public byte[] joinShowInfoUser;
    internal int delayStart;

    public void Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        createRoomMsg = reader.Get<CreateRoomMsg>();
        idRobert = reader.GetInt();
        
        joinUser = reader.GetBytesWithLength();
        joinShowInfoUser = reader.GetBytesWithLength();
        delayStart = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.CreateAutoCreateRoomRobert);
        writer.Put(createRoomMsg);
        writer.Put(idRobert);

        writer.PutBytesWithLength(joinUser);
        writer.PutBytesWithLength(joinShowInfoUser);
        writer.Put(delayStart);
    }
}


public enum BroadCoastType : byte
{
    Chat = 0,
    LoadingProgress = 1,
    UnsyncInfo = 2,
}

public partial struct BroadCastMsg : INetSerializable
{
    public BroadCoastType broadCoastType;
    public byte[] data;

    public void Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        broadCoastType = (BroadCoastType)reader.GetByte();
        data = reader.GetBytesWithLength();
        
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
        OnDeserialize(reader);
#endif
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.BroadCastMsg);
        writer.Put((byte)broadCoastType);
        writer.PutBytesWithLength(data);
    }
}

public struct ChangeRoomInfoMsg : INetSerializable
{
    public byte[] bytes;
    public bool needCancelReady;
    public void Deserialize(NetDataReader reader)
    {
        var msgType = reader.GetByte();
        needCancelReady = reader.GetBool();
        bytes = reader.GetBytesWithLength();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.ChangeRoomInfo);
        writer.Put(needCancelReady);
        writer.PutBytesWithLength(bytes);
    }
}
