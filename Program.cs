using Microsoft.Win32;

namespace MVP_2_0_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting server...");
            Server server = new Server();// создаем сервер

            WaitForExit();
        }

        static void WaitForExit()
        {
            if (Console.ReadLine() == "exit")
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Environment.Exit(0);
            }
            else
            {
                WaitForExit();
            }
        }
    }
}