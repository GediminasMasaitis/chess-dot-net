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
        public const ulong FileA = (1UL << 0) | (1UL << 8) | (1UL << 16) | (1UL << 24) | (1UL << 32) | (1UL << 40) | (1UL << 48) | (1UL << 56);

        public void Sync()
        {
            FilledSquares = WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKings | BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKings;
            EmptySquares = ~FilledSquares;
        }
    }
}