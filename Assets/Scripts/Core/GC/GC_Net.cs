using UnityEngine;

public class GC_Net : MonoBehaviour
{
    public class NetMsgCode
    {
        //登录 登出
        public const ushort CS_Login_Req  = 101;
        public const ushort SC_Login_Res  = 102;
        public const ushort CS_Logout_Req = 103;
        public const ushort SC_Logout_Res = 104;
    }
}