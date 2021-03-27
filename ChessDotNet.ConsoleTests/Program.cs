using System;
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

namespace ChessDotNet.ConsoleTests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            WritePieces();
            Init();
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
            //DoPerft();
            //DoPerftSuite();
            //await DoSearch2Async();
            //TestSee();

            //Console.WriteLine(new BoardFactory().ParseFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1").Print());
            //var pos = 27;
            //Debugging.ShowBitBoard(EvaluationService.PassedPawnMasksWhite[pos], EvaluationService.PassedPawnMasksBlack[pos], EvaluationService.IsolatedPawnMasks[pos]);
            //DoEvaluate();
            //DoSlideTest();
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static void WritePieces()
        {
            Console.WriteLine(ZobristKeys.ZWhiteToMove);
            //Console.WriteLine($"{nameof(ChessPiece.White)}: {ChessPiece.White}");
            //Console.WriteLine($"{nameof(ChessPiece.Black)}: {ChessPiece.Black}");
            //Console.WriteLine($"{nameof(ChessPiece.Empty)}: {ChessPiece.Empty}");
            //Console.WriteLine($"{nameof(ChessPiece.Pawn)}: {ChessPiece.Pawn}");
            //Console.WriteLine($"{nameof(ChessPiece.Knight)}: {ChessPiece.Knight}");
            //Console.WriteLine($"{nameof(ChessPiece.Bishop)}: {ChessPiece.Bishop}");
            //Console.WriteLine($"{nameof(ChessPiece.Rook)}: {ChessPiece.Rook}");
            //Console.WriteLine($"{nameof(ChessPiece.Queen)}: {ChessPiece.Queen}");
            //Console.WriteLine($"{nameof(ChessPiece.King)}: {ChessPiece.King}");
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

        private static async Task DoSearch2Async()
        {
            Console.WriteLine(Marshal.SizeOf<Move>());
            Console.WriteLine(Marshal.SizeOf<TranspositionTableEntry>());
            var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; // Starting pos
            //var fen = "rnbqkbnr/pppppppp/8/8/8/N7/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; // Starting pos
            //var fen = "rnbqkbnr/pppppppp/7n/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; // Starting pos

            //var fen = "r4rk1/p2n1ppp/3qp3/6B1/N5P1/3P1b2/PPP1BbP1/R2Q1R1K b - - 0 14"; // Mate in 3
            //var fen = "r1b1kb1r/2pp1ppp/1np1q3/p3P3/2P5/1P6/PB1NQPPP/R3KB1R b KQkq - 0 1"; // Midgame
            //var fen = "r3r1kb/p2bp2p/1q1p1npB/5NQ1/2p1P1P1/2N2P2/PPP5/2KR3R w - - 0 1"; // Midgame 2
            var fact = new BoardFactory();
            var board = fact.ParseFEN(fen);

            var fenSerializer = new FenSerializerService();
            var slideMoveGenerator = new MagicBitboardsService();
            //var slideMoveGenerator = new HyperbolaQuintessence();
            var evaluationService = new EvaluationService();
            var attacksService = new AttacksService(slideMoveGenerator);
            var movesService = new PossibleMovesService(attacksService, slideMoveGenerator);
            var searchService = new SearchService2(movesService, evaluationService);
            Console.WriteLine(board.Print(evaluationService, fenSerializer));
            searchService.SearchInfo += info => Console.WriteLine(info.ToString());
            var searchParameters = new SearchParameters();
            //searchParameters.WhiteTime = 1000;
            searchParameters.Infinite = true;
            //searchParameters.MaxDepth = 9;
            var options = new SearchOptions();
            options.Debug = true;
            var stopwatch = new Stopwatch();
            var cancellationTokenSource = new CancellationTokenSource();
            var searchTask = Task.Run(() =>
            {
                for (int i = 0; i < 1; i++)
                {
                    stopwatch.Restart();
                    var moves = searchService.Run(board, searchParameters, options, cancellationTokenSource.Token);
                    stopwatch.Stop();
                    searchService.NewGame();
                    Console.WriteLine($"Total time: {stopwatch.Elapsed.TotalMilliseconds} ms");
                }
            });
            HandleInterrupt(cancellationTokenSource);
            await searchTask;
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
            var fact = new BoardFactory();
            var board = fact.ParseFEN("rnbqkbnr/p1pppppp/8/8/1p6/3P4/PPPKPPPP/RNBQ1BNR w kq -");
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
