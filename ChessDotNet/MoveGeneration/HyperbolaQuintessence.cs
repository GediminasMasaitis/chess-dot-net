using ChessDotNet.Data;

namespace ChessDotNet.MoveGeneration
{
    public class HyperbolaQuintessence
    {
        public ulong AllSlide(Board bitboards, int position)
        {
            var hv = HorizontalVerticalSlide(bitboards, position);
            var dad = DiagonalAntidiagonalSlide(bitboards, position);
            return hv | dad;
        }

        public ulong HorizontalVerticalSlide(Board bitboards, int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(bitboards, pieceBitboard, Board.Ranks[position / 8]);
            var vertical = MaskedSlide(bitboards, pieceBitboard, Board.Files[position % 8]);
            return horizontal | vertical;
        }

        public ulong DiagonalAntidiagonalSlide(Board bitboards, int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(bitboards, pieceBitboard, Board.Diagonals[position / 8 + position % 8]);
            var vertical = MaskedSlide(bitboards, pieceBitboard, Board.Antidiagonals[position / 8 + 7 - position % 8]);
            return horizontal | vertical;
        }

        public ulong MaskedSlide(Board bitboards, ulong pieceBitboard, ulong mask)
        {
            var left = ((bitboards.AllPieces & mask) - 2 * pieceBitboard);
            var right = ((bitboards.AllPieces & mask).Reverse() - 2 * pieceBitboard.Reverse()).Reverse();
            var both = left ^ right;
            var slide = both & mask;
            return slide;
        }
    }
}