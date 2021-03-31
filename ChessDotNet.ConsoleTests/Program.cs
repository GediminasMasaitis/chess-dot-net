﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.Evaluation.V2;
using ChessDotNet.Fen;
using ChessDotNet.Hashing;
using ChessDotNet.Init;
using ChessDotNet.MoveGeneration;
using ChessDotNet.MoveGeneration.SlideGeneration;
using ChessDotNet.Perft;
using ChessDotNet.Perft.External;
using ChessDotNet.Perft.Suite;
using ChessDotNet.Protocols;
using ChessDotNet.Search2;
using ChessDotNet.Searching;
using ChessDotNet.Testing;

namespace ChessDotNet.ConsoleTests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Init();
            //WritePieces();
            //foreach (var file in BitboardConstants.Files)
            //{
            //    Console.WriteLine($"0x{file:X}ULL,");
            //}
            //Console.WriteLine();
            //foreach (var rank in BitboardConstants.Ranks)
            //{
            //    Console.WriteLine($"0x{rank:X}ULL,");
            //}

            //DoMagicBitboards();

            //DoTimings();


            //TestMove();
            //TestZobrist();
            //TestRepetitions();

            //DoPerftClient();
            DoPerft();
            //DoPerftSuite();
            //DoSearch2Async();
            //TestLoadState();
            //TestInvalid();
            //DoSpeedTest();
            //TestSee();
            //TestEval2();

            //Console.WriteLine(new BoardFactory().ParseFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1").Print());
            //var pos = 27;
            //Debugging.ShowBitBoard(EvaluationService.PassedPawnMasksWhite[pos], EvaluationService.PassedPawnMasksBlack[pos], EvaluationService.IsolatedPawnMasks[pos]);
            //DoEvaluate();
            //DoSlideTest();

            //Thread.Sleep(TimeSpan.FromDays(1));
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static void TestLoadState()
        {
            var savedState = State.LoadState("state.json");
            var board = savedState.Board;
            var state = savedState.State;

            Console.WriteLine(board.Print(new EvaluationService2(new EvaluationData()), new FenSerializerService()));
        }


        public static void TestInvalid()
        {
            var factory = new BoardFactory();
            var board = factory.ParseMoves("d2d4 d7d5 c1f4 c8f5 e2e3 e7e6 f1b5 c7c6 b5d3 d8b6 b2b3 f5d3 d1d3 b8a6 g1f3 a6b4 d3e2 b6b5 c2c4 d5c4 b3c4 b5f5 e1f1 e8c8 f4e5 f7f6 e3e4 f5h5 e5g3 f8d6 g3d6 d8d6 c4c5 d6d7 e2c4 b4a6 c4e6 a6c7 e6b3 c7b5 d4d5 b5c7 d5d6 c7a6 e4e5 a6c5 b3e3 b7b6 b1c3 g8h6 e5f6 g7f6 e3f4 h8d8 f4f6 d7d6 f6g7 d6d3 a1b1 h5g6 g7g6 h7g6 b1c1 c8b7 h2h3 d8e8 h3h4 h6f7 h4h5 e8h8 f1e2 g6h5 h1h4 d3d6 e2f1 c5d3 c1d1 c6c5 h4e4 h8d8 e4h4 d6h6 h4e4 d8d7 e4f4 h6h7 a2a3 a7a6 d1d2 b6b5 c3e4 c5c4 e4f6 h7h6 f3e1 f7g5 e1d3 d7d3 d2d3 c4d3 f4f5 g5e6 f6e4 b7c6 f5e5 h5h4 f1e1 h4h3 g2h3 e6f4 e5c5 c6b6 c5f5 f4e2 e1d2 h6h3 f5f6 b6c7 e4g5 h3h5 g5e6 c7d7 e6f8 d7e7 f6f3 h5g5 f8h7 g5g4 f3e3 e7f7 e3d3 e2d4 h7g5 f7f6 g5f3 d4e6 d3d6 g4a4 d6d3 e6c5 d3e3 a6a5 d2e2 b5b4 a3b4 a4a2 e2f1 a5b4 f3d4 a2b2 f2f4 b4b3 e3c3 c5e4 c3c6 f6f7 c6c7 f7g6 f1g1 b2b1 g1g2 b3b2 c7b7 b1d1 f4f5 g6h5 b7b2 d1d4 g2f3 e4d6 b2g2 d6f5 g2g1 d4b4 g1g8 f5h4 f3e3 h4g6 g8d8 g6e7 e3d3 h5g5 d3c3 b4b7 d8e8 g5f4 c3d3 e7c6 e8a8 c6e5 d3d4 b7d7 d4c5 f4e4 a8a4 e4f5 a4h4 e5d3 c5c4 f5e5 c4c3 d3f4 h4h8 d7d1 h8e8 f4e6 e8a8 e5e4 a8e8 e4d5 e8h8 d1g1 h8b8 g1f1 b8h8 d5e4 h8e8 e4e5 e8h8 f1g1 h8h5 e5e4 h5h8 g1c1");
            Console.WriteLine(board.Print(new EvaluationService2(new EvaluationData()), new FenSerializerService()));
        }

        public static void TestEval2()
        {
            //var board = MakeBoard("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            var board = MakeBoard("r3r1kb/p2bp2p/1q1p1npB/5NQ1/2p1P1P1/2N2P2/PPP5/2KR3R w - - 0 1");
            var eval2 = new EvaluationService2(new EvaluationData());
            var score = eval2.Evaluate(board);

        }

        public static void WritePieces()
        {
            Console.WriteLine($"{nameof(ChessPiece.White)}: {ChessPiece.White}");
            Console.WriteLine($"{nameof(ChessPiece.Black)}: {ChessPiece.Black}");
            Console.WriteLine($"{nameof(ChessPiece.Empty)}: {ChessPiece.Empty}");
            Console.WriteLine($"{nameof(ChessPiece.Pawn)}: {ChessPiece.Pawn}");
            Console.WriteLine($"{nameof(ChessPiece.Knight)}: {ChessPiece.Knight}");
            Console.WriteLine($"{nameof(ChessPiece.Bishop)}: {ChessPiece.Bishop}");
            Console.WriteLine($"{nameof(ChessPiece.Rook)}: {ChessPiece.Rook}");
            Console.WriteLine($"{nameof(ChessPiece.Queen)}: {ChessPiece.Queen}");
            Console.WriteLine($"{nameof(ChessPiece.King)}: {ChessPiece.King}");
            Console.WriteLine($"{nameof(ChessPiece.WhitePawn)}: {ChessPiece.WhitePawn}");
            Console.WriteLine($"{nameof(ChessPiece.WhiteKnight)}: {ChessPiece.WhiteKnight}");
            Console.WriteLine($"{nameof(ChessPiece.WhiteBishop)}: {ChessPiece.WhiteBishop}");
            Console.WriteLine($"{nameof(ChessPiece.WhiteRook)}: {ChessPiece.WhiteRook}");
            Console.WriteLine($"{nameof(ChessPiece.WhiteQueen)}: {ChessPiece.WhiteQueen}");
            Console.WriteLine($"{nameof(ChessPiece.WhiteKing)}: {ChessPiece.WhiteKing}");
            Console.WriteLine($"{nameof(ChessPiece.BlackPawn)}: {ChessPiece.BlackPawn}");
            Console.WriteLine($"{nameof(ChessPiece.BlackKnight)}: {ChessPiece.BlackKnight}");
            Console.WriteLine($"{nameof(ChessPiece.BlackBishop)}: {ChessPiece.BlackBishop}");
            Console.WriteLine($"{nameof(ChessPiece.BlackRook)}: {ChessPiece.BlackRook}");
            Console.WriteLine($"{nameof(ChessPiece.BlackQueen)}: {ChessPiece.BlackQueen}");
            Console.WriteLine($"{nameof(ChessPiece.BlackKing)}: {ChessPiece.BlackKing}");
            Console.WriteLine($"{nameof(ChessPiece.Count)}: {ChessPiece.Count}");
        }

        public static Board MakeBoard(string fen)
        {
            var boardFactory = new BoardFactory();
            var board = boardFactory.ParseFEN(fen);
            Console.WriteLine(board.Print(new EvaluationService(), new FenSerializerService()));
            return board;
        }

        public static void TestSee()
        {
            var board = MakeBoard("k7/8/2b5/3p4/4b3/5B2/6B1/K7 w - - 0 1");

            var slidingMoveGenerator = new MagicBitboardsService();
            var attacksService = new AttacksService(slidingMoveGenerator);
            var seeService = new SeeService(attacksService);

            var from = ChessPosition.F3;
            var to = ChessPosition.E4;
            var move = new Move(from, to, board.ArrayBoard[from], board.ArrayBoard[to]);
            //board.DoMove2(move);
            Console.WriteLine(1UL << move.From);
            Console.WriteLine(1UL << move.To);
            Console.WriteLine(board.Print());
            var score = seeService.See(board, move);
            Console.WriteLine(score);
        }

        public static void DoSlideTest()
        {
            var board = MakeBoard("8/8/3k2bp/8/8/3K4/8/8 w - - 0 1");
            
            var bishops = board.BitBoard[ChessPiece.BlackBishop];
            var bishopPos = bishops.BitScanForward();

            var kings = board.BitBoard[ChessPiece.WhiteKing];
            var kingPos = kings.BitScanForward();

            var slidingMoveGenerator = new MagicBitboardsService();
            var result = slidingMoveGenerator.DiagonalAntidiagonalSlide(board.AllPieces, kingPos);
            var a = 123;

        }

        public static void DoPerftClient()
        {
            var slidingMoveGenerator = new MagicBitboardsService();
            var attacksService = new AttacksService(slidingMoveGenerator);
            var movesService = new PossibleMovesService(attacksService, slidingMoveGenerator);
            var boardFactory = new BoardFactory();
            using var testClient = new InternalPerftClient(movesService, boardFactory);
            //using var verificationClient = new SharperPerftClient(@"C:\Chess\Engines\Sharper\Sharper.exe");
            using var verificationClient = new StockfishPerftClient(@"C:\Chess\Engines\stockfish_13_win_x64_avx2\stockfish_13_win_x64_avx2.exe");
        }

        private static void DoPerft()
        {
            var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            //var fen = "8/8/3k4/8/3K4/8/8/8 w - - 0 1";
            //var fen = "rnbqkbnr/2pppppp/p7/Pp6/8/8/1PPPPPPP/RNBQKBNR w KQkq b6 0 3 ";
            //var fen = "8/3R1k2/8/4B3/1K6/p6B/5p1P/8 b - - 1 67 ";

            //var fen = "8/1k6/8/2Pp3r/2K5/8/8/8 w - d6";
            //var fen = "8/8/4k3/8/2p5/8/B2P2K1/8 w - - 0 1";

            //var fen = "8/8/1k6/2b5/2pP4/8/5K2/8 b - d3 0 1";
            //fen = "8/1kP5/8/K2p3r/8/8/8/8 w - - 1 53 ";
            //fen = "r1b1k2r/ppppnppp/2n2q2/2b5/3NP3/2P1B3/PP3PPP/RN1QKB1R w KQkq - 0 1";
            //fen = "2k5/8/8/8/8/8/6p1/2K5 w - - 1 1 ";
            //fen = "rnbqkbnr/1ppppppp/8/p7/1P6/P7/2PPPPPP/RNBQKBNR b KQkq b3 0 2 ";
            var slidingMoveGenerator = new MagicBitboardsService();
            //var slidingMoveGenerator = new HyperbolaQuintessence();
            var attacksService = new AttacksService(slidingMoveGenerator);
            var movesService = new PossibleMovesService(attacksService, slidingMoveGenerator);
            var boardFactory = new BoardFactory();
            using var testClient = new InternalPerftClient(movesService, boardFactory);
            //using var verificationClient = new SharperPerftClient(@"C:\Chess\Engines\Sharper\Sharper.exe");
            using var verificationClient = new StockfishPerftClient(@"C:\Chess\Engines\stockfish_13_win_x64_avx2\stockfish_13_win_x64_avx2.exe");

            var fenSerializer = new FenSerializerService();
            var perftRunner = new PerftRunner(testClient, verificationClient, boardFactory, fenSerializer);
            perftRunner.OnOut += Console.Write;
            perftRunner.Test(fen, 7);
        }

        private static void DoSpeedTest()
        {
            var board = MakeBoard("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            var eval = new EvaluationService2(new EvaluationData());
            var scores = new EvaluationScores();
            var sw = new Stopwatch();
            sw.Start();
            var num = 100000000;
            for (var i = 0; i < num; i++)
            {
                //eval.evalPawnStructure(board, evaluationBoard);
                //eval.Evaluate(board);
                //eval.EvalRook(board, evaluationBoard, scores, 0, ChessPiece.White);
            }
            sw.Stop();
            var speed = (long)(num / sw.Elapsed.TotalSeconds);
            Console.WriteLine($"{speed.ToUserFriendly()}e/s");
        }

        private static void DoSearch2Async()
        {
            //Console.WriteLine(Marshal.SizeOf<Move>());
            //Console.WriteLine(Marshal.SizeOf<TranspositionTableEntry>());
            var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; // Starting pos
            //var fen = "8/6pp/3kp3/3P1p2/2P2P2/2R4P/7K/q7 w - -";
            //var fen = "8/6pp/3k4/3P1p2/5P2/4R2P/7K/q7 b - - 0 1";
            //var fen = "rnbqkbnr/pppppppp/8/8/8/N7/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; // Starting pos
            //var fen = "rnbqkbnr/pppppppp/7n/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; // Starting pos

            //var fen = "r4rk1/p2n1ppp/3qp3/6B1/N5P1/3P1b2/PPP1BbP1/R2Q1R1K b - - 0 14"; // Mate in 3
            //var fen = "r1b1kb1r/2pp1ppp/1np1q3/p3P3/2P5/1P6/PB1NQPPP/R3KB1R b KQkq - 0 1"; // Midgame
            //var fen = "r3r1kb/p2bp2p/1q1p1npB/5NQ1/2p1P1P1/2N2P2/PPP5/2KR3R w - - 0 1"; // Midgame 2
            var fact = new BoardFactory();
            var board = fact.ParseFEN(fen);
            //board = fact.ParseMoves("g1f3 g8f6 d2d4 d7d5 e2e3 e7e6 c2c3 c7c5 f1d3 f8d6 d4c5 d6c5 d1a4 b8c6 e1g1 e8g8 b1d2 e6e5 d3b5 d8d6 b5c6 d6c6 a4c6 b7c6 f3e5 c8a6 f1d1 a8c8 d2f3 f8e8 e5d3 c5d6 f3d4 c6c5 d4f5 d6f8 b2b3 c8b8 c1b2 a6b5 d3f4 b8d8 c3c4 d5c4 b2f6 d8d1 a1d1 c4b3 f4h5 b5e2 h5g7 f8g7 f5g7 e8b8 d1e1 b3a2 f2f4 b8b1 g1f2 e2d1 g7f5");
            //board.DoMove2(Move.FromPositionString(board, "a2a1b"));
            //board.DoMove2(Move.FromPositionString(board, "e6d5"));
            //board.DoMove2(Move.FromPositionString(board, "c4d5"));
            //board.DoMove2(Move.FromPositionString(board, "a1a2"));

            var fenSerializer = new FenSerializerService();
            var slideMoveGenerator = new MagicBitboardsService();
            //var slideMoveGenerator = new HyperbolaQuintessence();

            var evaluationService = new EvaluationService2(new EvaluationData());
            //var evaluationService = new EvaluationService();

            var attacksService = new AttacksService(slideMoveGenerator);
            var movesService = new PossibleMovesService(attacksService, slideMoveGenerator);
            var searchService = new SearchService2(movesService, evaluationService);
            //var state = State.LoadState("state-2021-03-31-10-05-17-407.json");
            //board = state.Board;
            //Console.WriteLine(board.Print(null, new FenSerializerService()));
            //searchService.SetState(state.State);
            Console.WriteLine(board.Print(evaluationService, fenSerializer));
            searchService.SearchInfo += info => Console.WriteLine(info.ToString());
            var searchParameters = new SearchParameters();
            //searchParameters.WhiteTime = 1000;
            searchParameters.Infinite = true;
            //searchParameters.MaxDepth = 9;
            EngineOptions.Debug = true;
            var stopwatch = new Stopwatch();
            var cancellationTokenSource = new CancellationTokenSource();
            //var searchTask = Task.Run(() =>
            //{
                for (int i = 0; i < 1; i++)
                {
                    stopwatch.Restart();
                    var moves = searchService.Run(board, searchParameters, cancellationTokenSource.Token);
                    stopwatch.Stop();
                    searchService.NewGame();
                    Console.WriteLine($"Total time: {stopwatch.Elapsed.TotalMilliseconds} ms");
                }
            //});
            HandleInterrupt(cancellationTokenSource);
            //await searchTask;
            var foo = 123;
        }

        private static void HandleInterrupt(CancellationTokenSource cancellationTokenSource)
        {
            var ch = Console.ReadKey();
            if (ch.KeyChar == 'x')
            {
                cancellationTokenSource.Cancel();
            }
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

        private static void DoEvaluate()
        {
            //var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            var fen = "r4rk1/p2n1ppp/3qp3/6B1/N5P1/3P1b2/PPP1BbP1/R2Q1R1K b - - 0 14";
            var boardFactory = new BoardFactory();
            var board = boardFactory.ParseFEN(fen);
            var evaluationService = new EvaluationService();
            var evaluation = evaluationService.Evaluate(board);
            Console.WriteLine(evaluation);
        }

        private static void DoPerftSuite()
        {
            var slidingMoveGenerator = new MagicBitboardsService();
            var attacksService = new AttacksService(slidingMoveGenerator);
            var movesService = new PossibleMovesService(attacksService, slidingMoveGenerator);
            var boardFactory = new BoardFactory();
            using var testClient = new InternalPerftClient(movesService, boardFactory);
            //using var verificationClient = new SharperPerftClient(@"C:\Chess\Engines\Sharper\Sharper.exe");
            using var verificationClient = new StockfishPerftClient(@"C:\Chess\Engines\stockfish_13_win_x64_avx2\stockfish_13_win_x64_avx2.exe");

            var fenSerializer = new FenSerializerService();
            var perftRunner = new PerftRunner(testClient, verificationClient, boardFactory, fenSerializer);
            perftRunner.OnOut += Console.Write;
            //var suite = new PerftSuite(perftRunner);
            //suite.Run();
            var suite = new PerftSuiteRunner(perftRunner);
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "hartmann.epd");
            suite.RunSuite(path);
        }

        private static void TestMove()
        {
            var board = MakeBoard("rnbqkbnr/pppppppp/8/8/8/3P4/PPP1PPPP/RNBQKBNR b KQkq -");
            //board.DoMove2(new Move(ChessPosition.D2, ChessPosition.D3, ChessPiece.WhitePawn));
            //board.UndoMove();
            Console.WriteLine(board.Print());
            var slideMoveGenerator = new MagicBitboardsService();
            //var evaluationService = new EvaluationService();
            //Console.WriteLine(evaluationService.Evaluate(board));

            var attacksService = new AttacksService(slideMoveGenerator);
            var movesService = new PossibleMovesService(attacksService, slideMoveGenerator);
            var forWhite = true;
            var moves = new Move[218];
            var moveCount = 0;
            movesService.GetAllPossibleMoves(board, moves, ref moveCount);
            //var dests = moves.Select(x => x.To);
            //var toMoveBoard = fact.PositionsToBitBoard(dests);
            //var attacked = attacksService.GetAllAttacked(board);

            //var newMove = new Move(4,2,ChessPiece.WhiteKing);
            //board.DoMove2(newMove);

            //Debugging.ShowBitBoard(board.BitBoard[ChessPiece.WhiteKing], board.BitBoard[ChessPiece.WhiteRook]);
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
            board.DoMove2(move);
            var keyAfterMove = ZobristKeys.CalculateKey(board);
            board.UndoMove();
            var keySameAfterMove = board.Key == keyAfterMove;

            var manualKey = board.Key;
            manualKey ^= ZobristKeys.ZPieces[8][ChessPiece.WhitePawn];
            manualKey ^= ZobristKeys.ZPieces[16][ChessPiece.WhitePawn];
            manualKey ^= ZobristKeys.ZWhiteToMove;

            Console.WriteLine(keySameAfterMove ? "Keys after move match" : "Keys after move are different");
        }
    }
}
