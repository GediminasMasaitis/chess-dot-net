namespace ChessDotNet.Search2
{
    public static class SearchConstants
    {
        public const int MaxDepth = 255;
        public const int MateScore = 50000;
        public const int MateThereshold = 49000;
        public const int Inf = int.MaxValue;

        public const int EndgameMaterial = 51300;

        public const int ThreadCount = 8;
        public const int InitialDepth = 1;
    }
}