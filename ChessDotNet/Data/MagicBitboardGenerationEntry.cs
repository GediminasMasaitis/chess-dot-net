using System;
using System.Collections.Generic;

using Bitboard = System.UInt64;
using Piece = System.Int32;

namespace ChessDotNet.Data
{
    public class MagicBitboardGenerationEntry
    {
        public int Position { get; set; }
        public Bitboard BlockerMask { get; set; }
        public Bitboard MagicNumber { get; set; }
        public byte BitCount { get; set; }
        public IReadOnlyList<Bitboard> Occupancies { get; set; }
        public IReadOnlyList<Bitboard> Moveboards { get; set; }
    }
}