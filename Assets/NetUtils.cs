using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;

public static class NetUtils
{
    static NetDataWriter _writer = new NetDataWriter();
    public static byte[] GetBytes(INetSerializable netSerializable)
    {
        _writer.Reset();
        _writer.Put(netSerializable);
        return _writer.CopyData();
    }

    
    static NetDataReader _reader = new NetDataReader();
    public static T ReadObj<T>(byte[] bytes) where T : struct, INetSerializable
    {
        _reader.SetSource(bytes);
        return _reader.Get<T>();
    }

}
