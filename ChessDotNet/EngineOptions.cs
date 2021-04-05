using ChessDotNet.Search2;

namespace ChessDotNet
{
    public static class EngineOptions
    {
        // OUTPUT
        public static bool Debug { get; set; } = false;
        public static bool SearchInfo { get; set; } = true;

        // SEARCH
        [Min(1)]
        [Max(2047)]
        public static uint Hash { get; set; } = 16;
        public static bool UseAspirationWindows { get; set; } = false;
        public static bool UseTranspositionTable { get; set; } = true;
        public static bool UseTranspositionTableQuiessence { get; set; } = false;
        public static bool UseStaticEvaluationPruning { get; set; } = true;
        public static bool UseNullMovePruning { get; set; } = true;
        public static bool UseRazoring { get; set; } = true;
        public static bool UseFutilityPruning { get; set; } = true;
        public static bool UseLateMoveReductions { get; set; } = true;               
        public static bool UsePrincipalVariationSearch { get; set; } = true;
        public static bool UseDeltaPruning { get; set; } = true;
        public static bool UseSeePruning { get; set; } = true;
        public static bool UseSeeOrdering { get; set; } = true;

        // EVALUATION
        public static bool UseEvalHashTable { get; set; } = true;
        public static bool UsePawnHashTable { get; set; } = true;
        public static bool UseNnue { get; set; } = true;
        public static string NnueDataPath { get; set; } = "C:/Temp/nn-62ef826d1a6d.nnue";
    }
}
