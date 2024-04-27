using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;

public enum MessageBit : ushort
{
    opt = 0
}

public struct BattleStartMessage : INetSerializable
{
    public string guid;
    public int initNum;
    public JoinMessage[] joins;

    public void Deserialize(NetDataReader reader)
    {
        guid = reader.GetString();
        initNum = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(guid);
        writer.Put(initNum);
    }
}

public struct BattleStartShowInfo : INetSerializable
{
    public void Deserialize(NetDataReader reader)
    {
    }

    public void Serialize(NetDataWriter writer)
    {
    }
}

public struct JoinMessageShowInfo : INetSerializable
{
    public string name;
    public int pen;
    public void Deserialize(NetDataReader reader)
    {
        pen = reader.GetInt();
        name = reader.GetString();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(pen);
        writer.Put(name);
    }
}

public struct JoinMessage : INetSerializable
{
    public string name;
    public int pen;
    public int userId;

    public void Deserialize(NetDataReader reader)
    {
        pen = reader.GetInt();
        name = reader.GetString();
        userId = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(pen);
        writer.Put(name);
        writer.Put(userId);
    }

    public JoinMessageShowInfo GetInfo()
    {
        return new JoinMessageShowInfo()
        {
            pen = pen,
            name = name
        };
    }
}

public struct PackageItem : INetSerializable
{
    public MessageItem messageItem;
    public void Deserialize(NetDataReader reader)
    {
        reader.GetByte();
        messageItem = reader.Get<MessageItem>();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)MsgType1.FrameMsg);
        writer.Put(messageItem);
    }
}

public struct MessageItem : INetSerializable
{
    public MessageBit messageBit;
    public int id;
    public int opt;

    public void Deserialize(NetDataReader reader)
    {
        messageBit = (MessageBit)reader.GetUShort();
        id = reader.GetInt();
        opt = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((ushort)messageBit);
        writer.Put(id);
        writer.Put(opt);
    }
}

public partial struct ServerPackageItem
{
    public List<MessageItem> list;
    public void OnDeserialize(NetDataReader reader)
    {
        var count = reader.GetByte();
        list = new List<MessageItem>();
        for(int i = 0; i < count; i++)
        {
            list.Add(reader.Get<MessageItem>());
        }
    }
}

public partial struct RoomUser
{
    RoomUserClientInfo roomUserClientInfo;
    public int pen => roomUserClientInfo.pen;
    public string name => roomUserClientInfo.name;
    public void OnDeserialize(NetDataReader reader)
    {
        roomUserClientInfo = NetUtils.ReadObj<RoomUserClientInfo>(userInfo);
    }
}

public struct RoomUserClientInfo : INetSerializable
{
    public int pen;
    public string name;

    public void Deserialize(NetDataReader reader)
    {
        pen = reader.GetInt();
        name = reader.GetString();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(pen);
        writer.Put(name);
    }
}

public partial struct UpdateRoomMemberList
{
    public void OnDeserialize(NetDataReader reader)
    {
        
    }
}

public partial struct BroadCastMsg
{
    public void OnDeserialize(NetDataReader reader)
    {
        
    }
}


