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

    public const int HEAD_LEN_LEN = 2;
    public const int HEAD_ID_LEN  = 2;

    public static int HEAD_LEN { get { return HEAD_ID_LEN + HEAD_LEN_LEN; } }
}