using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Net = UdpTest.NetHelper;
using UdpTest;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

public class Server
{

    public GameState CurrentState { get; set; }

    public bool isServer = false;
    public ConcurrentQueue<NetData> receivedData = new ConcurrentQueue<NetData>();
    public ConcurrentDictionary<IPEndPoint, NetConnection> connections = new ConcurrentDictionary<IPEndPoint, NetConnection>();
    public IPEndPoint[] connectionKeys;

    int MaxConnections = 4;

    IPEndPoint localEndpoint;
    IPEndPoint recvEndPoint;
    IPEndPoint anyEndPoint;
    UdpClient client;

    int port = Net.ServerPort;
    Thread thread;
    Random r = new Random();

    byte serverTick = 0;

    TimeSpan lastFrameTime = new TimeSpan(0);

    // Start is called before the first frame update
    public void Start()
    {
        CurrentState = new GameState();

        anyEndPoint = new IPEndPoint(IPAddress.Any, port);
        recvEndPoint = anyEndPoint;
        client = new UdpClient(recvEndPoint);
        Net.SetUdpConnReset(client);

        thread = new Thread(new ThreadStart(ThreadMethod));
        thread.IsBackground = true;
        thread.Start();
    }

    private void ThreadMethod()
    {
        NetData data;
        NetConnection connection;

        while (true)
        {
            data = AwaitMessage();

            if (data.MsgType == NetData.MessageType.Invalid)
                Debug.LogError(data.ErrorMsg);

            if (data.MsgType == NetData.MessageType.TimeSync && connections.TryGetValue(recvEndPoint, out connection))
            {
                NetData response = new NetData(data.SyncId);
                SendNetData(response);
                connection.CurrentState = NetConnection.State.TimeSync;
            }
            else
            {
                if (data.MsgType == NetData.MessageType.Connect)
                {
                    HandleConnectionMsg(data);
                }
                else if (connections.TryGetValue(recvEndPoint, out connection))
                {
                    if (data.MsgType == NetData.MessageType.Data)
                    {
                        if (data.Tick != connection.LastMsg.Tick)
                        {
                            int index = connection.AckData(data.AckId);
                            if (index >= 0)
                                Debug.SetPinMessage($"Rtt: {(NetData.GetTimeStamp() - connection.SentData[index].TimeStamp).ToString("D6")} - Average Rtt: {connection.GetAverageRtt().ToString("0000.00")}");
                        }

                        if (data.Commands != null && data.Commands.Length > 0)
                        {
                            if (!connection.hasMoved)
                                connection.hasMoved = true;

                            for (int i = 0; i < data.Commands.Length; i++)
                            {
                                if (connection.LastCommandId < data.Commands[i].Id || connection.LastCommandId > ushort.MaxValue / 2 && data.Commands[i].Id > 0)
                                {
                                    data.Commands[i].Perform(CurrentState, connection.Id);
                                    connection.LastCommandId = data.Commands[i].Id;
                                }
                            }
                        }
                        //if (index >= 0)
                        //{
                        //}
                        //receivedData.Enqueue(data);
                    }
                    else if (data.MsgType == NetData.MessageType.FinishedLoading)
                    {
                        connection.CurrentState = NetConnection.State.Ready;
                    }

                    if (data.MsgType == NetData.MessageType.Invalid)
                    {
                        Disconnect(recvEndPoint, false);
                    }
                    else
                    {
                        connection.LastMsg = data;
                        connection.LastMsgTime = NetData.watch.Elapsed;
                    }
                }
            }
        }
    }

    private void Disconnect(IPEndPoint endPoint, bool sendDisconnectMessage = true)
    {
        if (connections.TryRemove(endPoint, out NetConnection connection))
        {
            Debug.Log($"Disconnecting: {endPoint}");
            CurrentState.RemovePlayer(connection.Id);
            connectionKeys = connections.Keys.ToArray();
            if (sendDisconnectMessage)
                SendNetData(new NetData(NetData.MessageType.Disconnect), endPoint);
        }
    }

    private void HandleConnectionMsg(NetData data)
    {
        if (!connections.TryGetValue(recvEndPoint, out NetConnection connection))
        {
            if (connections.Count < MaxConnections)
            {
                connection = new NetConnection();
                connections[recvEndPoint] = connection;
                CurrentState.AddPlayer(connection.Id);
                connectionKeys = connections.Keys.ToArray();
                //Debug.AddPoint(connections[recvEndPoint].Id, 0, 0);
            }
            else
                return;
        }
        NetData response = NetData.ConnectMessage;
        byte[] connectionIdBytes = BitConverter.GetBytes(connection.Id);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(connectionIdBytes);
        response.RawData = connectionIdBytes;
        SendNetData(response);
    }

    private void SendNetData(NetData data, IPEndPoint endPoint)
    {
        if (data.MsgType != NetData.MessageType.Data)
        {
            Net.SendNetData(data, client, endPoint);
        }
        else if (connections.TryGetValue(endPoint, out NetConnection connection))
        {
            data.AckId = connection.LastCommandId;
            connection.AddSentData(data);
            Net.SendNetData(data, client, endPoint);
        }


    }

    private void SendNetData(NetData data)
    {
        SendNetData(data, recvEndPoint);
    }

    private NetData AwaitMessage()
    {
        recvEndPoint = anyEndPoint;
        return Net.AwaitData(client, ref recvEndPoint, true);
    }

    public void ServerUpdate()
    {
        TimeSpan startTime = NetData.watch.Elapsed;
        serverTick = (byte)((serverTick + 1) % (byte.MaxValue + 1));

        SimulatePhysics();

        Debug.Update();
        if (connectionKeys != null)
        {
            NetConnection connection;
            for (int i = 0; i < connectionKeys.Length; i++)
            {
                if (connections.TryGetValue(connectionKeys[i], out connection) && connection.CurrentState == NetConnection.State.Ready)
                {
                    if ((NetData.watch.Elapsed - connection.LastMsgTime).TotalSeconds >= Net.DisconnectTimer)
                    {
                        Debug.Log($"Lost contact to {connectionKeys[i]}");
                        Disconnect(connectionKeys[i]);
                    }
                    else
                    {
                        long timeDiff = NetData.GetTimeStamp() - connection.LastSendAdjustedTimeStamp;
                        if (timeDiff > Net.SendDelay * 2000)
                        {
                            timeDiff = Net.SendDelay * 1000;
                        }
                        if (timeDiff >= Net.SendDelay * 1000)
                        {
                            SendGameState(connectionKeys[i]);
                            connection.LastSendAdjustedTimeStamp = NetData.GetTimeStamp() + Net.SendDelay * 1000 - timeDiff;
                        }
                    }
                }
            }

        }

        lastFrameTime = NetData.watch.Elapsed - startTime;
        //Debug.SetPinMessage($"Frametime: {((long)(lastFrameTime.TotalMilliseconds * 1000)).ToString("D6")}");
        if (lastFrameTime.TotalMilliseconds + 1 < Net.UpdateDelay)
        {
            Thread.Sleep((int)(Net.UpdateDelay - lastFrameTime.TotalMilliseconds - 1));
            lastFrameTime = NetData.watch.Elapsed - startTime;
        }

    }

    private void SimulatePhysics()
    {
        foreach (var connection in connections.Values)
        {
            if (!connection.hasMoved)
            {
                int moveX = 0, moveY = 0;

                switch (r.Next(10))
                {
                    case 0:
                    case 1:
                    case 2:
                        moveX++;
                        break;
                    case 3:
                    case 4:
                        //moveY++;
                        break;
                    case 5:
                        //moveY--;
                        break;
                    case 6:
                        //moveX--;
                        break;
                    default:
                        break;
                }
                if (CurrentState.Players != null)
                    CurrentState.MovePlayer(connection.Id, (short)moveX, (short)moveY);
            }
        }
        Debug.PointsFromState(CurrentState);
    }

    private void SendGameState(IPEndPoint endPoint)
    {
        //byte[] gameState = new byte[Debug.points.Count * sizeof(int) * 2];
        //int bIndex = 0;

        //foreach (System.Drawing.Point point in Debug.points.Values)
        //{
        //    BitConverter.TryWriteBytes(new Span<byte>(gameState, bIndex, sizeof(int)), point.X);
        //    bIndex += sizeof(int);
        //    BitConverter.TryWriteBytes(new Span<byte>(gameState, bIndex, sizeof(int)), point.Y);
        //    bIndex += sizeof(int);
        //}

        SendNetData(new NetData(NetData.MessageType.Data) { GameState = CurrentState, Tick = serverTick }, endPoint);
    }

    private void OnApplicationQuit()
    {
        client.Close();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
