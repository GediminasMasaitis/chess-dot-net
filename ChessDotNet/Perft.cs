using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ChessDotNet
{
    public class Perft
    {
        public PossibleMovesService PossibleMovesService { get; set; }
        public int MaxDepth { get; set; }
        public string PathToSharper { get; set; }

        public event Action<string> OnOut;

        private void Out(string msg) => OnOut?.Invoke(msg);
        private void OutLine(string msg) => OnOut?.Invoke(msg + Environment.NewLine);

        public Perft(PossibleMovesService possibleMovesService)
        {
            PossibleMovesService = possibleMovesService;
            MaxDepth = 1;
            PathToSharper = @"C:\sharper\Sharper.exe";
        }

        public void Test(BitBoards bitBoards, bool whiteToMove)
        {
            for (var i = 1; i <= MaxDepth; i++)
            {
                OutLine("Testing with depth " + i);
                var results = TestIteration(bitBoards, whiteToMove, 1).Count();
                OutLine($"Chess.NET found {results} possible moves");
                var sharperResults = TestBySharper(null, i);
                OutLine($"Sharper found {sharperResults} possible moves");
            }

            OutLine("Tests completed!");
        }

        public int TestBySharper(IEnumerable<Move> moves, int depth)
        {
            var movesRegex = new Regex(@"Nodes: (\d+)", RegexOptions.Compiled);
            var results = QuerySharper(moves, depth);

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

        public IList<string> QuerySharper(IEnumerable<Move> moves, int depth)
        {
            var lines = new List<string>();
            var startInfo = new ProcessStartInfo(PathToSharper);
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Path.GetDirectoryName(PathToSharper);
            using (var sharper = Process.Start(startInfo))
            {
                sharper.StandardInput.WriteLine("divide " + depth);
                while (true)
                {
                    var line = sharper.StandardOutput.ReadLine();
                    lines.Add(line);

                    if (line.Contains("Moves"))
                    {
                        return lines;
                    }
                }
            }
        }

        public IEnumerable<BitBoards> TestIteration(BitBoards bitBoards, bool whiteToMove, int depth)
        {
            var moves = PossibleMovesService.GetAllPossibleMoves(bitBoards, whiteToMove);
            foreach (var move in moves)
            {
                var movedBoard = bitBoards.DoMove(move);
                if (depth >= MaxDepth)
                {
                    yield return movedBoard;
                }
                else
                {
                    foreach (var otherBoards in TestIteration(movedBoard, !whiteToMove, depth+1))
                    {
                        yield return otherBoards;
                    }
                }
            }
        }
    }
}