using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ChessDotNet
{
    public static class Debugging
    {
        public static void ShowBitBoard(ulong bitBoard)
        {
            // TODO: Clean this hack up
            var dll = Assembly.GetExecutingAssembly().Location;
            var dllPath = Path.GetDirectoryName(dll);
            var exePath = dllPath + @"\..\..\..\ChessDotNet.BoardVisualizer\bin\Debug\ChessDotNet.BoardVisualizer.exe";
            
            Process.Start(exePath, bitBoard.ToString());
        }
    }
}