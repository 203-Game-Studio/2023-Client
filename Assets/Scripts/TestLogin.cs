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
            Code = GC_Net.NetMsgCode.CS_Login_Req.ToString(),
        };

        GameManager.Instance.gmNet.Send(netMsg, GC_Net.NetMsgCode.CS_Login_Req);
    }
}