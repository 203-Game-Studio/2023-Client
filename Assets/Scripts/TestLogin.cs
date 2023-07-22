using UnityEngine;
using Google.Protobuf;

public class TestLogin : MonoBehaviour
{

    void Start()
    {
        CS_Login_Req loginReq = new CS_Login_Req
        {
            Uid = 123456,
            Pwd = "654321"
        };

        NetMsg netMsg = new NetMsg
        {
            LoginReq = loginReq,
            Code = GC_Net.NetMsgCode.CS_Login_Req,
        };

        GM_Net.Instance.AddNetListener(GC_Net.NetMsgCode.SC_Login_Res, OnLoginRes);

        GameManager.Instance.gmNet.Send(netMsg, GC_Net.NetMsgCode.CS_Login_Req);
    }

    void OnLoginRes(NetMsg msg) { 
        Debug.Log($"收到服务器登录回复！{msg.Code} {msg.LoginRes.Code}  {msg.LoginRes.DeviceId}");
    }
}