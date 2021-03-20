using System.Collections.Generic;
using System.Text;

namespace ChessDotNet.Search2
{
    public class SearchOptions
    {
        public bool Debug { get; set; } = false;

        [Min(1)]
        [Max(2047)]
        public uint Hash { get; set; } = 16;

        public bool UseAspirationWindows { get; set; } = true;
        public bool UseTranspositionTable { get; set; } = true;
        public bool UseStaticEvaluationPruning { get; set; } = false;
        public bool UseNullMovePruning { get; set; } = true;
        public bool UseRazoring { get; set; } = true;
        public bool UseFutilityPruning { get; set; } = true;
        public bool UseLateMoveReductions { get; set; } = true;
        public bool UsePrincipalVariationSearch { get; set; } = true;
    }
}
