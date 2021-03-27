using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.Hashing;

using Piece = System.Byte;

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

        private static void SyncPiecesCount(Board board)
        {
            board.PieceCounts = new int[ChessPiece.Count];
            for (var i = 0; i < 64; i++)
            {
                var piece = board.ArrayBoard[i];
                board.PieceCounts[piece]++;
            }
        }

        public static Board ToDebugRookBoard(this ulong bitBoard)
        {
            var board = new Board();
            board.ArrayBoard = new Piece[64];
            board.BitBoard = new ulong[ChessPiece.Count];
            board.BitBoard[ChessPiece.WhiteRook] = bitBoard;
            board.SyncExtraBitBoards();
            board.SyncBitBoardsToArrayBoard();
            SyncPiecesCount(board);
            board.SyncMaterial();
            board.Key = ZobristKeys.CalculateKey(board);
            return board;
        }

        public static void DumpConsole(this Board board, bool evaluate = true)
        {
            Console.WriteLine(board.Print(evaluate ? new EvaluationService() : null));
        }

        public static void DumpConsole(this ulong bitBoard)
        {
            var board = ToDebugRookBoard(bitBoard);
            board.DumpConsole(false);
        }

        public static string Print(this ulong bitBoard)
        {
            return bitBoard.ToDebugRookBoard().Print();
        }

        public static void DumpConsole(this MagicBitboardEntry generationEntry)
        {
            Console.WriteLine("Blocker mask:");
            generationEntry.BlockerMask.Dump();
        }
    }
}