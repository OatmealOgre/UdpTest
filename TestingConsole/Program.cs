using System;

namespace TestingConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleKey key;
            bool toggle = false;

            while (true)
            {
                
                while(!Console.KeyAvailable)
                {

                }
                key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.Escape)
                    break;
                if (key == ConsoleKey.Spacebar)
                {
                    toggle = !toggle;
                    if (toggle)
                    {
                        DoStuff();
                    }
                    else
                    {
                        UndoStuff();
                    }

                }
                
            }
            
        }

        private static void UndoStuff()
        {
            Console.WriteLine("Undo");
        }

        private static void DoStuff()
        {
            Console.WriteLine("Do");
        }
    }
}
