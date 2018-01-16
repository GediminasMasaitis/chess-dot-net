using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessDotNet.Data;

namespace ChessDotNet.Perft
{
    public class PerftRunner
    {
        public PerftService PerftService { get; set; }
        public IPerftClient Client { get; set; }
        public BoardFactory BoardFactory { get; set; }


        public event Action<string> OnOut;
        private void Out(string msg) => OnOut?.Invoke(msg);
        private void OutLine(string msg) => OnOut?.Invoke(msg + Environment.NewLine);

        public PerftRunner(PerftService perftService, IPerftClient client, BoardFactory boardFactory)
        {
            PerftService = perftService;
            Client = client;
            BoardFactory = boardFactory;
        }

        public void Test(string fen, int depth)
        {
            Client.SetBoard(fen);
            var bitBoards = BoardFactory.ParseFEN(fen);
            OutLine($"Running perft testing, up to depth {depth}. FEN: {fen}");
            OutLine(string.Empty);
            var sw = new Stopwatch();
            for (var i = 1; i <= depth; i++)
            {
                OutLine($"Testing engine with depth {i}");
                sw.Restart();
                var engineDivision = PerftService.Divide(bitBoards, i);
                var engineMoveCount = engineDivision.Sum(x => x.Nodes);
                sw.Stop();
                var speed = engineMoveCount / sw.Elapsed.TotalSeconds;
                var speedStr = GetSpeedDisplay(speed);
                OutLine($"Engine found {engineMoveCount} possible moves in {sw.Elapsed.TotalMilliseconds} miliseconds ({speedStr})");
                OutLine($"Testing client with depth {i}");
                var clientMoveCount = Client.GetMoveCount(i);
                OutLine($"Client found {clientMoveCount} possible moves");
                OutLine("");

                if (engineMoveCount != clientMoveCount)
                {
                    OutLine("Mismatch detected");
                    FindMismatch(bitBoards, i, engineDivision);
                    return;
                }
            }

            OutLine("Tests completed!");
        }

        private string GetSpeedDisplay(double speed)
        {
            if (speed < 10000)
            {
                return $"{speed:0} N/s";
            }
            speed /= 1000;
            if (speed < 10000)
            {
                return $"{speed:0} kN/s";
            }
            speed /= 1000;
            if (speed < 10000)
            {
                return $"{speed:0} MN/s";
            }
            speed /= 1000;
            return $"{speed:0} GN/s";
        }

        private void FindMismatch(Board board, int mismatchDepth, IList<MoveAndNodes> engineResults, IList<string> previousBadMoves = null)
        {
            previousBadMoves = previousBadMoves ?? new List<string>();
            var allBadMoves = previousBadMoves.Aggregate("", (c, n) => c + " " + n);
            var clientMan = Client.GetMovesAndNodes(mismatchDepth, previousBadMoves);

            var biggerCount = Math.Max(engineResults.Count, clientMan.Count);

            for (var i = 0; i < biggerCount; i++)
            {
                if (i >= engineResults.Count)
                {
                    OutLine($"Engine didn't find result {allBadMoves} {clientMan[i].Move} (index out)");
                    return;
                }
                if (i >= clientMan.Count)
                {
                    OutLine($"Engine found result {allBadMoves} {engineResults[i].Move} that it shouldn't have found (index out)");
                    return;
                }

                if (engineResults[i].Move != clientMan[i].Move)
                {
                    if (engineResults.All(x => x.Move != clientMan[i].Move))
                    {
                        OutLine($"Engine didn't find result {allBadMoves} {clientMan[i].Move}");
                    }
                    else
                    {
                        OutLine($"Engine found result {allBadMoves} {engineResults[i].Move} that it shouldn't have found");
                    }
                    OutLine("Mismatch found!");
                    return;
                }
                var ok = engineResults[i].Nodes == clientMan[i].Nodes;
                var okWord = ok ? "OK" : "WRONG";
                OutLine($"{allBadMoves} {engineResults[i].Move}; Engine: {engineResults[i].Nodes}, client: {clientMan[i].Nodes}; {okWord}");

                if (!ok)
                {
                    var badmove = engineResults[i].Move;
                    previousBadMoves.Add(badmove);

                    var boardAfterBadMove = board.DoMove(engineResults[i].EngineMove.Value);
                    var newResults = PerftService.Divide(boardAfterBadMove, mismatchDepth - 1);

                    FindMismatch(boardAfterBadMove, mismatchDepth - 1, newResults, previousBadMoves);
                    return;
                }
            }
        }

    }
}