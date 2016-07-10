using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Perft;

namespace ChessDotNet.ConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //DoTimings();
            //DoPerft();
            TestMove();

            Console.ReadLine();
        }

        private static void DoTimings()
        {
            var num = 123456789159753UL;
            var sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < 100000000; i++)
            {
                //var num2 = num.HasBit(15);
                var num2 = (num & (1UL << 15)) != 0;
            }
            sw.Stop();
            Console.WriteLine(sw.Elapsed.TotalMilliseconds + " ms.");
        }

        private static void DoPerft()
        {
            var fact = new BoardFactory();
            var arrayBoard = fact.ParseFENToArrayBoard("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 ");
            var bitBoards = fact.ArrayBoardToBitBoards(arrayBoard);
            var movesService = new PossibleMovesService(bitBoards);
            var perft = new Perft.Perft(movesService);
            using (var sharperClient = new SharperPerftClient(@"C:\sharper\Sharper.exe"))
            {
                var perftRunner = new PerftRunner(perft, sharperClient);
                perftRunner.OnOut += Console.Write;
                perftRunner.Test(bitBoards, true, 5);
            }
        }

        private static void TestMove()
        {
            var fact = new BoardFactory();
            var arrayBoard = fact.ParseFENToArrayBoard("rnb1kbnr/pp1ppppp/8/q1p5/8/3P4/PPPKPPPP/RNBQ1BNR w kq - 2 3 ");
            var bitBoards = fact.ArrayBoardToBitBoards(arrayBoard);
            bitBoards.EnPassantFile = BitBoards.Files[1];
            var movesService = new PossibleMovesService(bitBoards);
            var forWhite = true;
            var moves = movesService.GetAllPossibleMoves(bitBoards, forWhite).ToList();
            var dests = moves.Select(x => x.To);
            var destBoard = fact.PiecesToBitBoard(dests);
            Debugging.ShowBitBoard(bitBoards.WhitePieces, bitBoards.BlackPieces, destBoard);
        }
    }
}
