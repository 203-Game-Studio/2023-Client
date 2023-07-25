using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public delegate void NetMsgCallBack(NetMsg msg);

/// <summary>
/// 网络管理器
/// </summary>
public class GM_Net : MonoBehaviour, IManager
{
    //private readonly string serverName = "203 Game Sever";//服务器名
    //private readonly int serverID = 1;//服务器ID
    private readonly string serverIP = "39.105.212.17";//服务器ip地址 "127.0.0.1"
    private readonly int serverPort = 9903;//端口号

    private Client client = new Client();

    public static GM_Net Instance => GameManager.Instance.gmNet;

    public void OnManagerInit() 
    {
        StartClient();
    }

    public void OnManagerUpdate(float deltTime)
    {
        client.Update(Time.unscaledDeltaTime);
    }

    public void OnManagerDestroy()
    {
        client.Clear();
    }

    public void StartClient()
    {
        client.Init(serverIP, serverPort);
        client.Clear();
        client.Start();
    }

    //给服务器发送数据
    public void Send(NetMsg data, int id)
    {
        client.Send(PackNetMsg(data, id));
    }

    //注册监听
    public void AddNetListener(int id, NetMsgCallBack cb) {
        client.netListenerMap.TryAdd(id, cb);
    }

    //注册监听
    public void RemoveNetListener(int id) {
        client.netListenerMap.Remove(id);
    }

    public enum E_NetState
    {
        None,

        // 开始连接
        LinkStart,

        //连接中
        Linking,

        //连接成功
        LinkSuccess,

        //连接失败
        LinkFail,

        //连接超时
        LinkTimeout,

        // 连接中
        Connenting,

        // 连接断开
        ConnectBreak,

        // 结束
        ToClose,
        Closed,
    }

    public class Client
    {
        private TcpClient client;
        private NetworkStream clientStream;

        //会自动扩容 先开50个
        private ByteBuffer byteBuffer = new ByteBuffer(1024 * 50);

        //ip地址
        private string ip;
        //端口
        private int port; 

        //当前状态
        public E_NetState mState = E_NetState.None;
        public bool isConnecting => mState == E_NetState.Connenting;

        //手动控制
        public bool toLink = false;
        public bool toClose = false;

        //计时
        public float time = 0;

        //事件
        private Action Event_LinkFail;
        private Action Event_LinkSuccess;
        private Action Event_LinkTimeout;
        private Action Event_ConnectBreak;
        private Action Event_SendError;

        //接收缓冲大小
        public const int BUFFER_SIZE = 1024 * 5;

        //缓冲区
        byte[] rawBuff = new byte[BUFFER_SIZE];

        //接收数据队列
        public Queue<byte[]> receiveBytesList = new Queue<byte[]>();
        //发送数据队列
        public Queue<byte[]> sendBytesList = new Queue<byte[]>();

        //监听注册map
        public Dictionary<int, NetMsgCallBack> netListenerMap = new Dictionary<int, NetMsgCallBack>(); 

        //初始化
        public void Init(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
            time = 0;
        }

        public void Start() {
            toLink = true;
            toClose = false;
        }

        public void Close() {
            toLink = false;
            toClose = true;
        }

        //update 操作
        public void Update(float deltaTime)
        {
            //非启动状态，允许连接服务器
            if (mState == E_NetState.None || mState == E_NetState.Closed)
            {
                if (toLink)
                {
                    mState = E_NetState.LinkStart;
                }
            }

            if (mState != E_NetState.None && mState != E_NetState.Closed)
            {
                if (toClose)
                {
                    mState = E_NetState.ToClose;
                }
            }

            if (mState == E_NetState.LinkStart)//开始连接
            {
                mState = E_NetState.Linking;//状态修改
                Connect();
            }

            if (mState == E_NetState.Linking)//等待结果
            {
                if (time > 0)
                {
                    time -= Time.deltaTime;
                    if (time <= 0)
                    {
                        mState = E_NetState.LinkTimeout;
                    }
                }
            }

            if (mState == E_NetState.LinkSuccess)
            {
                Debug.Log("服务器连接成功");
                mState = E_NetState.Connenting;

                //开始接收服务器推送
                StartReceive();

                //连接成功回调
                Event_LinkSuccess?.Invoke();
            }

            if (mState == E_NetState.LinkFail)
            {
                Debug.LogError("服务器连接失败");
                mState = E_NetState.ToClose;

                //连接失败回调
                Event_LinkFail?.Invoke();
            }

            if (mState == E_NetState.LinkTimeout)
            {
                Debug.LogError("服务器连接超时");
                mState = E_NetState.ToClose;

                //连接超时回调
                Event_LinkTimeout?.Invoke();
            }


            if (mState == E_NetState.Connenting)
            {
                SendUpdate();//开启消息发送
                ReceiveUpdate();//开启消息接收

                //断线
                if (client != null && !client.Connected)
                {
                    mState = E_NetState.ConnectBreak;
                }

                //todo:超时判断
            }

            //网络连接中断
            if (mState == E_NetState.ConnectBreak)
            {
                Event_ConnectBreak?.Invoke();
                mState = E_NetState.ToClose;
            }

            //清除连接 
            if (mState == E_NetState.ToClose)
            {
                Close();
                mState = E_NetState.Closed;
            }
        }

        //连接
        public void Connect()
        {
            client = new TcpClient();
            client.BeginConnect(ip, port, OnEndConnect, client);
        }

        //停止连接
        private void OnEndConnect(IAsyncResult ar)
        {
            try
            {
                TcpClient tcpClient = (TcpClient)ar.AsyncState;
                tcpClient.EndConnect(ar);
                clientStream = tcpClient.GetStream();
                mState = E_NetState.LinkSuccess;
            }
            catch (Exception e)
            {
                mState = E_NetState.LinkFail;
                Debug.LogError(e);
                Clear();
            }
        }

        //开始接收消息
        private void StartReceive()
        {
            clientStream.BeginRead(rawBuff, 0, rawBuff.Length, OnReceiveData, clientStream);
            Debug.Log("开始接收消息");
        }

        private void ReReceive()
        {
            clientStream.BeginRead(rawBuff, 0, rawBuff.Length, OnReceiveData, clientStream);
        }

        //持续接收服务器推送
        private void OnReceiveData(IAsyncResult ar)
        {
            try
            {
                if (ar == null)
                {
                    Debug.LogError($"IAsyncResult is null!");
                }

                NetworkStream steam = ar.AsyncState as NetworkStream;
                if (steam == null)
                {
                    Debug.LogError($"NetworkStream is null!");
                }


                //连接换了 清理数据
                if (steam != clientStream)
                {
                    Debug.LogError($"网络连接已更改");
                    return;
                }

                //连接已经失效，则返回
                if (clientStream == null || client == null || !client.Connected)
                {
                    Debug.LogError($"连接已失效");
                    return;
                }

                int receiveLen = steam.EndRead(ar);

                byteBuffer.SetPosition(byteBuffer.Length());
                byteBuffer.Put(rawBuff, receiveLen);
                byteBuffer.Flip();

                while (byteBuffer.Remaining() > GC_Net.HEAD_LEN)
                {
                    int dataLen = byteBuffer.GetInt();
                    int id = byteBuffer.GetInt();
                    if (byteBuffer.Remaining() >= dataLen)
                    {
                        byte[] data = new byte[dataLen];
                        byteBuffer.Get(data);
                        byteBuffer.Compact();
                        byteBuffer.Flip();
                        lock (receiveBytesList)
                        {
                            receiveBytesList.Enqueue(data);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                ReReceive();
            }
            catch (Exception e)
            {
                Debug.LogError($"服务器数据异常！ {e.Message}");
                Clear();
            }
        }

        /// <summary>
        ///解包分发消息
        ///|协议数据长度|协议ID|协议内容|
        /// </summary>
        private void ReceiveUpdate()
        {
            lock (receiveBytesList)
            {
                if (receiveBytesList.Count > 0)
                {
                    while (receiveBytesList.Count > 0)
                    {
                        try {
                            NetMsg msg = GU_ProtoBuf.Deserialize(receiveBytesList.Dequeue()) as NetMsg;

                            if (msg != null)
                            {
#if UNITY_EDITOR
                                Debug.Log($"收到服务器消息! 协议号:{msg.Code} 数据：{msg.ToString()}");
#endif
                                if (netListenerMap.TryGetValue(msg.Code, out var callback))
                                {
                                    callback(msg);
                                }
                            }
                        }
                        catch(Exception e)
                        {
                            Debug.LogError($"解包异常! {e}");
                        }
                    }
                }
            }
        }

        /// <summary>
        ///发包
        ///|协议数据长度|协议ID|协议内容|
        /// </summary>
        private void SendUpdate() {
            lock (sendBytesList)
            {
                if (sendBytesList.Count > 0)
                {
                    var data = sendBytesList.Dequeue();
                    clientStream.BeginWrite(data, 0, data.Length, OnSendComplete, clientStream);
                }
            }
        }

        //发包结束回调
        private void OnSendComplete(IAsyncResult ar)
        {
            try
            {
                NetworkStream steam = ar.AsyncState as NetworkStream;
                //防止关闭网络后，还有之前的调用．
                if (client == null || steam != clientStream || !client.Connected)
                {
                    return;
                }

                steam.EndWrite(ar);

                //发送下一个包
                byte[] data = null;
                lock (sendBytesList)
                {
                    if (sendBytesList.Count > 0)
                    {
                        data = sendBytesList.Dequeue();
                    }
                }

                if (data != null)
                {
                    steam.BeginWrite(data, 0, data.Length, OnSendComplete, steam);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"发包异常:{e}");
                Event_SendError?.Invoke();
                Clear();
            }
        }


        //给服务器发送信息
        public void Send(byte[] data)
        {
            lock (sendBytesList)
            {
                sendBytesList.Enqueue(data);
            }
        }

        //清空
        public void Clear()
        {
            if (clientStream != null)
            {
                clientStream.Close();
                clientStream = null;
            }
            if (client != null)
            {
                client.Close();
                client = null;
            }
            sendBytesList.Clear();
            receiveBytesList.Clear();
            byteBuffer.Clear();

            Event_LinkFail = null;
            Event_LinkSuccess = null;
            Event_LinkTimeout = null;
            Event_ConnectBreak = null;
            Event_SendError = null;
        }
    }



    // 网络封包方法
    // |协议数据长度|协议ID|协议内容|
    public static byte[] PackNetMsg(NetMsg data, int id)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            ms.Position = 0;
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                byte[] msgData = GU_ProtoBuf.Serialize(data);
                bw.Write(msgData.Length);
                bw.Write(id);
                bw.Write(msgData);
                bw.Flush();
#if UNITY_EDITOR
                Debug.Log($"协议长度:{msgData.Length} 协议ID:{id} 数据:{msgData}");
#endif
                return ms.ToArray();
            }
        }
    }

    public class ByteBuffer
    {
        private readonly MemoryStream msgBuff;

        public ByteBuffer(int maxSize)
        {
            msgBuff = new MemoryStream(maxSize);
        }

        public void Put(byte[] data, int length)
        {
            msgBuff.Write(data, 0, length);
        }

        public void Put(byte[] data)
        {
            msgBuff.Write(data, 0, data.Length);
        }

        public void Get(byte[] data)
        {
            msgBuff.Read(data, 0, data.Length);
        }

        public int GetInt()
        {
            byte[] bytes = new byte[4];
            msgBuff.Read(bytes, 0, 4);
            return BitConverter.ToInt32(bytes, 0);
        }

        public ushort GetUShort()
        {
            byte[] bytes = new byte[2];
            msgBuff.Read(bytes, 0, 2);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public byte[] GetBuffer()
        {
            return msgBuff.GetBuffer();
        }

        public void Flip()
        {
            msgBuff.Seek(0, SeekOrigin.Begin);
        }

        public void Compact()
        {
            if (msgBuff.Position == 0)
            {
                return;
            }

            long remaining = Remaining();
            if (remaining <= 0)
            {
                Clear();
                return;
            }

            byte[] leftData = new byte[remaining];
            Get(leftData);
            Clear();
            Put(leftData);
        }

        public void Clear()
        {
            msgBuff.Seek(0, SeekOrigin.Begin);
            msgBuff.SetLength(0);
        }

        public long Remaining()
        {
            return msgBuff.Length - msgBuff.Position;
        }

        public bool HasRemaining()
        {
            return msgBuff.Length > msgBuff.Position;
        }

        public long Position()
        {
            return msgBuff.Position;
        }

        public long Length()
        {
            return msgBuff.Length;
        }

        public void SetPosition(long position)
        {
            msgBuff.Seek(position, SeekOrigin.Begin);
        }
    }
}