using System.Collections.Generic;
using System.Linq;

namespace ChessDotNet.Searching
{
    public class SearchInfo
    {
        public int Depth { get; set; }
        public int SelectiveDepth { get; set; }
        public int Score { get; set; }
        public int? MateIn { get; set; }
        public long NodesSearched { get; set; }
        public long Time { get; set; }

        public override string ToString()
        {
            return $"Depth: {Depth}, SelectiveDepth: {SelectiveDepth}, Score: {Score}, MateIn: {MateIn}, NodesSearched: {NodesSearched}, Time: {Time}, PrincipalVariation: {PrincipalVariation.Select(x=>x.Move.ToPositionString()).Aggregate((x,n) => x + " " + n)}";
        }

        public IList<PVSResult> PrincipalVariation { get; set; }
    }
}