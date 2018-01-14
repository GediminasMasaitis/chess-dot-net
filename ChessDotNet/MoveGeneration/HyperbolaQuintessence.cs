using ChessDotNet.Common;
using ChessDotNet.Data;

namespace ChessDotNet.MoveGeneration
{
    public class HyperbolaQuintessence : IHyperbolaQuintessence
    {
        public ulong AllSlide(ulong allPieces, int position)
        {
            var hv = HorizontalVerticalSlide(allPieces, position);
            var dad = DiagonalAntidiagonalSlide(allPieces, position);
            return hv | dad;
        }

        public ulong HorizontalVerticalSlide(ulong allPieces, int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(allPieces, pieceBitboard, BitboardConstants.Ranks[position / 8]);
            var vertical = MaskedSlide(allPieces, pieceBitboard, BitboardConstants.Files[position % 8]);
            return horizontal | vertical;
        }

        public ulong DiagonalAntidiagonalSlide(ulong allPieces, int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(allPieces, pieceBitboard, BitboardConstants.Diagonals[position / 8 + position % 8]);
            var vertical = MaskedSlide(allPieces, pieceBitboard, BitboardConstants.Antidiagonals[position / 8 + 7 - position % 8]);
            return horizontal | vertical;
        }

        private ulong MaskedSlide(ulong allPieces, ulong pieceBitboard, ulong mask)
        {
            var left = ((allPieces & mask) - 2 * pieceBitboard);
            var right = ReverseBits(ReverseBits(allPieces & mask) - 2 * ReverseBits(pieceBitboard));
            var both = left ^ right;
            var slide = both & mask;
            return slide;
        }

        private ulong ReverseBits(ulong bitboard)
        {
            const ulong h1 = 0x5555555555555555;
            const ulong h2 = 0x3333333333333333;
            const ulong h4 = 0x0F0F0F0F0F0F0F0F;
            const ulong v1 = 0x00FF00FF00FF00FF;
            const ulong v2 = 0x0000FFFF0000FFFF;
            bitboard = ((bitboard >> 1) & h1) | ((bitboard & h1) << 1);
            bitboard = ((bitboard >> 2) & h2) | ((bitboard & h2) << 2);
            bitboard = ((bitboard >> 4) & h4) | ((bitboard & h4) << 4);
            bitboard = ((bitboard >> 8) & v1) | ((bitboard & v1) << 8);
            bitboard = ((bitboard >> 16) & v2) | ((bitboard & v2) << 16);
            bitboard = (bitboard >> 32) | (bitboard << 32);
            return bitboard;
        }
    }
}