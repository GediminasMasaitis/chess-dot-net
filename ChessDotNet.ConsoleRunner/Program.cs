using System;
using System.Collections.Generic;
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
            Console.WriteLine("Chess.NET by Gediminas Masaitis");
            IChessProtocol protocol = null;
            var interruptor = new ConsoleInterruptor();
            while (true)
            {
                string line;
                if (interruptor.IsRunning)
                {
                    line = interruptor.WaitStopAndGetResult();
                }
                else
                {
                    line = Console.ReadLine();
                }
                if (protocol == null && line == "uci")
                {
                    protocol = new UCIProtocol(interruptor);
                    protocol.OnOutput += Console.WriteLine;
                    protocol.OnExit += Environment.Exit;
                }
                protocol?.Input(line);
            }
        }
    }
}
