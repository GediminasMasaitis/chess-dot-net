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
            DoTimings();
            //DoPerft();
            //TestMove();

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
            var arrayBoard = fact.ParseFENToArrayBoard("rnbqkbnr/p1pp1p1p/1p4p1/3Np3/8/5P1N/PPPPP1PP/R1BQKB1R b KQkq - 1 4 ");
            var bitBoards = fact.ArrayBoardToBitBoards(arrayBoard);
            bitBoards.EnPassantFile = BitBoards.Files[1];
            var movesService = new PossibleMovesService(bitBoards);
            var forWhite = true;
            var moves = movesService.GetAllPossibleMoves(bitBoards, forWhite).ToList();
            var dests = moves.Select(x => x.To);
            var toMoveBoard = fact.PiecesToBitBoard(dests);
            var attacked = movesService.GetAllAttacked(bitBoards, forWhite);
            Debugging.ShowBitBoard(attacked, toMoveBoard);
        }
    }
}
