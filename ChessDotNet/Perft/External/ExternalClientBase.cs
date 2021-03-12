using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ChessDotNet.Perft.External
{
    public abstract class ExternalClientBase : IPerftClient
    {
        protected Process Process { get; }
        public ExternalClientBase(string path)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = path;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Path.GetDirectoryName(path);

            Process = Process.Start(startInfo);
        }

        public int GetMoveCount(int depth)
        {
            var moves = GetMovesAndNodesImpl(depth);
            var totalNodes = moves.Sum(entry => entry.Nodes);
            return totalNodes;
        }

        public IList<MoveAndNodes> GetMovesAndNodes(int depth)
        {
            var results = GetMovesAndNodesImpl(depth).ToList();
            results = results.OrderBy(x => x.Move).ToList();
            return results;
        }

        public abstract void SetBoard(string fen);
        protected abstract IEnumerable<MoveAndNodes> GetMovesAndNodesImpl(int depth);

        public void Dispose()
        {
            //Process.StandardInput.Write("quit");
            //Process.WaitForExit(200);
            //if (!Process.HasExited)
            //{
            //    Process.Kill();
            //}
            Process.Kill();
            Process.Dispose();
        }
    }
}