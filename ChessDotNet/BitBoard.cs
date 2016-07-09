using System;

namespace ChessDotNet
{
    public class BitBoard
    {
        public ulong WhitePawns { get; set; }
        public ulong WhiteKnights { get; set; }
        public ulong WhiteBishops { get; set; }
        public ulong WhiteRooks { get; set; }
        public ulong WhiteQueens { get; set; }
        public ulong WhiteKings { get; set; }

        public ulong BlackPawns { get; set; }
        public ulong BlackKnights { get; set; }
        public ulong BlackBishops { get; set; }
        public ulong BlackRooks { get; set; }
        public ulong BlackQueens { get; set; }
        public ulong BlackKings { get; set; }

        public ulong EmptySquares { get; set; }
        public ulong FilledSquares { get; set; }

        public const ulong AllBoard = ulong.MaxValue;

        public const ulong FileA = (1UL << 0 << 0) | (1UL << 0 << 8) | (1UL << 0 << 16) | (1UL << 0 << 24) | (1UL << 0 << 32) | (1UL << 0 << 40) | (1UL << 0 << 48) | (1UL << 0 << 56);
        public const ulong FileB = (1UL << 1 << 0) | (1UL << 1 << 8) | (1UL << 1 << 16) | (1UL << 1 << 24) | (1UL << 1 << 32) | (1UL << 1 << 40) | (1UL << 1 << 48) | (1UL << 1 << 56);
        public const ulong FileC = (1UL << 2 << 0) | (1UL << 2 << 8) | (1UL << 2 << 16) | (1UL << 2 << 24) | (1UL << 2 << 32) | (1UL << 2 << 40) | (1UL << 2 << 48) | (1UL << 2 << 56);
        public const ulong FileD = (1UL << 3 << 0) | (1UL << 3 << 8) | (1UL << 3 << 16) | (1UL << 3 << 24) | (1UL << 3 << 32) | (1UL << 3 << 40) | (1UL << 3 << 48) | (1UL << 3 << 56);
        public const ulong FileE = (1UL << 4 << 0) | (1UL << 4 << 8) | (1UL << 4 << 16) | (1UL << 4 << 24) | (1UL << 4 << 32) | (1UL << 4 << 40) | (1UL << 4 << 48) | (1UL << 4 << 56);
        public const ulong FileF = (1UL << 5 << 0) | (1UL << 5 << 8) | (1UL << 5 << 16) | (1UL << 5 << 24) | (1UL << 5 << 32) | (1UL << 5 << 40) | (1UL << 5 << 48) | (1UL << 5 << 56);
        public const ulong FileG = (1UL << 6 << 0) | (1UL << 6 << 8) | (1UL << 6 << 16) | (1UL << 6 << 24) | (1UL << 6 << 32) | (1UL << 6 << 40) | (1UL << 6 << 48) | (1UL << 6 << 56);
        public const ulong FileH = (1UL << 7 << 0) | (1UL << 7 << 8) | (1UL << 7 << 16) | (1UL << 7 << 24) | (1UL << 7 << 32) | (1UL << 7 << 40) | (1UL << 7 << 48) | (1UL << 7 << 56);
    
        public void Sync()
        {
            FilledSquares = WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKings | BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKings;
            EmptySquares = ~FilledSquares;
        }
    }
}