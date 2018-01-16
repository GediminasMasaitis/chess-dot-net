using System;
using System.Collections.Generic;

using Bitboard = System.UInt64;
using Piece = System.Int32;

namespace ChessDotNet.Data
{
    public class MagicBitboardEntry
    {
        public MagicBitboardEntry(ulong blockerMask, ulong magicNumber, byte offset, IReadOnlyList<ulong> moveboards)
        {
            BlockerMask = blockerMask;
            MagicNumber = magicNumber;
            Offset = offset;
            Moveboards = moveboards;
        }

        public Bitboard BlockerMask { get; }
        public Bitboard MagicNumber { get; }
        public byte Offset { get; }
        public IReadOnlyList<Bitboard> Moveboards { get; }
    }
}