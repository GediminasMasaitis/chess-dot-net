using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChessDotNet.Testing
{
    public class Perft : IDisposable
    {
        private Process Sharper { get; set; }
        public PossibleMovesService PossibleMovesService { get; set; }
        public string PathToSharper { get; set; }

        public event Action<string> OnOut;
        private void Out(string msg) => OnOut?.Invoke(msg);
        private void OutLine(string msg) => OnOut?.Invoke(msg + Environment.NewLine);


        public Perft(PossibleMovesService possibleMovesService)
        {
            PossibleMovesService = possibleMovesService;
            PathToSharper = @"C:\sharper\Sharper.exe";

            var startInfo = new ProcessStartInfo(PathToSharper);
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Path.GetDirectoryName(PathToSharper);
            Sharper = Process.Start(startInfo);
        }

        public void Test(BitBoards bitBoards, bool whiteToMove, int depth)
        {
            for (var i = 1; i <= depth; i++)
            {
                OutLine("Testing with depth " + i);
                var engineResults = GetEngineNodes(bitBoards, whiteToMove, i);
                OutLine($"Chess.NET found {engineResults.Count} possible moves");
                var sharperResults = GetSharperNodeCount(i);
                OutLine($"Sharper found {sharperResults} possible moves");
                OutLine("");

                if (engineResults.Count != sharperResults)
                {
                    OutLine("Mismatch detected");
                    FindMismatch(i, engineResults); 
                }
            }

            OutLine("Tests completed!");
        }

        private void FindMismatch(int mismatchDepth, IList<string> engineResults, IList<string> previousBadMoves = null)
        {
            previousBadMoves = previousBadMoves ?? new List<string>();
            var allBadMoves = previousBadMoves.Aggregate("", (c, n) => c + " " + n);
            var engineMan = FindMoveAndNodesFromEngineResults(engineResults);
            var sharperResults = QuerySharper(mismatchDepth, previousBadMoves);
            var sharperMan = FindMoveAndNodesFromSharperResults(sharperResults);

            for (var i = 0; i < engineMan.Count; i++)
            {
                if (engineMan[i].Move != sharperMan[i].Move)
                {
                    if (engineMan.All(x => x.Move != sharperMan[i].Move))
                    {
                        OutLine($"Engine didn't find result {allBadMoves} {sharperMan[i].Move}");
                    }
                    else
                    {
                        OutLine($"Engine found result {allBadMoves} {engineMan[i].Move} that it shouldn't have found");
                    }
                    OutLine("Mismatch found!");
                    return;
                }
                var ok = engineMan[i].Nodes == sharperMan[i].Nodes;
                var okWord = ok ? "OK" : "WRONG";
                OutLine($"{allBadMoves} {engineMan[i].Move}; Engine: {engineMan[i].Nodes}, sharper: {sharperMan[i].Nodes}; {okWord}");

                if (!ok)
                {
                    var badmove = engineMan[i].Move;
                    previousBadMoves.Add(badmove);

                    var badEngineResults = engineResults.Where(x => x.StartsWith(badmove)).Select(x => x.Substring(5)).ToList();

                    FindMismatch(mismatchDepth-1, badEngineResults, previousBadMoves);
                    return;
                }
            }
        }

        private IList<MoveAndNodes> FindMoveAndNodesFromSharperResults(IEnumerable<string> sharperResults)
        {
            return FindMoveAndNodesFromSharperResultsInner(sharperResults).OrderBy(x => x.Move).ToList();
        }

        private IEnumerable<MoveAndNodes> FindMoveAndNodesFromSharperResultsInner(IEnumerable<string> sharperResults)
        {
            foreach (var result in sharperResults)
            {
                var split = result.Split(' ');
                if (split.Length != 2 || split[0].Length != 4)
                {
                    continue;
                }

                var man = new MoveAndNodes(split[0], int.Parse(split[1]));
                yield return man;
            }
        }

        private IList<MoveAndNodes> FindMoveAndNodesFromEngineResults(IEnumerable<string> engineResults)
        {
            var grouped = engineResults.GroupBy(x => x.Split(' ')[0]);
            var man = grouped.Select(x => new MoveAndNodes(x.Key, x.Count()));
            return man.OrderBy(x => x.Move).ToList();
        }

        public int GetSharperNodeCount(int depth)
        {
            var movesRegex = new Regex(@"Nodes: (\d+)", RegexOptions.Compiled);
            var results = QuerySharper(depth);
            return GetMovesFromSharperResults(results, movesRegex);
        }

        private static int GetMovesFromSharperResults(IList<string> results, Regex movesRegex)
        {
            foreach (var line in results)
            {
                var match = movesRegex.Match(line);
                if (match.Success)
                {
                    var num = int.Parse(match.Groups[1].Value);
                    return num;
                }
            }
            return -1;
        }

        private IList<string> QuerySharper(int depth, IEnumerable<string> commands = null)
        {
            var lines = new List<string>();

            Sharper.StandardInput.WriteLine("new");
            Sharper.StandardInput.WriteLine("force");

            if (commands != null)
            {
                foreach (var command in commands)
                {
                    Sharper.StandardInput.WriteLine(command);
                }
            }

            Sharper.StandardInput.WriteLine("divide " + depth);
            while (true)
            {
                var line = Sharper.StandardOutput.ReadLine();
                lines.Add(line);

                if (line.Contains("Moves"))
                {
                    return lines;
                }
            }
        }

        public IList<string> GetEngineNodes(BitBoards bitBoards, bool whiteToMove, int depth)
        {
            return TestEngineInner(bitBoards, whiteToMove, depth, 1, "").ToList();
        }

        private IEnumerable<string> TestEngineInner(BitBoards bitBoards, bool whiteToMove, int depth, int currentDepth, string currentString)
        {
            var moves = PossibleMovesService.GetAllPossibleMoves(bitBoards, whiteToMove);
            foreach (var move in moves)
            {
                var moveString = currentString + (currentString.Length == 0 ? string.Empty : " ") + move.ToPositionString();
                if (currentDepth >= depth)
                {
                    yield return moveString;
                }
                else
                {
                    var movedBoard = bitBoards.DoMove(move);
                    foreach (var otherBoards in TestEngineInner(movedBoard, !whiteToMove, depth, currentDepth + 1, moveString))
                    {
                        yield return otherBoards;
                    }
                }
            }
        }

        public void Dispose()
        {
            Sharper.StandardInput.Write("quit");
            Sharper.WaitForExit(200);
            if (!Sharper.HasExited)
            {
                Sharper.Kill();
            }
            Sharper.Dispose();
        }
    }
}