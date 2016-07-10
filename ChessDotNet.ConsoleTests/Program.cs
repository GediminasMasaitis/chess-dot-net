using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
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
            var num = 123456UL;

            var sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < 1000000; i++)
            {
                for (var j = 0; j < 64; j++)
                {
                    if ((num & (1UL << j)) > 0)
                    {
                        var a = 123;
                    }
                }
                /*foreach (var pos in num.GetOnes())
                {
                    var a = 123;
                }*/
            }

            sw.Stop();
            Console.WriteLine(sw.Elapsed.TotalMilliseconds + " ms.");
        }

        private static void DoPerft()
        {
            var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 ";
            fen = "3k4/3p4/8/K1P4r/8/8/8/8 b - - 0 50";
            var fact = new BoardFactory();
            var movesService = new PossibleMovesService();
            var perft = new Perft.Perft(movesService);
            using (var sharperClient = new SharperPerftClient(@"C:\sharper\Sharper.exe", fen))
            {
                var perftRunner = new PerftRunner(perft, sharperClient, fact);
                perftRunner.OnOut += Console.Write;
                perftRunner.Test(fen, false, 5);
            }
        }

        private static void TestMove()
        {
            var fact = new BoardFactory();
            var arrayBoard = fact.ParseFENToArrayBoard("3k4/8/8/K1Pp3r/8/8/8/8 w - d6 0 51 ");
            var bitBoards = fact.ArrayBoardToBitBoards(arrayBoard);
            bitBoards.EnPassantFile = BitBoards.Files[1];
            var movesService = new PossibleMovesService();
            var forWhite = true;
            var moves = movesService.GetAllPossibleMoves(bitBoards, forWhite).ToList();
            var dests = moves.Select(x => x.To);
            var toMoveBoard = fact.PiecesToBitBoard(dests);
            var attacked = movesService.GetAllAttacked(bitBoards, forWhite);
            Debugging.ShowBitBoard(bitBoards.WhitePieces, bitBoards.BlackPieces, toMoveBoard);
        }
    }
}
