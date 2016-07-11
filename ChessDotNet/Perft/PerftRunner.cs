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
            var bitBoards = BoardFactory.ParseFENToBitBoards(fen);
            OutLine($"Running perft testing, up to depth {depth}");
            OutLine(string.Empty);
            var sw = new Stopwatch();
            for (var i = 1; i <= depth; i++)
            {
                OutLine($"Testing engine with depth {i}");
                sw.Restart();
                var engineMoveCount = PerftService.GetPossibleMoveCount(bitBoards, i);
                sw.Stop();
                OutLine($"Engine found {engineMoveCount} possible moves in {sw.Elapsed.TotalMilliseconds} miliseconds");
                OutLine($"Testing client with depth {i}");
                var clientMoveCount = Client.GetMoveCount(i);
                OutLine($"Client found {clientMoveCount} possible moves");
                OutLine("");

                if (engineMoveCount != clientMoveCount)
                {
                    OutLine("Mismatch detected");
                    OutLine("Engine finding all possible moves");
                    var engineMoves = PerftService.GetPossibleMoves(bitBoards, i);
                    FindMismatch(i, engineMoves);
                    return;
                }
            }

            OutLine("Tests completed!");
        }

        private void FindMismatch(int mismatchDepth, IList<string> engineResults, IList<string> previousBadMoves = null)
        {
            previousBadMoves = previousBadMoves ?? new List<string>();
            var allBadMoves = previousBadMoves.Aggregate("", (c, n) => c + " " + n);
            var engineMan = PerftService.FindMoveAndNodesFromEngineResults(engineResults);
            var clientMan = Client.GetMovesAndNodes(mismatchDepth, previousBadMoves);

            var biggerCount = Math.Max(engineMan.Count, clientMan.Count);

            for (var i = 0; i < biggerCount; i++)
            {
                if (i >= engineMan.Count)
                {
                    OutLine($"Engine didn't find result {allBadMoves} {clientMan[i].Move} (index out)");
                    return;
                }
                if (i >= clientMan.Count)
                {
                    OutLine($"Engine found result {allBadMoves} {engineMan[i].Move} that it shouldn't have found (index out)");
                    return;
                }

                if (engineMan[i].Move != clientMan[i].Move)
                {
                    if (engineMan.All(x => x.Move != clientMan[i].Move))
                    {
                        OutLine($"Engine didn't find result {allBadMoves} {clientMan[i].Move}");
                    }
                    else
                    {
                        OutLine($"Engine found result {allBadMoves} {engineMan[i].Move} that it shouldn't have found");
                    }
                    OutLine("Mismatch found!");
                    return;
                }
                var ok = engineMan[i].Nodes == clientMan[i].Nodes;
                var okWord = ok ? "OK" : "WRONG";
                OutLine($"{allBadMoves} {engineMan[i].Move}; Engine: {engineMan[i].Nodes}, client: {clientMan[i].Nodes}; {okWord}");

                if (!ok)
                {
                    var badmove = engineMan[i].Move;
                    previousBadMoves.Add(badmove);

                    var badEngineResults = engineResults.Where(x => x.StartsWith(badmove)).Select(x => x.Substring(badmove.Length + 1)).ToList();

                    FindMismatch(mismatchDepth - 1, badEngineResults, previousBadMoves);
                    return;
                }
            }
        }

    }
}