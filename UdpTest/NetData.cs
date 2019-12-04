using System;
using Net = UdpTest.NetHelper;
using static UdpTest.NetHelper;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.IO;

namespace UdpTest
{
    public class NetData
    {
        private static readonly ArraySegment<byte> EmptyData = new ArraySegment<byte>();
        static readonly int MessageTypeCount = Enum.GetValues(typeof(MessageType)).Length;

        static public Stopwatch watch;
        public static readonly NetData ConnectMessage;
        public readonly string ErrorMsg;

        public enum MessageType
        {
            TimeSync = 0,
            Connect = 1,
            Disconnect = 2,
            Data = 3,
            Invalid = 4,
            Denied = 5,
            Load = 6,
            FinishedLoading = 7,
        }
        public readonly MessageType MsgType;
        public byte SyncId { get; set; }
        public byte Tick { get; set; }
        public ushort AckId { get; set; }

        public long TimeStamp { get; set; }
        public ArraySegment<byte> RawData { get; set; }
        public GameState GameState { get; set; }

        public ClientCommand[] Commands { get; set; }

        static NetData()
        {
            watch = new Stopwatch();
            watch.Start();
            ConnectMessage = new NetData(MessageType.Connect) { TimeStamp = 0 };
        }

        public NetData()
        {
            TimeStamp = GetTimeStamp();
        }

        public NetData(string errorMessage)
        {
            MsgType = MessageType.Invalid;
            ErrorMsg = errorMessage;
            TimeStamp = -1;
        }

        public NetData(byte syncId) : this()
        {
            MsgType = GetMessageType(syncId);
            if (MsgType == MessageType.TimeSync)
                SyncId = syncId;
        }

        public NetData(MessageType messageType) : this()
        {
            MsgType = messageType;
        }

        /// <summary>
        /// Gets time since start in 1000 milliseconds
        /// </summary>
        /// <returns>Milliseconds * 1000</returns>
        public static long GetTimeStamp()
        {
            if (watch == null)
                Debug.LogError("Watch hasn't been inititalized!");
            else
            {
                return (long)(watch?.Elapsed.TotalMilliseconds * 1000);
            }
            return -1;
        }

        public static MessageType GetMessageType(byte data)
        {
            if (data <= NumberOfTimeSyncChecks)
            {
                return MessageType.TimeSync;
            }
            data -= NumberOfTimeSyncChecks;
            if (data < MessageTypeCount)
            {
                return (MessageType)data;
            }
            return MessageType.Invalid;
        }

        public static NetData ParseData(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            NetData result;
            using (BinaryReader reader = new BinaryReader(stream))
            {

                result = new NetData(reader.ReadByte());
                if (stream.Length > stream.Position)
                    result.Tick = reader.ReadByte();
                if (stream.Length > stream.Position)
                    result.AckId = reader.ReadUInt16();
                if (stream.Length > stream.Position)
                    result.TimeStamp = reader.ReadInt64();
                if (result.MsgType == MessageType.Data)
                {
                    if (stream.Length > stream.Position)
                        result.GameState = GameState.ReadState(reader);
                    if (stream.Length > stream.Position)
                        result.Commands = ClientCommand.ReadCommands(reader);
                }

                if (stream.Length > stream.Position)
                {
                    result.RawData = reader.ReadBytes((int)(stream.Length - stream.Position));
                }


            }
            return result;
            //int byteIndex = 0;
            //data = Net.Decrypt(data);
            //if (data == null || data.Length <= 0)
            //    return InvalidData;
            //NetData result = new NetData(GetMessageType(data[byteIndex]));
            //if (result.MsgType == MessageType.TimeSync)
            //{
            //    result.SyncId = data[byteIndex];
            //}
            //if (data.Length <= ++byteIndex)
            //    return result;
            //result.Tick = data[byteIndex];
            //if (data.Length <= ++byteIndex)
            //    return result;
            //result.TimeStamp = BitConverter.ToInt64(data, byteIndex);
            //byteIndex += sizeof(long);
            //if (data.Length <= byteIndex)
            //    return result;
            //result.RawGameState = new ArraySegment<byte>(data, byteIndex, data.Length - byteIndex);
            //return result;
        }

        public byte[] ToBytes()
        {
            //int byteIndex = 0;
            //byte[] result = new byte[sizeof(byte) + sizeof(byte) + sizeof(long) + RawGameState.Count];

            MemoryStream stream = new MemoryStream();

            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                if (MsgType == MessageType.TimeSync)
                    writer.Write(SyncId);
                else
                    writer.Write((byte)(MsgType + NumberOfTimeSyncChecks));
                writer.Write(Tick);
                writer.Write(AckId);
                writer.Write(TimeStamp);
                if (MsgType == MessageType.Data)
                {
                    if (GameState != null)
                    {
                        GameState.WriteState(writer);
                    }
                    else
                    {
                        writer.Write((byte)0);
                    }
                    ClientCommand.Write(Commands, writer);
                }

                if (RawData.Count > 0)
                    writer.Write(RawData.ToArray());

            }
            //if (MsgType == MessageType.TimeSync)
            //{
            //    result[byteIndex++] = SyncId;
            //}
            //else
            //{
            //    result[byteIndex++] = (byte)(MsgType + NumberOfTimeSyncChecks);
            //}
            //result[byteIndex++] = Tick;



            //if (!BitConverter.TryWriteBytes(new Span<byte>(result, byteIndex, sizeof(long)), TimeStamp))
            //{
            //    throw new Exception("Error converting TimeStamp to bytes!");
            //}
            //byteIndex += sizeof(long);

            //if (RawGameState.Count > 0)
            //    Buffer.BlockCopy(RawGameState.Array, 0, result, byteIndex, RawGameState.Count);





            return Net.Encrypt(stream.ToArray());
        }

        public override string ToString()
        {
            return $"Type: {MsgType}, SyncId: {SyncId}, Tick: {Tick}, TimeStamp: {TimeStamp}, RawState: {Encoding.ASCII.GetString(RawData)}";
        }
    }
}