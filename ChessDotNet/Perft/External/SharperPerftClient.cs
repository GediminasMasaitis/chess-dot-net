using System.Collections.Generic;
using System.Globalization;

namespace ChessDotNet.Perft.External
{
    public class SharperPerftClient : ExternalClientBase
    {
        public SharperPerftClient(string path) : base(path)
        {
        }

        public override void SetBoard(string fen)
        {
            Process.StandardInput.WriteLine($"setboard {fen}");
            Process.StandardInput.WriteLine("force");
        }

        private IList<string> QuerySharper(int depth)
        {
            var lines = new List<string>();

            Process.StandardInput.WriteLine("divide " + depth);
            while (true)
            {
                var line = Process.StandardOutput.ReadLine();
                lines.Add(line);

                if (line.Contains("Moves"))
                {
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
                if (move == "Moves:" || move == "Nodes:")
                {
                    continue;
                }

                move = move.ToLower(CultureInfo.InvariantCulture);

                var nodes = int.Parse(split[1]);
                var moveAndNodes = new MoveAndNodes(move, nodes);
                yield return moveAndNodes;
            }
        }
    }
}