using ChessDotNet.Data;

namespace ChessDotNet.MoveGeneration
{
    public class HyperbolaQuintessence
    {
        public ulong AllSlide(BitBoards bitboards, int position)
        {
            var hv = HorizontalVerticalSlide(bitboards, position);
            var dad = DiagonalAntidiagonalSlide(bitboards, position);
            return hv | dad;
        }

        public ulong HorizontalVerticalSlide(BitBoards bitboards, int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(bitboards, pieceBitboard, BitBoards.Ranks[position / 8]);
            var vertical = MaskedSlide(bitboards, pieceBitboard, BitBoards.Files[position % 8]);
            return horizontal | vertical;
        }

        public ulong DiagonalAntidiagonalSlide(BitBoards bitboards, int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(bitboards, pieceBitboard, BitBoards.Diagonals[position / 8 + position % 8]);
            var vertical = MaskedSlide(bitboards, pieceBitboard, BitBoards.Antidiagonals[position / 8 + 7 - position % 8]);
            return horizontal | vertical;
        }

        public ulong MaskedSlide(BitBoards bitboards, ulong pieceBitboard, ulong mask)
        {
            var left = ((bitboards.AllPieces & mask) - 2 * pieceBitboard);
            var right = ((bitboards.AllPieces & mask).Reverse() - 2 * pieceBitboard.Reverse()).Reverse();
            var both = left ^ right;
            var slide = both & mask;
            return slide;
        }
    }
}