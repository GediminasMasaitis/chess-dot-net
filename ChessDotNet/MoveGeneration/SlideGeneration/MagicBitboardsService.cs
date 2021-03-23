using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ChessDotNet.Common;
using ChessDotNet.Data;

namespace ChessDotNet.MoveGeneration.SlideGeneration
{
    public class MagicBitboardsService : ISlideMoveGenerator
    {
        public ulong AllSlide(ulong allPieces, int position)
        {
            var hv = HorizontalVerticalSlide(allPieces, position);
            var dad = DiagonalAntidiagonalSlide(allPieces, position);
            return hv | dad;
        }

        public ulong HorizontalVerticalSlide(ulong allPieces, int position)
        {
            return Foo(allPieces, position, MagicBitboards.Rooks);
        }


        public ulong DiagonalAntidiagonalSlide(ulong allPieces, int position)
        {
            return Foo(allPieces, position, MagicBitboards.Bishops);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UInt64 Foo(ulong allPieces, int position, MagicBitboardEntry[] entries)
        {
            var entry = entries[position];
            var occupancy = allPieces & entry.BlockerMask;
            var index = (occupancy * entry.MagicNumber) >> entry.Offset;
            var indexInt = (int) index;
            var moveboard = entry.Moveboards[indexInt];
            return moveboard;
        }
    }
}