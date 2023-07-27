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
public class NetManager : MonoBehaviour, IManager
{
    //private readonly string serverName = "203 Game Sever";//服务器名
    //private readonly int serverID = 1;//服务器ID
    private readonly string _serverIP = "39.105.212.17";//服务器ip地址 "127.0.0.1"
    private readonly int _serverPort = 9903;//端口号

    private Client _client = new Client();

    public static NetManager Instance => GameManager.Instance.NetMgr;

    public void OnManagerInit() 
    {
        StartClient();
    }

    public void OnManagerUpdate(float deltTime)
    {
        _client.Update(Time.unscaledDeltaTime);
    }

    public void OnManagerDestroy()
    {
        _client.Clear();
    }

    public void StartClient()
    {
        _client.Init(_serverIP, _serverPort);
        _client.Clear();
        _client.Start();
    }

    //给服务器发送数据
    public void Send(NetMsg data, int id)
    {
        _client.Send(PackNetMsg(data, id));
    }

    //注册监听
    public void AddNetListener(int id, NetMsgCallBack cb) {
        _client.netListenerMap.TryAdd(id, cb);
    }

    //注册监听
    public void RemoveNetListener(int id) {
        _client.netListenerMap.Remove(id);
    }

    public enum ENetState
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
        private TcpClient _client;
        private NetworkStream _clientStream;

        //会自动扩容 先开50个
        private ByteBuffer byteBuffer = new ByteBuffer(1024 * 50);

        //ip地址
        private string _ip;
        //端口
        private int _port; 

        //当前状态
        private ENetState _state = ENetState.None;
        public bool IsConnecting => _state == ENetState.Connenting;

        //手动控制
        private bool _toLink = false;
        private bool _toClose = false;

        //计时
        private float _time = 0;

        //事件
        private Action _eventLinkFail;
        private Action _eventLinkSuccess;
        private Action _eventLinkTimeout;
        private Action _eventConnectBreak;
        private Action _eventSendError;

        //接收缓冲大小
        public const int BUFFER_SIZE = 1024 * 5;

        //缓冲区
        byte[] _rawBuff = new byte[BUFFER_SIZE];

        //接收数据队列
        private Queue<byte[]> _receiveBytesList = new Queue<byte[]>();
        //发送数据队列
        private Queue<byte[]> _sendBytesList = new Queue<byte[]>();

        //监听注册map
        public Dictionary<int, NetMsgCallBack> netListenerMap = new Dictionary<int, NetMsgCallBack>(); 

        //初始化
        public void Init(string ip, int port)
        {
            this._ip = ip;
            this._port = port;
            _time = 0;
        }

        public void Start() {
            _toLink = true;
            _toClose = false;
        }

        public void Close() {
            _toLink = false;
            _toClose = true;
        }

        //update 操作
        public void Update(float deltaTime)
        {
            //非启动状态，允许连接服务器
            if (_state == ENetState.None || _state == ENetState.Closed)
            {
                if (_toLink)
                {
                    _state = ENetState.LinkStart;
                }
            }

            if (_state != ENetState.None && _state != ENetState.Closed)
            {
                if (_toClose)
                {
                    _state = ENetState.ToClose;
                }
            }

            if (_state == ENetState.LinkStart)//开始连接
            {
                _state = ENetState.Linking;//状态修改
                Connect();
            }

            if (_state == ENetState.Linking)//等待结果
            {
                if (_time > 0)
                {
                    _time -= Time.deltaTime;
                    if (_time <= 0)
                    {
                        _state = ENetState.LinkTimeout;
                    }
                }
            }

            if (_state == ENetState.LinkSuccess)
            {
                Debug.Log("服务器连接成功");
                _state = ENetState.Connenting;

                //开始接收服务器推送
                StartReceive();

                //连接成功回调
                _eventLinkSuccess?.Invoke();
            }

            if (_state == ENetState.LinkFail)
            {
                Debug.LogError("服务器连接失败");
                _state = ENetState.ToClose;

                //连接失败回调
                _eventLinkFail?.Invoke();
            }

            if (_state == ENetState.LinkTimeout)
            {
                Debug.LogError("服务器连接超时");
                _state = ENetState.ToClose;

                //连接超时回调
                _eventLinkTimeout?.Invoke();
            }


            if (_state == ENetState.Connenting)
            {
                SendUpdate();//开启消息发送
                ReceiveUpdate();//开启消息接收

                //断线
                if (_client != null && !_client.Connected)
                {
                    _state = ENetState.ConnectBreak;
                }

                //todo:超时判断
            }

            //网络连接中断
            if (_state == ENetState.ConnectBreak)
            {
                _eventConnectBreak?.Invoke();
                _state = ENetState.ToClose;
            }

            //清除连接 
            if (_state == ENetState.ToClose)
            {
                Close();
                _state = ENetState.Closed;
            }
        }

        //连接
        public void Connect()
        {
            _client = new TcpClient();
            _client.BeginConnect(_ip, _port, OnEndConnect, _client);
        }

        //停止连接
        private void OnEndConnect(IAsyncResult ar)
        {
            try
            {
                TcpClient tcpClient = (TcpClient)ar.AsyncState;
                tcpClient.EndConnect(ar);
                _clientStream = tcpClient.GetStream();
                _state = ENetState.LinkSuccess;
            }
            catch (Exception e)
            {
                _state = ENetState.LinkFail;
                Debug.LogError(e);
                Clear();
            }
        }

        //开始接收消息
        private void StartReceive()
        {
            _clientStream.BeginRead(_rawBuff, 0, _rawBuff.Length, OnReceiveData, _clientStream);
            Debug.Log("开始接收消息");
        }

        private void ReReceive()
        {
            _clientStream.BeginRead(_rawBuff, 0, _rawBuff.Length, OnReceiveData, _clientStream);
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
                if (steam != _clientStream)
                {
                    Debug.LogError($"网络连接已更改");
                    return;
                }

                //连接已经失效，则返回
                if (_clientStream == null || _client == null || !_client.Connected)
                {
                    Debug.LogError($"连接已失效");
                    return;
                }

                int receiveLen = steam.EndRead(ar);

                byteBuffer.SetPosition(byteBuffer.Length());
                byteBuffer.Put(_rawBuff, receiveLen);
                byteBuffer.Flip();

                while (byteBuffer.Remaining() > NetConst.HEAD_LEN)
                {
                    int dataLen = byteBuffer.GetInt();
                    int id = byteBuffer.GetInt();
                    if (byteBuffer.Remaining() >= dataLen)
                    {
                        byte[] data = new byte[dataLen];
                        byteBuffer.Get(data);
                        byteBuffer.Compact();
                        byteBuffer.Flip();
                        lock (_receiveBytesList)
                        {
                            _receiveBytesList.Enqueue(data);
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
            lock (_receiveBytesList)
            {
                if (_receiveBytesList.Count > 0)
                {
                    while (_receiveBytesList.Count > 0)
                    {
                        try {
                            NetMsg msg = ProtoBufTools.Deserialize(_receiveBytesList.Dequeue()) as NetMsg;

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
            lock (_sendBytesList)
            {
                if (_sendBytesList.Count > 0)
                {
                    var data = _sendBytesList.Dequeue();
                    _clientStream.BeginWrite(data, 0, data.Length, OnSendComplete, _clientStream);
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
                if (_client == null || steam != _clientStream || !_client.Connected)
                {
                    return;
                }

                steam.EndWrite(ar);

                //发送下一个包
                byte[] data = null;
                lock (_sendBytesList)
                {
                    if (_sendBytesList.Count > 0)
                    {
                        data = _sendBytesList.Dequeue();
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
                _eventSendError?.Invoke();
                Clear();
            }
        }


        //给服务器发送信息
        public void Send(byte[] data)
        {
            lock (_sendBytesList)
            {
                _sendBytesList.Enqueue(data);
            }
        }

        //清空
        public void Clear()
        {
            if (_clientStream != null)
            {
                _clientStream.Close();
                _clientStream = null;
            }
            if (_client != null)
            {
                _client.Close();
                _client = null;
            }
            _sendBytesList.Clear();
            _receiveBytesList.Clear();
            byteBuffer.Clear();

            _eventLinkFail = null;
            _eventLinkSuccess = null;
            _eventLinkTimeout = null;
            _eventConnectBreak = null;
            _eventSendError = null;
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
                byte[] msgData = ProtoBufTools.Serialize(data);
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