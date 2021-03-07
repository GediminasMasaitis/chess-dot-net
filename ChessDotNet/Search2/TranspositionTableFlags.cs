using System;

namespace ChessDotNet.Search2
{
    public static class TranspositionTableFlags
    {
        public const Byte None = 0;
        public const Byte Alpha = 1;
        public const Byte Beta = 2;
        public const Byte Exact = 3;
    }
}