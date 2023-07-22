using UnityEngine;

public class TestLogin : MonoBehaviour
{

    void Start()
    {
        var req = Net_Login.Get(123456, "654321");
        req.OnSuccess = (NetMsg msg) =>
        {
            Debug.Log($"�յ���������¼�ظ���{msg.Code} {msg.LoginRes.Code}  {msg.LoginRes.DeviceId}");
        };
        req.Request();
    }
}