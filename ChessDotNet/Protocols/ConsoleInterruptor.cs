using System;
using System.IO;
using System.Threading.Tasks;

namespace ChessDotNet.Protocols
{
    public class ConsoleInterruptor : IInterruptor
    {
        public bool IsRunning { get; set; }
        private Task<string> ReadTask { get; set; }

        public ConsoleInterruptor()
        {
            
        }

        public bool IsInterrupted()
        {
            return ReadTask.IsCompleted;
        }

        public void Start()
        {
            if (IsRunning)
            {
                throw new Exception("Attempt to start a started interruptor");
            }
            IsRunning = true;
            ReadTask = Task.Run(() => Console.ReadLine());
        }

        public string WaitStopAndGetResult()
        {
            IsRunning = false;
            return ReadTask.Result;
        }
    }
}
