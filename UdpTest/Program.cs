using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Net = UdpTest.NetHelper;


namespace UdpTest
{
    class Program
    {
        static Random r;

        static void Main(string[] args)
        {
            Console.WriteLine("Host server? (y/n) ");
            bool isServer = Console.ReadKey(true).Key == ConsoleKey.Y;
            Server server = null;
            Client client = null;

            if (isServer)
            {
                server = new Server();
                server.Start();
            }
            else
            {
                client = new Client();
                client.Connect("127.0.0.1");
            }

            r = new Random();
            
            while (true)
            {
                //RandomDebugOutput(r);
                
                if (!isServer)
                {
                    while (!Console.KeyAvailable)
                    {
                        client.Update();
                        Thread.Sleep(10);
                    }
                    ConsoleKey key = Console.ReadKey(true).Key;
                    client.HandleKeyPress(key);
                }
                else
                {        
                    server.ServerUpdate();
                }
            }
            //ConsoleKey key = ConsoleKey.Escape;
            //Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //IPAddress serverAddr = IPAddress.Parse("127.0.0.1");

            //IPEndPoint endPoint = new IPEndPoint(serverAddr, 7777);
            //do
            //{
            //    //Console.WriteLine($"Port: {endPoint.}");

            //    string text;
            //    if (key == ConsoleKey.Escape)
            //        text = "First message!";
            //    else
            //        text = key.ToString();
            //    byte[] send_buffer = Encoding.ASCII.GetBytes(text);

            //    sock.SendToAsync(send_buffer, SocketFlags.None, endPoint);
            //    //sock.SendTo(send_buffer, endPoint);


            //    key = Console.ReadKey(true).Key;
            //} while (key != ConsoleKey.Escape);
        }


        //private static void TaskTest(Object o, int i)
        //{
        //    string s = o.ToString();
        //    Debug.Log($"1 - {i}: {o} {s}");

        //    Task t = Task.Run(async () => {
        //        if (Net.FakeMaxLag > Net.FakeMinLag)
        //        {
        //            Debug.Log($"2 - {i}: {o} {s}");
        //            await Task.Delay(r.Next(Net.FakeMinLag, Net.FakeMaxLag));
        //            Debug.Log($"3 - {i}: {o} {s}");
        //        }
        //    });
        //}


        private static void RandomDebugOutput(Random r)
        {
            int randResult;
            int numberOfPoints = 0;
            string[] testMsg = new string[] {
                "TEST",
                "A LONGER TEST",
                "a less obnoxious test",
                "a tset a dyszlecix cuold write",
                "Something, something, something, dark side...",
                "How about some numbers? No?",
                "123 456 789 012 034 056"
                };


            int[] probabilities = { 10, 30, 5, 1 };
            int Sum(int n)
            {
                int sum = 0;
                for (int i = 0; i < n + 1; i++)
                {
                    sum += probabilities[i];
                }
                return sum;
            }

            randResult = r.Next(100);
            if (randResult < Sum(0))
            {
                Debug.AddPoint(numberOfPoints++, r.Next(60), r.Next(20));
            }
            else if (randResult < Sum(1))
            {
                if (numberOfPoints > 0)
                {
                    Debug.UpdatePoint(r.Next(numberOfPoints), r.Next(60), r.Next(20));
                }
            }
            else if (randResult < Sum(2))
            {
                Debug.Log(testMsg[r.Next(testMsg.Length)]);
            }
            else if (randResult <= Sum(3))
            {
                Debug.LogError(testMsg[r.Next(testMsg.Length)]);
            }

            
        }
    }
}
