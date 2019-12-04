using Net = UdpTest.NetHelper;
using UdpTest;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;
using System;
using System.Net;
using System.Text;
using Diagnostics = System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace UdpTest
{
    public class Client
    {
        private ConcurrentQueue<NetData> receivedData;
        public ConcurrentQueue<NetData> sendData;
        private UdpClient client;
        private Thread thread;
        private bool connected;
        private GameState[] gameStates;
        private long[] timeStamps;
        //public GameState lastReceivedState;
        public GameState oldDisplayState;
        public GameState displayState;
        private NetData lastData;
        private byte clientTick;

        private Mutex stateMutex;

        private ClientCommand[] commands;
        public int unAckedCommands;
        public const int MaxCommands = 100;
        //private Dictionary<ushort, ClientCommand[]> sentCommands;

        public ushort Id { get; set; }

        public double MinRtt { get; private set; }
        public long TimeOffset { get; private set; }

        private IPEndPoint serverEndPoint;
        Random r = new Random();

        public Client()
        {
            receivedData = new ConcurrentQueue<NetData>();
            sendData = new ConcurrentQueue<NetData>();
            client = new UdpClient(0);
            Net.SetUdpConnReset(client);

            connected = false;
            gameStates = new GameState[5];
            timeStamps = new long[gameStates.Length];
            for (int i = 0; i < gameStates.Length; i++)
            {
                gameStates[i] = new GameState();
                timeStamps[i] = 0;
            }
            //lastReceivedState = new GameState();
            //oldState = lastReceivedState;
            displayState = new GameState();
            oldDisplayState = displayState;
            commands = new ClientCommand[MaxCommands];
            stateMutex = new Mutex();
            //sentCommands = new Dictionary<ushort, ClientCommand[]>();
        }

        private void AddCommand(ClientCommand command)
        {
            if (unAckedCommands < MaxCommands)
            {
                commands[unAckedCommands++] = command;
            }
            else
            {
                for (int i = 0; i < MaxCommands - 1; i++)
                {
                    commands[i] = commands[i + 1];
                }
                commands[^1] = command;
            }
        }

        public void Connect(string ip, int port = Net.ServerPort)
        {
            if (thread == null)
            {
                thread = new Thread(new ThreadStart(ThreadMethod));
                thread.IsBackground = true;
            }
            else if (thread.ThreadState == ThreadState.Running)
            {
                throw new Exception("Multiple connection calls attempted!");
            }
            serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            thread.Start();
        }

        public void HandleKeyPress(ConsoleKey key)
        {

            if (connected)
            {
                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                        AddCommand(new ClientCommand { Left = true });
                        break;
                    case ConsoleKey.UpArrow:
                        AddCommand(new ClientCommand { Up = true });
                        break;
                    case ConsoleKey.RightArrow:
                        AddCommand(new ClientCommand { Right = true });
                        break;
                    case ConsoleKey.DownArrow:
                        AddCommand(new ClientCommand { Down = true });
                        break;
                    default:
                        break;
                }
                //lastReceivedState.MovePlayer(Id, (short)(moveX * 10), (short)(moveY * 10));
            }
            //NetData data = new NetData(NetData.MessageType.Data) { Commands = commands.Where(c => c != null).ToArray() };
            //AddSendData(data);
        }

        public void Update()
        {            
            RunCommands();
            Debug.PointsFromState(displayState);
            Debug.Update();
        }


        private void ThreadMethod()
        {
            if (!EstablishConnection())
            {
                return;
            }
            Debug.Log($"Connection made to: {serverEndPoint}");
            if (!TimeSync())
            {
                Debug.LogError("Unable to establish a stable connection.");
                return;
            }
            Debug.Log($"TimeSync successful. Minimum roundtrip time: {(int)MinRtt}");

            NetData data = null;
            for (int i = 0; i < Net.MaxConnectionAttempts; i++)
            {
                SendNetData(new NetData(NetData.MessageType.FinishedLoading));
                data = AwaitResponse();
                if (data.MsgType == NetData.MessageType.Data)
                {
                    connected = true;
                    break;
                }
                else
                {
                    Debug.Log($"Received {data}");
                }
            }


            client.Client.ReceiveTimeout = 0;
            while (connected)
            {
                clientTick++;
                int x, y, bIndex = 0;

                //if (data.MsgType == NetData.MessageType.Data && data.RawData.Count > 0)
                //{
                //    byte[] bData = data.RawData.ToArray();

                //    foreach (var key in Debug.points.Keys)
                //    {
                //        x = BitConverter.ToInt32(bData, bIndex);
                //        bIndex += sizeof(int);
                //        y = BitConverter.ToInt32(bData, bIndex);
                //        bIndex += sizeof(int);
                //        Debug.UpdatePoint(key, x, y);
                //    }
                //}
                if (data.MsgType == NetData.MessageType.Data)
                {
                    HandleReceivedState(data);

                    
                    //RunCommands();
                    Debug.SetPinMessage($"Unacked: {unAckedCommands}");
                    //if (prevPlayers != null)
                    //{
                    //    GameState.Player thisPlayer = lastReceivedState.Players.FirstOrDefault(p => p.id == Id);
                    //    GameState.Player thisPlayerLast = prevPlayers.FirstOrDefault(p => p.id == Id);
                    //    if (thisPlayer != null && thisPlayerLast != null)
                    //    {
                    //        thisPlayer.x = thisPlayerLast.x;
                    //        thisPlayer.y = thisPlayerLast.y;
                    //    }
                    //}

                    //Debug.SetPinMessage($"{(long)((data.TimeStamp - (NetData.GetTimeStamp() + TimeOffset)) / 1000)} dT: {data.TimeStamp} T: {NetData.GetTimeStamp()}");
                    //foreach (GameState.Player player in data.GameState.Players)
                    //{
                    //    if (player.id == Id)
                    //        player.id = 1339;
                    //}

                    NetData sendD;
                    bool hadDataToSend = false;
                    while (sendData.TryDequeue(out sendD))
                    {
                        SendNetData(sendD);
                        hadDataToSend = true;
                    }
                    if (!hadDataToSend)
                    {
                        SendNetData(new NetData(NetData.MessageType.Data));
                    }
                }
                data = AwaitResponse();
                if (data.MsgType == NetData.MessageType.Disconnect)
                {
                    connected = false;
                }
                //while (!sendData.IsEmpty)
                //{
                //    NetData data;
                //    if (sendData.TryDequeue(out data))
                //    {
                //        SendNetData(data);
                //    }
                //}
                //Thread.Sleep(10);
            }
        }

        private void HandleReceivedState(NetData data)
        {
            stateMutex.WaitOne();
            for (int i = 0; i < gameStates.Length - 1; i++)
            {
                gameStates[i] = gameStates[i + 1];
                timeStamps[i] = timeStamps[i + 1];
            }
            gameStates[^1] = data.GameState.Clone();
            timeStamps[^1] = NetData.GetTimeStamp();

            //lastData = data;
            //oldState = lastReceivedState;
            //lastReceivedState = data.GameState.Clone();
            AckCommands(data.AckId);
            stateMutex.ReleaseMutex();
        }

        private GameState InterPolateState(ushort id, GameState oldState, GameState toState, double fraction)
        {
            GameState result = oldState.Clone();
            GameState.Player match;
            for (int i = 0; i < (int)toState.Players?.Length; i++)
            {
                match = result.GetPlayer(toState.Players[i].id);
                if (match == null)
                    match = result.AddPlayer(toState.Players[i].id);

                if (match.id == id)
                {
                    match.x = gameStates[^1].Players[i].x;
                    match.y = gameStates[^1].Players[i].y;
                }
                else
                {
                    match.x += (short)((toState.Players[i].x - match.x) * fraction);
                    match.y += (short)((toState.Players[i].y - match.y) * fraction);
                }
            }
            //GameState result = new GameState();

            //for (int i = 0; i < toState.Players.Length; i++)
            //{
            //    result.AddPlayer(toState.Players[i].id);
            //    for (int j = 0; j < oldState.Players.Length; j++)
            //    {
            //        if (toState.Players[i].id == oldState.Players[j].id)
            //        {

            //            break;
            //        }
            //    }
            //}

            return result;
        }

        private bool RunCommands()
        {
            if (stateMutex.WaitOne(1))
            {
                double timeAgo = 0;
                int n = 1;
                long timeStamp = NetData.GetTimeStamp();
                while (timeAgo < Net.SendDelay * 1000 && n < timeStamps.Length)
                {
                    timeAgo = timeStamp - timeStamps[^++n];
                }
                timeAgo -= Net.SendDelay * 1000;

                if (timeAgo <= 0)
                    timeAgo = 0;

                double diff = timeStamps[^(n-1)] - timeStamps[^n];

                if (diff > 0)
                {
                    double frac = timeAgo / diff;
                    if (frac > 1)
                    {
                        //Debug.Log($"ERR: {timeAgo / diff} N: {n} TimeAgo: {timeAgo} Diff: {diff}");
                        frac = 1;
                    }
                    oldDisplayState = displayState.Clone();
                    displayState = InterPolateState(Id, gameStates[^n], gameStates[^(n-1)], frac);
                    
                    for (int i = 0; i < unAckedCommands; i++)
                    {
                        ClientCommand com = commands[i];
                        if (commands[i] != null)
                            commands[i].Perform(displayState, Id);
                    }
                }

                stateMutex.ReleaseMutex();
                return true;
            }
            return false;
        }

        //private void PerformCommand(ClientCommand clientCommand)
        //{
        //    if (clientCommand.Right && !clientCommand.Left)
        //    {
        //        displayState.MovePlayer(Id, 1, 0);
        //    }
        //    else if (!clientCommand.Right && clientCommand.Left)
        //    {
        //        displayState.MovePlayer(Id, -1, 0);
        //    }

        //    if (clientCommand.Up && !clientCommand.Down)
        //    {
        //        displayState.MovePlayer(Id, 0, -1);
        //    }
        //    else if (!clientCommand.Up && clientCommand.Down)
        //    {
        //        displayState.MovePlayer(Id, 0, 1);
        //    }

        //}

        private void AckCommands(ushort ackId)
        {
            //TODO make some error detection and correction

            //if (sentCommands.ContainsKey(ackId))

            //bool found;
            //for (int i = 0; i < sentCommands[ackId].Length; i++)
            //{
            //    found = false;
            //    for (int j = 0; j < prevUnAckedCommands; j++)
            //    {
            //        if (!found && sentCommands[ackId][i].Equals( commands[j]))
            //        {
            //            found = true;
            //            unAckedCommands--;
            //            break;
            //        }
            //    }
            //    if (!found)
            //        break;
            //}
            
            ClientCommand lastAcked = null;
            for (int i = 0; i < unAckedCommands; i++)
            {
                if (commands[i].Id == ackId)
                {
                    lastAcked = commands[i];
                    break;
                }
            }
            if (lastAcked == null)
                return;

            int index = -1;
            for (int i = 0; i < unAckedCommands; i++)
            {
                if (index >= 0)
                {
                    commands[i - index - 1] = commands[i];
                    commands[i] = null;
                }
                else if (commands[i] == lastAcked)
                {
                    index = i;
                }
            }
            if (index >= 0)
            {
                unAckedCommands -= index + 1;
            }

            //for (int i = 0; i < prevUnAckedCommands; i++)
            //{
            //    if (i >= unAckedCommands)
            //        commands[i] = null;
            //    else
            //        commands[i] = commands[prevUnAckedCommands - unAckedCommands + i];
            //}
            
        }

        public void AddSendData(NetData data)
        {
            sendData.Enqueue(data);
        }

        private bool TimeSync()
        {
            bool valid = false;
            int attempts = 0;
            int drops;
            double minRtt = -1;
            int minRttIndex = 0;
            double roundTripTime;
            NetData[] responses = new NetData[Net.NumberOfTimeSyncChecks];
            long[] timeStamps = new long[Net.NumberOfTimeSyncChecks];
            Diagnostics.Stopwatch watch = new Diagnostics.Stopwatch();

            client.Client.ReceiveTimeout = Net.TimeSyncTimeout;
            while (!valid && attempts++ < Net.TimeSyncAttempts)
            {
                drops = 0;
                for (int i = 0; i < Net.NumberOfTimeSyncChecks; i++)
                {
                    watch.Restart();
                    NetData sendData = new NetData(NetData.MessageType.TimeSync) { SyncId = (byte)i };
                    SendNetData(sendData);
                    responses[i] = AwaitResponse();
                    while (responses[i].MsgType != NetData.MessageType.Invalid && responses[i].SyncId < i)
                    {
                        responses[i] = AwaitResponse();
                    }
                    watch.Stop();
                    timeStamps[i] = NetData.GetTimeStamp();//NetData.watch.ElapsedMilliseconds * 1000;
                    roundTripTime = watch.Elapsed.TotalMilliseconds;

                    if (responses[i] == null || responses[i].MsgType == NetData.MessageType.Invalid)
                    {
                        drops++;
                        Debug.LogError($"Attempt {attempts} timed out or dropped ({drops}) - [{i}] - {responses[i]?.ToString()}");
                    }
                    else
                    {
                        if (minRtt == -1 || roundTripTime < minRtt)
                        {
                            minRtt = roundTripTime;
                            minRttIndex = i;
                        }
                    }
                }
                if (drops < 0.6 * Net.NumberOfTimeSyncChecks)
                {
                    valid = true;
                }
                else if (drops >= Net.NumberOfTimeSyncChecks)
                {
                    Debug.LogError("No responses after initial connect.");
                    break;
                }
            }
            if (valid)
            {
                TimeOffset = (responses[minRttIndex].TimeStamp - timeStamps[minRttIndex] - (long)((minRtt * 1000) / 2));
                Debug.Log($"Time Offset: {TimeOffset}");
            }
            MinRtt = minRtt;
            return valid;
        }

        private bool EstablishConnection()
        {
            int attempts = 0;
            NetData response;
            client.Client.ReceiveTimeout = Net.ConnectTimeout;
            Diagnostics.Stopwatch watch = new Diagnostics.Stopwatch();

            do
            {
                watch.Restart();
                NetData sendData = NetData.ConnectMessage;
                SendNetData(sendData);
                response = AwaitResponse();
                watch.Stop();
                if (response != null && response.MsgType != NetData.MessageType.Invalid)
                    break;
                if (watch.ElapsedMilliseconds < Net.ConnectTimeout)
                {
                    Thread.Sleep((int)(Net.ConnectTimeout - watch.ElapsedMilliseconds));
                    Debug.LogError("Retrying...");
                }
            } while (++attempts < Net.MaxConnectionAttempts);

            if (response != null && response.MsgType == NetData.MessageType.Connect)
            {
                if (!BitConverter.IsLittleEndian)
                {
                    byte[] reverse = response.RawData.Array;
                    Array.Reverse(reverse);
                    response.RawData = reverse;
                }
                Id = BitConverter.ToUInt16(response.RawData);
                Debug.Log($"Id is: {Id}");
                return true;
            }
            else
            {
                if (attempts == 0)
                {
                    Debug.LogError($"Data received: {response}");
                }
                Debug.LogError($"Connection attempt failed after {attempts} tries");
                return false;
            }
        }


        private void SendNetData(NetData data)
        {
            if (lastData != null)
                data.AckId = lastData.Tick;
            data.Tick = clientTick;

            if (data.MsgType == NetData.MessageType.Data)
            {
                if (unAckedCommands > 0)
                {
                    data.Commands = commands.Where(c => c != null).ToArray();
                }
                Net.SendNetData(data, client, serverEndPoint);
            }
            else
            {
                Net.SendNetData(data, client, serverEndPoint);
            }
        }

        private NetData AwaitResponse()
        {
            NetData result = Net.AwaitData(client, ref serverEndPoint);
            if (result.MsgType == NetData.MessageType.Invalid)
                Debug.LogError(result.ErrorMsg);
            return result;
        }

        public bool GetNextData(out NetData data)
        {
            return receivedData.TryDequeue(out data);
        }
    }
}