using Google.Protobuf;
using System.IO;
using UnityEngine;

public class GU_ProtoBuf : MonoBehaviour
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

    // 网络封包方法
    // |协议数据长度|协议ID|协议内容|
    public static byte[] PackNetMsg(NetMsg data, ushort id) { 
        using (MemoryStream ms = new MemoryStream())
        {
            ms.Position = 0;
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                byte[] msgData = Serialize(data);
                ushort len = (ushort)msgData.Length;
                bw.Write(len);
                bw.Write(id);
                bw.Write(msgData);
                bw.Flush();
#if UNITY_EDITOR
                Debug.Log($"协议长度:{len} 协议ID:{id} 数据:{msgData}");
#endif
                return ms.ToArray();
            }
        }
    }

    // 网络解包方法
    // |协议数据长度|协议ID|协议内容|
    public static NetMsg UnpackNetMsg(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                ushort len = br.ReadUInt16();
                ushort id = br.ReadUInt16();
#if UNITY_EDITOR
                Debug.Log($"协议长度:{len} 协议ID:{id} 数据:{data}");
#endif
                if (len <= data.Length - 4)
                {
                    NetMsg msg = Deserialize(br.ReadBytes(len)) as NetMsg;
                    return msg;
                }
                else
                {
                    Debug.LogError("协议长度错误!!");
                }
            }
        }

        return null;
    }
}