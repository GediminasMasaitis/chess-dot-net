using Bitboard = System.UInt64;

namespace ChessDotNet.MoveGeneration.SlideGeneration.Magics
{
    public class MagicBitboardGenerationEntry
    {
        public int Position { get; set; }
        public bool Bishop { get; set; }
        public Bitboard BlockerMask { get; set; }
        public Bitboard MagicNumber { get; set; }
        public byte BitCount { get; set; }
        public Bitboard[] Occupancies { get; set; }
        public Bitboard[] Moveboards { get; set; }
    }
}