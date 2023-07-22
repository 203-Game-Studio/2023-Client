using Google.Protobuf;
using System;

public abstract class Net_Base
{
    // 协议返回成功
    public Action<NetMsg> OnSuccess;

    public NetMsg MsgResponse { get; private set; }

    public abstract int ListenerID { get; }

    public NetMsg MsgRequest { get; } = new NetMsg();

    public static T Get<T>(IMessage msg) where T : Net_Base
    {
        return (T)Get(typeof(T), msg);
    }

    public static Net_Base Get(Type type, IMessage req)
    {
        if (type == null || req == null) return null;
        var netType = (Net_Base)Activator.CreateInstance(type);
        netType?.Init(req);
        return netType;
    }

    public virtual void Init(IMessage req)
    {
        if (req == null) return;
        AddNetListener();
        foreach (var msgName in Enum.GetNames(typeof(NetMsg.DataOneofCase)))
        {
            var reqProperty = typeof(NetMsg).GetProperty(msgName);
            if (reqProperty != null && reqProperty.PropertyType == req.GetType())
            {
                reqProperty.SetValue(MsgRequest, req);
                break;
            }
        }
    }

    public void AddNetListener()
    {
        GM_Net.Instance.AddNetListener(ListenerID, Response);
    }

    public void RemoveNetListener()
    {
        GM_Net.Instance.RemoveNetListener(ListenerID);
    }

    protected virtual void Response(NetMsg msg)
    {
        MsgResponse = msg;

        OnSuccess?.Invoke(msg);
        OnSuccess = null;
    }

    public virtual void Request()
    {
        GameManager.Instance.gmNet.Send(MsgRequest, ListenerID);
    }

    protected virtual void Clear()
    {
        RemoveNetListener();
        OnSuccess = null;
    }
}