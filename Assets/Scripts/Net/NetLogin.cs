public class NetLogin : NetBase
{
    public override int ListenerID => NetConst.NetMsgCode.SC_Login_Res;

    private static NetLogin Get(CS_Login_Req req)
    {
        return Get(typeof(NetLogin), req) as NetLogin;
    }

    public static NetLogin Get(int uid, string pwd)
    {
        var loginReq = new CS_Login_Req()
        {
            Uid = uid,
            Pwd = pwd,
        };

        return Get(loginReq);
    }
}