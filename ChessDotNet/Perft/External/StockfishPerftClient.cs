using System.Collections.Generic;

namespace ChessDotNet.Perft.External
{
    public class StockfishPerftClient : ExternalClientBase
    {
        public StockfishPerftClient(string path) : base(path)
        {
        }

        public override void SetBoard(string fen)
        {
            Process.StandardInput.WriteLine($"position fen {fen}");
        }

        private IList<string> QuerySharper(int depth)
        {
            var lines = new List<string>();

            Process.StandardInput.WriteLine("go perft " + depth);
            while (true)
            {
                var line = Process.StandardOutput.ReadLine();
                lines.Add(line);

                if(line.Contains("Nodes searched"))
                {
                    Process.StandardOutput.ReadLine();
                    return lines;
                }
            }
        }

        protected override IEnumerable<MoveAndNodes> GetMovesAndNodesImpl(int depth)
        {
            var sharperResults = QuerySharper(depth);
            foreach (var result in sharperResults)
            {
                var split = result.Split(' ');
                if (split.Length != 2)
                {
                    continue;
                }

                var move = split[0];
                move = move.TrimEnd(':');

                var nodes = int.Parse(split[1]);
                var moveAndNodes = new MoveAndNodes(move, nodes);
                yield return moveAndNodes;
            }
        }
    }
}