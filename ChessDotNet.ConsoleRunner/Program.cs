using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Protocols;

namespace ChessDotNet.ConsoleRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Console.WriteLine("Chess.NET by Gediminas Masaitis");
            IChessProtocol protocol = null;
            while (true)
            {
                var line = Console.ReadLine();
                if (protocol == null && line == "uci")
                {
                    protocol = new UciProtocol();
                    protocol.OnOutput += Console.WriteLine;
                    protocol.OnExit += Environment.Exit;
                }
                protocol?.Input(line);
            }

            
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.WriteAllText($"C:/Chess/Crashes/{DateTime.Now:yyyy-MM-dd-hh-mm-ss-fff}", e.ExceptionObject.ToString());
        }
    }
}
