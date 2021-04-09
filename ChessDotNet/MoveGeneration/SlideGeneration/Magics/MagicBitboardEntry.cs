using System;

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

        public UInt64 BlockerMask { get; }
        public UInt64 MagicNumber { get; }
        public byte Offset { get; }
        public UInt64[] Moveboards { get; }
    }
}