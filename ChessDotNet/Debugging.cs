using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ChessDotNet
{
    public static class Debugging
    {
        public static void ShowBitBoard(params ulong[] bitBoard)
        {
            // TODO: Clean this hack up
            var dll = Assembly.GetExecutingAssembly().Location;
            var dllPath = Path.GetDirectoryName(dll);
            var exePath = dllPath + @"\..\..\..\ChessDotNet.BoardVisualizer\bin\Debug\ChessDotNet.BoardVisualizer.exe";

            var argsStr = bitBoard.Select(x => x.ToString()).Aggregate((c, n) => c + " " + n);

            Process.Start(exePath, argsStr);
        }
    }
}