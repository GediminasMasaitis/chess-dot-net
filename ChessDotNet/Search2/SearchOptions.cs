using System.Collections.Generic;
using System.Text;

namespace ChessDotNet.Search2
{
    public class SearchOptions
    {
        public bool Debug { get; set; } = false;
        public bool SearchInfo { get; set; } = true;

        [Min(1)]
        [Max(2047)]
        public uint Hash { get; set; } = 16;

        public bool UseAspirationWindows { get; set; } = false;
        public bool UseTranspositionTable { get; set; } = true;
        public bool UseTranspositionTableQuiessence { get; set; } = false;
        public bool UseStaticEvaluationPruning { get; set; } = true;
        public bool UseNullMovePruning { get; set; } = true;
        public bool UseRazoring { get; set; } = true;
        public bool UseFutilityPruning { get; set; } = true;
        public bool UseLateMoveReductions { get; set; } = true;
        public bool UsePrincipalVariationSearch { get; set; } = true;
        public bool UseDeltaPruning { get; set; } = true;
        public bool UseSeePruning { get; set; } = true;
        public bool UseSeeOrdering { get; set; } = true;
    }
}
