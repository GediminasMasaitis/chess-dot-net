using System.Collections.Generic;
using System.Linq;
using ChessDotNet.Search2;

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
            var principalVariation = string.Join(" ", PrincipalVariation.Select(x=>x.Move.ToPositionString()));
            return $"Depth: {Depth}, SelectiveDepth: {SelectiveDepth}, Score: {Score}, MateIn: {MateIn}, NodesSearched: {NodesSearched}, Time: {Time}, PrincipalVariation: {principalVariation}";
        }

        public IList<TranspositionTableEntry> PrincipalVariation { get; set; }
    }
}