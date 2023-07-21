using UnityEngine;
using Google.Protobuf;

public class TestLogin : MonoBehaviour
{
    User user;
    byte[] data;
    void Start()
    {
        user = new User
        {
            Uid = 123456,
            Pwd = "654321"
        };
        data = user.ToByteArray();
        Debug.Log($"{FromProto(data).Uid}-{FromProto(data).Pwd}");
    }

    //反序列化
    User FromProto(byte[] buffer)
    {
        IMessage message = new User();
        User person = message.Descriptor.Parser.ParseFrom(buffer) as User;
        return person;
    }
}