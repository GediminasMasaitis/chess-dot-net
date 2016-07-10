using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChessDotNet.Perft
{
    public class SharperPerftClient : IPerftClient
    {
        private Process Sharper { get; }
        public string FEN { get; set; }

        public SharperPerftClient(string path, string fen)
        {
            FEN = fen;
            var startInfo = new ProcessStartInfo(path);
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Path.GetDirectoryName(path);
            Sharper = Process.Start(startInfo);
        }
        
        public int GetMoveCount(int depth)
        {
            var movesRegex = new Regex(@"Nodes: (\d+)", RegexOptions.Compiled);
            var results = QuerySharper(depth);
            return GetMovesFromSharperResults(results, movesRegex);
        }

        public IList<MoveAndNodes> GetMovesAndNodes(int depth, IEnumerable<string> moves)
        {
            var sharperResults = QuerySharper(depth, moves);
            var sharperMan = FindMoveAndNodesFromSharperResults(sharperResults);
            return sharperMan;
        }

        private IList<string> QuerySharper(int depth, IEnumerable<string> commands = null)
        {
            var lines = new List<string>();

            Sharper.StandardInput.WriteLine("setboard " + FEN);
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

        private int GetMovesFromSharperResults(IList<string> results, Regex movesRegex)
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

        private IList<MoveAndNodes> FindMoveAndNodesFromSharperResults(IEnumerable<string> sharperResults)
        {
            return FindMoveAndNodesFromSharperResultsInner(sharperResults).OrderBy(x => x.Move).ToList();
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