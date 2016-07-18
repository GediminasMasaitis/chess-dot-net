using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ChessDotNet.Data;

namespace ChessDotNet
{
    public static class Debugging
    {
        public static void ShowBitBoard(params ulong[] bitBoard)
        {
            // TODO: Clean this hack up
            var dll = Assembly.GetExecutingAssembly().Location;
            var dllPath = Path.GetDirectoryName(dll).Replace(@"\x64", string.Empty);

#if TEST
            var debugPath = "Test";
#elif DEBUG
            var debugPath = "Debug";
#else
            var debugPath = "Release";
#endif

            var exePath = dllPath + @"\..\..\..\ChessDotNet.BoardVisualizer\bin\" + debugPath + @"\ChessDotNet.BoardVisualizer.exe";

            var argsStr = bitBoard.Select(x => x.ToString()).Aggregate((c, n) => c + " " + n);

            Process.Start(exePath, argsStr);
        }

        public static void Dump(this Board board)
        {
            ShowBitBoard(board.BitBoard);
        }

        public static void Dump(this ulong bitBoard)
        {
            ShowBitBoard(bitBoard);
        }
    }
}