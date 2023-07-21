using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class GM_Net : MonoBehaviour, IManager
{
    private readonly string serverIP = "39.105.212.17";//服务器ip地址
    private readonly int serverPort = 9903;//端口号

    private ClientSocket clientSocket;

    public void OnManagerInit() {
        clientSocket = new ClientSocket(serverIP, serverPort);
    }

    public void OnManagerUpdate(float deltTime)
    {
        //Debug.Log($"GM_Net OnManagerUpdate {deltTime}");
    }

    public void OnManagerDestroy()
    {
        //Debug.Log("GM_Net OnManagerDestroy");
        clientSocket?.Close();
    }

    //给服务器发送数据
    public void Send(NetMsg data, ushort id) {
        clientSocket?.Send(GU_ProtoBuf.PackNetMsg(data, id));
    }

    public class ClientSocket
    {
        private Socket clientSocket;
        private Thread receiveThread;
        private string ip;  //ip地址
        private int port;  //端口

        public enum E_NetState
        {
            None,

            // 开始连接、重新开始连接
            LinkStart,
            Linking,
            LinkOK,
            LinkFail,

            //连接中
            Connenting,

            // 连接断开
            ConnectBreak,


            // 结束
            To_Close,
            Closed,
        }

        public E_NetState mState = E_NetState.None;

        public ClientSocket(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
            Connect();
        }

        //连接
        private void Connect()
        {
            try
            {
                mState = E_NetState.LinkStart;
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
                mState = E_NetState.Connenting;
                StartReceiveThread();
            }
            catch
            {
                throw;
            }
        }
        //开始接收消息
        private void StartReceiveThread()
        {
            receiveThread = new Thread(OnReceive);
            receiveThread.IsBackground = true;
            Debug.Log("开始接收消息");
            receiveThread.Start();
        }

        //持续接收服务器推送
        private void OnReceive()
        {
            while (true)
            {
                byte[] data = new byte[1024 * 8];
                //获取数据 
                clientSocket.Receive(data);

                NetMsg result = GU_ProtoBuf.UnpackNetMsg(data);

                if (result != null)
                {
                    Debug.Log($"接收到服务器数据 ID：{result.Code}");
                    try {
                        switch (ushort.Parse(result.Code))
                        {
                            case GC_Net.NetMsgCode.SC_Login_Res:
                                Debug.Log($"服务器数据 DeviceId：{result.LoginRes.DeviceId} Code：{result.LoginRes.Code}");
                                break;
                            case GC_Net.NetMsgCode.CS_Login_Req:
                                Debug.Log($"服务器数据 DeviceId：{result.LoginReq.Uid} Code：{result.LoginReq.Pwd}");
                                break;
                            case GC_Net.NetMsgCode.SC_Logout_Res:
                                Debug.Log($"服务器数据 Code：{result.LogoutRes.Code}");
                                break;
                        }
                    }
                    catch(Exception e)
                    {
                        Debug.LogError($"服务器数据异常！ {e.Message}");
                        Close();
                    }
                }
            }
        }

        //给服务器发送信息
        public void Send(byte[] data)
        {
            try
            {
                Debug.Log($"发送数据:{data}");
                clientSocket.Send(data);
            }
            catch (Exception)
            {
                Close();
            }
        }

        //断开连接
        public void Close()
        {
            receiveThread.Abort();
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }
}