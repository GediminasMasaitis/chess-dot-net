using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessDotNet.Data;
using ChessDotNet.Fen;

namespace ChessDotNet.Perft
{
    public class PerftRunner
    {
        private readonly IPerftClient _testClient;
        private readonly IPerftClient _verificationClient;
        private readonly BoardFactory _boardFactory;
        private readonly FenSerializerService _fenSerializer;

        public event Action<string> OnOut;

        public PerftRunner(IPerftClient testClient, IPerftClient verificationClient, BoardFactory boardFactory, FenSerializerService fenSerializer)
        {
            _testClient = testClient;
            _verificationClient = verificationClient;
            _boardFactory = boardFactory;
            _fenSerializer = fenSerializer;
        }

        private void Out(string msg) => OnOut?.Invoke(msg);
        private void OutLine(string msg = null) => OnOut?.Invoke((msg ?? string.Empty) + Environment.NewLine);

        public void Test(string fen, int depth)
        {
            if (depth == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth, null);
            }
            OutLine($"Running perft up to depth {depth} for position {fen}");
            IterativeDeepen(fen, depth);
        }

        private void IterativeDeepen(string fen, int depth)
        {
            for (var i = 1; i <= depth; i++)
            {
                var result = RunComparison(fen, i);
                if (!result.Correct)
                {
                    RunFaultyLineSearch(fen, i, result);
                    break;
                }
            }
            OutLine();
        }

        private void RunFaultyLineSearch(string fen, int depth, PerftComparisonResult faultyResult)
        {
            var faultyResults = new List<PerftComparisonResult> {faultyResult};
            var board = _boardFactory.ParseFEN(fen);
            Console.WriteLine(board.Print(null, _fenSerializer));
            while (faultyResult.CorrectMove && depth > 0)
            {
                board.DoMove2(faultyResult.PerftResult.EngineMove.Value);
                Console.WriteLine(board.Print(null, _fenSerializer));
                fen = _fenSerializer.SerializeToFen(board);
                var newBoard = _boardFactory.ParseFEN(fen);
                board.ExactlyEquals(newBoard);
                newBoard.ExactlyEquals(board);
                depth--;
                faultyResult = RunComparison(fen, depth);
                if (faultyResult.Correct)
                {
                    var faultyMovesFail = string.Join(" ", faultyResults.Select(result => result.PerftResult.Move));
                    OutLine($"Faulty line: {faultyMovesFail}");
                    throw new Exception();
                }
                faultyResults.Add(faultyResult);
            }

            var faultyMoves = string.Join(" ", faultyResults.Select(result => result.PerftResult.Move));
            OutLine($"Faulty FEN: {fen}");
            OutLine($"Faulty line: {faultyMoves}");
        }

        private class PerftComparisonResult
        {
            public bool CorrectMove { get; }
            public bool CorrectNodes { get; }
            public MoveAndNodes PerftResult { get; }
            public bool Correct => CorrectMove && CorrectNodes;

            public PerftComparisonResult(bool correctMove, bool correctNodes, MoveAndNodes perftResult = default)
            {
                CorrectMove = correctMove;
                CorrectNodes = correctNodes;
                PerftResult = perftResult;
            }
        }

        private PerftComparisonResult RunComparison(string fen, int depth)
        {
            Out($"Depth: {depth}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var testResults = GetResults(_testClient, fen, depth);
            stopwatch.Stop();
            var testNodes = testResults.Sum(entry => entry.Value.Nodes);
            Out($", Perft nodes: {testNodes}");
            var verificationResults = GetResults(_verificationClient, fen, depth);
            var verificationNodes = verificationResults.Sum(entry => entry.Value.Nodes);
            Out($", Verification nodes: {verificationNodes}");
            var speed = testNodes / stopwatch.Elapsed.TotalSeconds;
            var speedDisplay = GetSpeedDisplay(speed);
            Out($", {stopwatch.Elapsed.TotalMilliseconds} ms ({speedDisplay})");
            OutLine();
            
            foreach (var testResult in testResults.Values)
            {
                if (!verificationResults.TryGetValue(testResult.Move, out var verificationResult))
                {
                    OutLine($"Found result {testResult.Move} with {testResult.Nodes} nodes that it shouldn't have");
                    return new PerftComparisonResult(false, false, testResult);
                }

                if (testResult.Nodes != verificationResult.Nodes)
                {
                    OutLine($"Move {testResult.Move} had {testResult.Nodes} nodes, {verificationResult.Nodes} nodes expected");
                    return new PerftComparisonResult(true, false, testResult);
                }
            }

            foreach (var verificationResult in verificationResults.Values)
            {
                if (!testResults.ContainsKey(verificationResult.Move))
                {
                    OutLine($"Didn't find result {verificationResult.Move}, {verificationResult.Nodes} nodes expected");
                    return new PerftComparisonResult(false, false, verificationResult);
                }
            }

            return new PerftComparisonResult(true, true);
        }


        private IDictionary<string, MoveAndNodes> GetResults(IPerftClient client, string fen, int depth, IEnumerable<string> filter = null)
        {
            client.SetBoard(fen);
            var results = client.GetMovesAndNodes(depth);
            if (filter != null)
            {
                var filterMap = filter as HashSet<string> ?? new HashSet<string>(filter);
                results = results.Where(result => filterMap.Contains(result.Move)).ToList();
            }
            var dictionary = results.ToDictionary(result => result.Move);
            return dictionary;
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
    }
}