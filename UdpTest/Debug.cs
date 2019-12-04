using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;

namespace UdpTest
{
    class Debug
    {
        private const int MaxNumberOfMessages = 15;
        private static int numberOfMessages = 0;
        private static string[] messages = new string[MaxNumberOfMessages];
        private static string pinMessage1 = " ";
        private static ConsoleColor[] colors = new ConsoleColor[MaxNumberOfMessages];
        private static int[] previousMsgLength = new int[MaxNumberOfMessages];
        private static StringBuilder progress = new StringBuilder("                    ");
        public static ConcurrentDictionary<int, Point> points = new ConcurrentDictionary<int, Point>();
        private static ConcurrentDictionary<int, Point> oldPoints = new ConcurrentDictionary<int, Point>();
        private static bool first = true;
        private const string ProgressMsg = "Running";
        private static readonly ConsoleColor[] ValidColors = Enum.GetValues(typeof(ConsoleColor)).Cast<ConsoleColor>().Where(c => c != ConsoleColor.Black).ToArray();
        private static Mutex mutex = new Mutex();
        private static int drawCount = 0;
        

        public static void Update()
        {
            UpdateProgress();
            Print();
        }

        private static void Print()
        {
            if (mutex.WaitOne(1))
            {
                drawCount = (drawCount + 1) % 25;
                Console.CursorVisible = false;
                Console.ForegroundColor = ConsoleColor.Green;
                if (first)
                {
                    Console.Clear();
                    Console.Write(ProgressMsg);
                    first = false;
                }
                else
                {

                    Console.CursorLeft = ProgressMsg.Length;
                    Console.CursorTop = 0;
                    Console.Write(progress);
                    if (drawCount == 0)
                    {
                        Console.Write(new string(' ', Console.BufferWidth - ProgressMsg.Length - progress.Length - pinMessage1.Length - 1));
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(pinMessage1);
                    }
                    Console.CursorTop++;
                    for (int i = 0; i < numberOfMessages; i++)
                    {
                        Console.ForegroundColor = colors[i];
                        Console.CursorLeft = 0;
                        Console.Write(messages[i]);
                        if (previousMsgLength[i] > messages[i].Length)
                            Console.Write(new string(' ', previousMsgLength[i] - messages[i].Length));
                        previousMsgLength[i] = messages[i].Length;
                        Console.CursorTop++;
                    }
                
                    PrintPoints();
                
                    Console.CursorLeft = 0;
                    Console.CursorTop = 0;
                    Console.CursorVisible = true;
                }

                mutex.ReleaseMutex();
            }
        }

        private static void PrintPoints()
        {
            Point old, p;
            foreach (int key in points.Keys)
            {
                Console.ForegroundColor = ValidColors[key % ValidColors.Length];
                if (!oldPoints.TryGetValue(key, out old))
                {
                    old = new Point(-1, -1);
                    oldPoints[key] = old;
                }
                    
                p = points[key];

                if (old != p)
                {
                    if (old.X >= 0 && old.Y > -MaxNumberOfMessages)
                    {
                        Console.SetCursorPosition(old.X % Console.BufferWidth,
                            (old.Y % (Console.WindowHeight - MaxNumberOfMessages - 1)) + MaxNumberOfMessages + 1);
                        Console.Write(' ');
                    }
                    Console.SetCursorPosition(p.X % Console.BufferWidth,
                            (p.Y % (Console.WindowHeight - MaxNumberOfMessages - 1)) + MaxNumberOfMessages + 1);
                    Console.Write('X');
                    oldPoints[key] = p;
                }
            }
        }

        private static void UpdateProgress()
        {
            if (progress[0] == progress[progress.Length - 1])
            {
                progress[0] = progress[0] == ' ' ? '.' : ' ';
            }
            else
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    if (progress[i] != progress[0])
                    {
                        progress[i] = progress[0];
                        break;
                    }
                }
            }
        }

        public static void PointsFromState(GameState state)
        {
            for (int i = 0; i < state.Players?.Length; i++)
            {
                if (state.Players[i] != null)
                    UpdatePoint(state.Players[i].id, state.Players[i].x, state.Players[i].y);
            }
        }


        public static void AddPoint(int key, int x, int y)
        {
            UpdatePoint(key, x, y);
        }

        public static void UpdatePoint(int key, int x, int y)
        {
            while (x < 0)
                x += Console.BufferWidth;
            while (y < 0)
                y += Math.Abs(Console.WindowHeight - MaxNumberOfMessages - 1);
            points[key] = new Point(x, y);
        }

        public static void MovePoint(int key, int x, int y)
        {
            x += points[key].X;
            y += points[key].Y;
            while (x < 0)
                x += Console.BufferWidth;
            while (y < 0)
                y += Math.Abs(Console.WindowHeight - MaxNumberOfMessages - 1);
            points[key] = new Point(x, y);
        }

        private static void Log(string str, ConsoleColor color)
        {
            mutex.WaitOne();
            if (numberOfMessages > 0 && messages[numberOfMessages - 1].StartsWith(str))
            {
                if (messages[numberOfMessages - 1].Length == str.Length)
                {
                    messages[numberOfMessages - 1] = $"{str} [2]";
                }
                else if (messages[numberOfMessages - 1].EndsWith(']'))
                {
                    int occurrences;
                    if (int.TryParse(messages[numberOfMessages - 1].Substring(str.Length + 2, messages[numberOfMessages - 1].Length - (str.Length + 2) - 1), out occurrences))
                        messages[numberOfMessages - 1] = $"{str} [{occurrences + 1}]";
                }
            }
            else if (numberOfMessages < MaxNumberOfMessages)
            {
                messages[numberOfMessages] = str;
                colors[numberOfMessages] = color;
                previousMsgLength[numberOfMessages++] = str.Length;
            }
            else
            {
                for (int i = 0; i < numberOfMessages - 1; i++)
                {
                    if (previousMsgLength[i] < messages[i + 1].Length)
                        previousMsgLength[i] = messages[i + 1].Length;
                    messages[i] = messages[i + 1];
                    colors[i] = colors[i + 1];
                }
                messages[MaxNumberOfMessages - 1] = str;
                colors[MaxNumberOfMessages - 1] = color;
                if (previousMsgLength[MaxNumberOfMessages - 1] == 0)
                    previousMsgLength[MaxNumberOfMessages - 1] = str.Length;
            }
            mutex.ReleaseMutex();
        }

        public static void Log(string str)
        {
            Log(str, ConsoleColor.Cyan);
        }

        public static void Log(object obj)
        {
            Log(obj.ToString());
        }

        public static void LogError(string str)
        {
            Log(str, ConsoleColor.Red);
        }

        public static void LogError(object obj)
        {
            LogError(obj.ToString());
        }

        public static void SetPinMessage(string str)
        {
            pinMessage1 = str;
                
        }
    }
}
