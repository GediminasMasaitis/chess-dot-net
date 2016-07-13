﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.MoveGeneration;
using ChessDotNet.Perft;
using ChessDotNet.Searching;

namespace ChessDotNet.ConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //DoTimings();
            DoPerft();
            //TestMove();
            //DoSearch();

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
            var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            //fen = "3k4/3p4/8/K1P4r/8/8/8/8 b - - 0 50";
            //fen = "8/1kP5/8/K2p3r/8/8/8/8 w - - 1 53 ";
            var fact = new BoardFactory();
            var hyperbola = new HyperbolaQuintessence();
            var attacksService = new AttacksService(hyperbola);
            var movesService = new PossibleMovesService(attacksService, hyperbola);
            var perft = new PerftService(movesService);
            var results = perft.GetPossibleMoves(fact.ParseFEN(fen), 1);
            using (var sharperClient = new SharperPerftClient(@"C:\sharper\Sharper.exe", fen))
            {
                var perftRunner = new PerftRunner(perft, sharperClient, fact);
                perftRunner.OnOut += Console.Write;
                perftRunner.Test(fen, 5);
            }
        }

        private static void TestMove()
        {
            var fact = new BoardFactory();
            var board = fact.ParseFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -");
            board.EnPassantFile = Board.Files[3];
            var hyperbola = new HyperbolaQuintessence();
            var evaluationService = new EvaluationService();
            Console.WriteLine(evaluationService.Evaluate(board));
            var attacksService = new AttacksService(hyperbola);
            var movesService = new PossibleMovesService(attacksService, hyperbola);
            var forWhite = true;
            var moves = movesService.GetPossibleKingMoves(board).ToList();
            var dests = moves.Select(x => x.To);
            var toMoveBoard = fact.PiecesToBitBoard(dests);
            var attacked = attacksService.GetAllAttacked(board);

            var newMove = new Move(4,2,ChessPiece.WhiteKing);
            var movedBoard = board.DoMove(newMove);

            Debugging.ShowBitBoard(movedBoard.BitBoard[ChessPiece.WhiteKing], movedBoard.BitBoard[ChessPiece.WhiteRook]);
        }

        private static void DoSearch()
        {
            var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            //fen = "2rr3k/pp3pp1/1nnqbN1p/3pN3/2pP4/2P3Q1/PPB4P/R4RK1 w - -";
            var fact = new BoardFactory();
            var board = fact.ParseFEN(fen);

            var hyperbola = new HyperbolaQuintessence();
            var evaluationService = new EvaluationService();
            var attacksService = new AttacksService(hyperbola);
            var movesService = new PossibleMovesService(attacksService, hyperbola);
            var searchService = new SearchService(movesService, evaluationService);

            var move = searchService.Search(board, 6);
        }
    }
}
