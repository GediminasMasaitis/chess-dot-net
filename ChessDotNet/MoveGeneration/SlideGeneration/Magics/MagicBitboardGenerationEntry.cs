using Bitboard = System.UInt64;

namespace ChessDotNet.MoveGeneration.SlideGeneration.Magics
{
    public class MagicBitboardEntry
    {
        public MagicBitboardEntry(ulong blockerMask, ulong magicNumber, byte offset, ulong[] moveboards)
        {
            BlockerMask = blockerMask;
            MagicNumber = magicNumber;
            Offset = offset;
            Moveboards = moveboards;
        }

        public Bitboard BlockerMask { get; }
        public Bitboard MagicNumber { get; }
        public byte Offset { get; }
        public Bitboard[] Moveboards { get; }
    }
}