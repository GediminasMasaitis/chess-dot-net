using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.Hashing;
using ChessDotNet.Init;
using ChessDotNet.MoveGeneration;
using ChessDotNet.MoveGeneration.SlideGeneration;
using ChessDotNet.Perft;
using ChessDotNet.Protocols;
using ChessDotNet.Searching;

namespace ChessDotNet.ConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Init();

            //DoMagicBitboards();

            //DoTimings();
            //DoPerft();
            DoPerftSuite();
            //TestMove();
            //TestZobrist();
            //TestRepetitions();
            //DoSearch();
            //Console.WriteLine(new BoardFactory().ParseFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1").Print());
            //var pos = 27;
            //Debugging.ShowBitBoard(EvaluationService.PassedPawnMasksWhite[pos], EvaluationService.PassedPawnMasksBlack[pos], EvaluationService.IsolatedPawnMasks[pos]);

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static void Init()
        {
            BitboardConstants.Init();
            new MagicBitboardsInitializer(new HyperbolaQuintessence(), new KnownMagicNumberProvider()).Init();
        }

        private static void DoMagicBitboards()
        {
            //new BoardFactory().ParseFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1").DumpConsole();
            //new MagicBitboardsInitializer(new OtherGenerator()).Init();
            //MagicBitboards.Bishops[27].BlockerMask.DumpConsole();
            //BitboardConstants.Diagonals[3].DumpConsole();
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
            //var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            var fen = "8/8/1k6/2b5/2pP4/8/5K2/8 b - d3 0 1";
            //fen = "8/1kP5/8/K2p3r/8/8/8/8 w - - 1 53 ";
            //fen = "r1b1k2r/ppppnppp/2n2q2/2b5/3NP3/2P1B3/PP3PPP/RN1QKB1R w KQkq - 0 1";
            //fen = "2k5/8/8/8/8/8/6p1/2K5 w - - 1 1 ";
            //fen = "rnbqkbnr/1ppppppp/8/p7/1P6/P7/2PPPPPP/RNBQKBNR b KQkq b3 0 2 ";
            var fact = new BoardFactory();
            //CppInitializer.Init();
            //var hyperbola = new HyperbolaQuintessence();
            var hyperbola = new MagicBitboardsService();
            var attacksService = new AttacksService(hyperbola);
            var movesService = new PossibleMovesService(attacksService, hyperbola);
            var perft = new PerftService(movesService);
            perft.MultiThreaded = false;
            var board = fact.ParseFEN(fen);
            var results = perft.GetPossibleMoves(board, 1);
            using (var sharperClient = new SharperPerftClient(@"C:\sharper\Sharper.exe"))
            {
                var perftRunner = new PerftRunner(perft, sharperClient, fact);
                perftRunner.OnOut += Console.Write;
                perftRunner.Test(fen, 1);
            }
        }

        private static void DoPerftSuite()
        {
            var fact = new BoardFactory();
            var hyperbola = new MagicBitboardsService();
            var attacksService = new AttacksService(hyperbola);
            var movesService = new PossibleMovesService(attacksService, hyperbola);
            var perft = new PerftService(movesService);
            //perft.MultiThreaded = false;
            using(var sharperClient = new SharperPerftClient(@"C:\sharper\Sharper.exe"))
            {
                var perftRunner = new PerftRunner(perft, sharperClient, fact);
                var suite = new PerftSuite(perftRunner);
                perftRunner.OnOut += Console.Write;
                suite.Run();
            }
        }

        private static void TestMove()
        {
            var fact = new BoardFactory();
            var board = fact.ParseFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -");
            board.EnPassantFile = BitboardConstants.Files[3];
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

        private static void TestZobrist()
        {
            var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            var fact = new BoardFactory();
            var board = fact.ParseFEN(fen);

            var key = ZobristKeys.CalculateKey(board);
            var keySame = board.Key == key;
            Console.WriteLine(keySame ? "Initial keys match" : "Initial keys are different");

            var move = new Move(8, 16, ChessPiece.WhitePawn);
            var boardAfterMove = board.DoMove(move);
            var keyAfterMove = ZobristKeys.CalculateKey(boardAfterMove);
            var keySameAfterMove = boardAfterMove.Key == keyAfterMove;

            var manualKey = board.Key;
            manualKey ^= ZobristKeys.ZPieces[8, ChessPiece.WhitePawn];
            manualKey ^= ZobristKeys.ZPieces[16, ChessPiece.WhitePawn];
            manualKey ^= ZobristKeys.ZWhiteToMove;

            Console.WriteLine(keySameAfterMove ? "Keys after move match" : "Keys after move are different");
        }

        private static void TestRepetitions()
        {
            var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; // Starting pos
            //fen = "2rr3k/pp3pp1/1nnqbN1p/3pN3/2pP4/2P3Q1/PPB4P/R4RK1 w - -"; // Mate in 3
            fen = "r1b1k2r/ppppnppp/2n2q2/2b5/3NP3/2P1B3/PP3PPP/RN1QKB1R w KQkq - 0 1"; // Developed

            var fact = new BoardFactory();
            var board = fact.ParseFEN(fen);

            var hyperbola = new HyperbolaQuintessence();
            var evaluationService = new EvaluationService();
            var attacksService = new AttacksService(hyperbola);
            var movesService = new PossibleMovesService(attacksService, hyperbola);
            var interruptor = new ConsoleInterruptor();
            var searchService = new SearchService(movesService, evaluationService, interruptor);

            Console.WriteLine(searchService.IsRepetition(board));

            var move = new Move(1, 18, ChessPiece.WhiteKnight);
            board = board.DoMove(move);
            Console.WriteLine(move.ToPositionString() + " " + searchService.IsRepetition(board));

            move = new Move(57, 42, ChessPiece.BlackKnight);
            board = board.DoMove(move);
            Console.WriteLine(move.ToPositionString() + " " + searchService.IsRepetition(board));

            move = new Move(18, 1, ChessPiece.WhiteKnight);
            board = board.DoMove(move);
            Console.WriteLine(move.ToPositionString() + " " + searchService.IsRepetition(board));

            move = new Move(42, 57, ChessPiece.BlackKnight);
            board = board.DoMove(move);
            Console.WriteLine(move.ToPositionString() + " " + searchService.IsRepetition(board));

            move = new Move(1, 18, ChessPiece.WhiteKnight);
            board = board.DoMove(move);
            Console.WriteLine(move.ToPositionString() + " " + searchService.IsRepetition(board));

            move = new Move(57, 40, ChessPiece.BlackKnight);
            board = board.DoMove(move);
            Console.WriteLine(move.ToPositionString() + " " + searchService.IsRepetition(board));
        }

        private static void DoSearch()
        {
            var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; // Starting pos
            //fen = "2rr3k/pp3pp1/1nnqbN1p/3pN3/2pP4/2P3Q1/PPB4P/R4RK1 w - -"; // Mate in 3
            fen = "r1b1k2r/ppppnppp/2n2q2/2b5/3NP3/2P1B3/PP3PPP/RN1QKB1R w KQkq - 0 1"; // Developed
            fen = "r1b1kb1r/2pp1ppp/1np1q3/p3P3/2P5/1P6/PB1NQPPP/R3KB1R b KQkq - 0 1 "; // Midgame
            var fact = new BoardFactory();
            var board = fact.ParseFEN(fen);

            var hyperbola = new HyperbolaQuintessence();
            var evaluationService = new EvaluationService();
            var attacksService = new AttacksService(hyperbola);
            var movesService = new PossibleMovesService(attacksService, hyperbola);
            var interruptor = new ConsoleInterruptor();
            var searchService = new SearchService(movesService, evaluationService, interruptor);
            searchService.OnSearchInfo += info => Console.WriteLine(info.ToString());
            var sParams = new SearchParams();
            //sParams.MaxDepth = 5;
            sParams.Infinite = true;

            var move = searchService.Search(board, sParams);
        }
    }
}
