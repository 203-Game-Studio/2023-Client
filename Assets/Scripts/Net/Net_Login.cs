public class Net_Login : Net_Base
{
    public override int ListenerID => GC_Net.NetMsgCode.SC_Login_Res;

    private static Net_Login Get(CS_Login_Req req)
    {
        return Get(typeof(Net_Login), req) as Net_Login;
    }

    public static Net_Login Get(int uid, string pwd)
    {
        var loginReq = new CS_Login_Req()
        {
            Uid = uid,
            Pwd = pwd,
        };

        return Get(loginReq);
    }
}