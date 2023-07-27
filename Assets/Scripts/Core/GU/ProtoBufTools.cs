using Google.Protobuf;
using System.IO;
using UnityEngine;

public class ProtoBufTools : MonoBehaviour
{
    // 序列化
    public static byte[] Serialize(IMessage msg)
    {
        return msg.ToByteArray();
    }

    // 反序列化  
    static public IMessage Deserialize(byte[] data)
    {
        IMessage result = default(IMessage);
        if (data != null)
        {
            result = NetMsg.Descriptor.Parser.ParseFrom(data);
        }
        return result;
    }
}