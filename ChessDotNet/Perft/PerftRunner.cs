using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChessDotNet.Perft
{
    public class PerftRunner
    {
        public Perft Perft { get; set; }
        public IPerftClient Client { get; set; }


        public event Action<string> OnOut;
        private void Out(string msg) => OnOut?.Invoke(msg);
        private void OutLine(string msg) => OnOut?.Invoke(msg + Environment.NewLine);

        public PerftRunner(Perft perft, IPerftClient client)
        {
            Perft = perft;
            Client = client;
        }

        public void Test(BitBoards bitBoards, bool whiteToMove, int depth)
        {
            OutLine($"Running perft testing, up to depth {depth}");
            OutLine(string.Empty);
            var sw = new Stopwatch();
            for (var i = 1; i <= depth; i++)
            {
                OutLine($"Testing engine with depth {i}");
                sw.Restart();
                var engineMoveCount = Perft.GetPossibleMoveCount(bitBoards, whiteToMove, i);
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
                    var engineMoves = Perft.GetPossibleMoves(bitBoards, whiteToMove, depth);
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
            var engineMan = Perft.FindMoveAndNodesFromEngineResults(engineResults);
            var clientMan = Client.GetMovesAndNodes(mismatchDepth, previousBadMoves);

            for (var i = 0; i < engineMan.Count; i++)
            {
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

                    var badEngineResults = engineResults.Where(x => x.StartsWith(badmove)).Select(x => x.Substring(5)).ToList();

                    FindMismatch(mismatchDepth - 1, badEngineResults, previousBadMoves);
                    return;
                }
            }
        }

    }
}