using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet
{
    public enum PieceColor
    {
        White,
        Black
    }

    public class PossibleMovesService
    {

        public IEnumerable<Move> GetPossibleWhiteMoves(BitBoards bitBoards)
        {
            var pawnMoves = GetPossibleWhitePawnMoves(bitBoards);
            var rookMoves = GetPossibleRookMoves(bitBoards, true);
            return pawnMoves.Concat(rookMoves);
        }

        public IEnumerable<Move> GetPossibleWhitePawnMoves(BitBoards bitBoards)
        {
            var takeLeft = (bitBoards.WhitePawns << 7) & bitBoards.BlackPieces & ~BitBoards.Files[7];
            var takeRight = (bitBoards.WhitePawns << 9) & bitBoards.BlackPieces & ~BitBoards.Files[0];
            var moveOne = (bitBoards.WhitePawns << 8) & bitBoards.EmptySquares;
            var moveTwo = (bitBoards.WhitePawns << 16) & bitBoards.EmptySquares & bitBoards.EmptySquares << 8 & BitBoards.Ranks[3];

            for (byte i = 0; i < 64; i++)
            {
                if (takeLeft.HasBit(i))
                {
                    var move = new Move(i-7, i);
                    yield return move;
                }

                if (takeRight.HasBit(i))
                {
                    var move = new Move(i - 9, i);
                    yield return move;
                }

                if (moveOne.HasBit(i))
                {
                    var move = new Move(i - 8, i);
                    yield return move;
                }

                if (moveTwo.HasBit(i))
                {
                    var move = new Move(i - 16, i);
                    yield return move;
                }
            }
        }

        public IEnumerable<Move> GetPossibleRookMoves(BitBoards bitBoards, bool forWhite)
        {
            var rooks = forWhite ? bitBoards.WhiteRooks : bitBoards.BlackRooks;
            var ownPieces = forWhite ? bitBoards.WhitePieces : bitBoards.BlackPieces;
            for (var i = 0; i < 64; i++)
            {
                if (rooks.HasBit(i))
                {
                    var slide = HorizontalVerticalSlide(bitBoards, i);
                    var validSlide = slide & ~ownPieces;
                    for (var j = 0; j < 64; j++)
                    {
                        if (validSlide.HasBit(j))
                        {
                            var move = new Move(i, j);
                            yield return move;
                        }
                    }
                }
            }
        }

        public ulong HorizontalVerticalSlide(BitBoards bitboards, int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(bitboards, pieceBitboard, BitBoards.Ranks[position/8]);
            var vertical = MaskedSlide(bitboards, pieceBitboard, BitBoards.Files[position%8]);
            return horizontal | vertical;
        }

        public ulong MaskedSlide(BitBoards bitboards, ulong pieceBitboard, ulong mask)
        {
            var slide = (((bitboards.FilledSquares & mask) - 2 * pieceBitboard) ^ ((bitboards.FilledSquares & mask).Reverse() - 2 * pieceBitboard.Reverse()).Reverse()) & mask;
            return slide;
        }
    }
}
