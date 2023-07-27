using Google.Protobuf;
using System;

public abstract class NetBase
{
    // 协议返回成功
    public Action<NetMsg> OnSuccess;

    public NetMsg MsgResponse { get; private set; }

    public abstract int ListenerID { get; }

    public NetMsg MsgRequest { get; } = new NetMsg();

    public static T Get<T>(IMessage msg) where T : NetBase
    {
        return (T)Get(typeof(T), msg);
    }

    public static NetBase Get(Type type, IMessage req)
    {
        if (type == null || req == null) return null;
        var netType = (NetBase)Activator.CreateInstance(type);
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
        NetManager.Instance.AddNetListener(ListenerID, Response);
    }

    public void RemoveNetListener()
    {
        NetManager.Instance.RemoveNetListener(ListenerID);
    }

    protected virtual void Response(NetMsg msg)
    {
        MsgResponse = msg;

        OnSuccess?.Invoke(msg);
        OnSuccess = null;
    }

    public virtual void Request()
    {
        GameManager.Instance.NetMgr.Send(MsgRequest, ListenerID);
    }

    protected virtual void Clear()
    {
        RemoveNetListener();
        OnSuccess = null;
    }
}