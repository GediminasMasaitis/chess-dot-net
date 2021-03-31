using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ChessDotNet.Testing
{
    static class Dump
    {
        public static void CreateDump()
        {
            int processId;
            using (var currentProcess = Process.GetCurrentProcess())
            {
                processId = currentProcess.Id;
            }

            var startInfo = new ProcessStartInfo("C:\\Portable\\Procdump\\delayed.bat");
            startInfo.Arguments = $"-accepteula -ma {processId}";
            Process.Start(startInfo);
            Console.WriteLine("Going to sleep for dumping");
            Thread.Sleep(TimeSpan.FromDays(1));
        }
    }
}
