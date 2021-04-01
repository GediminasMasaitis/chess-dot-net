namespace ChessDotNet.Search2
{
    public static class SearchConstants
    {
        public const int MaxDepth = 32;

        public const int MateScore = 30000;
        public const int MateThereshold = 29000;
        public const int Inf = short.MaxValue;

        public const int EndgameMaterial = 1300;

        public const bool Multithreading = false;
        public const int ThreadCount = Multithreading ? 8 : 1;
        public const int InitialDepth = 1;
    }
}