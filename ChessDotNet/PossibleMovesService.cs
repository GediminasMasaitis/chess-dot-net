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

        public IEnumerable<Move> GetPossibleMoves(BitBoards bitBoards, bool isWhite)
        {
            var pawnMoves = GetPossiblePawnMoves(bitBoards, isWhite);
            var knightMoves = GetPossibleKnightMoves(bitBoards, isWhite);
            var bishopMoves = GetPossibleBishopMoves(bitBoards, isWhite);
            var rookMoves = GetPossibleRookMoves(bitBoards, isWhite);
            var queenMoves = GetPossibleQueenMoves(bitBoards, isWhite);
            var allMoves = pawnMoves.Concat(knightMoves).Concat(bishopMoves).Concat(rookMoves).Concat(queenMoves);
            return allMoves;
        }

        public IEnumerable<Move> GetPossiblePawnMoves(BitBoards bitBoards, bool isWhite)
        {
            return isWhite ? GetPossibleWhitePawnMoves(bitBoards) : GetPossibleBlackPawnMoves(bitBoards);
        }

        private IEnumerable<Move> GetPossibleBlackPawnMoves(BitBoards bitBoards)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Move> GetPossibleWhitePawnMoves(BitBoards bitBoards)
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

        public IEnumerable<Move> GetPossibleKnightMoves(BitBoards bitBoards, bool forWhite)
        {
            var knights = forWhite ? bitBoards.WhiteNights : bitBoards.BlackNights;
            var ownPieces = forWhite ? bitBoards.WhitePieces : bitBoards.BlackPieces;
            for (var i = 0; i < 64; i++)
            {
                if (knights.HasBit(i))
                {
                    ulong jumps;
                    if (i > BitBoards.KnightSpanPosition)
                    {
                        jumps = BitBoards.KnightSpan << (i - BitBoards.KnightSpanPosition);
                    }
                    else
                    {
                        jumps = BitBoards.KnightSpan >> (BitBoards.KnightSpanPosition - i);
                    }

                    jumps &= ~(i%8 < 4 ? BitBoards.Files[6] | BitBoards.Files[7] : BitBoards.Files[0] | BitBoards.Files[1]);
                    jumps &= ~ownPieces;

                    foreach (var move in BitmaskToMoves(jumps, i))
                    {
                        yield return move;
                    }
                }
            }
        }

        public IEnumerable<Move> GetPossibleRookMoves(BitBoards bitBoards, bool forWhite)
        {
            var rooks = forWhite ? bitBoards.WhiteRooks : bitBoards.BlackRooks;
            return GetPossibleSlidingPieceMoves(bitBoards, rooks, forWhite, HorizontalVerticalSlide);
        }

        public IEnumerable<Move> GetPossibleBishopMoves(BitBoards bitBoards, bool forWhite)
        {
            var bishops = forWhite ? bitBoards.WhiteBishops : bitBoards.BlackBishops;
            return GetPossibleSlidingPieceMoves(bitBoards, bishops, forWhite, DiagonalAntidiagonalSlide);
        }

        public IEnumerable<Move> GetPossibleQueenMoves(BitBoards bitBoards, bool forWhite)
        {
            var bishops = forWhite ? bitBoards.WhiteQueens : bitBoards.BlackQueens;
            return GetPossibleSlidingPieceMoves(bitBoards, bishops, forWhite, AllSlide);
        }

        private IEnumerable<Move> GetPossibleSlidingPieceMoves(BitBoards bitBoards, ulong slidingPieces, bool forWhite, Func<BitBoards, int, ulong> slideResolutionFunc)
        {
            var ownPieces = forWhite ? bitBoards.WhitePieces : bitBoards.BlackPieces;
            for (var i = 0; i < 64; i++)
            {
                if (slidingPieces.HasBit(i))
                {
                    var slide = slideResolutionFunc.Invoke(bitBoards, i);
                    slide &= ~ownPieces;
                    foreach (var move in BitmaskToMoves(slide, i))
                    {
                        yield return move;
                    }
                }
            }
        }

        private static IEnumerable<Move> BitmaskToMoves(ulong bitmask, int positionFrom)
        {
            for (var j = 0; j < 64; j++)
            {
                if (bitmask.HasBit(j))
                {
                    var move = new Move(positionFrom, j);
                    yield return move;
                }
            }
        }

        private ulong AllSlide(BitBoards bitboards, int position)
        {
            var hv = HorizontalVerticalSlide(bitboards, position);
            var dad = DiagonalAntidiagonalSlide(bitboards, position);
            return hv | dad;
        }

        private ulong HorizontalVerticalSlide(BitBoards bitboards, int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(bitboards, pieceBitboard, BitBoards.Ranks[position/8]);
            var vertical = MaskedSlide(bitboards, pieceBitboard, BitBoards.Files[position%8]);
            return horizontal | vertical;
        }

        private ulong DiagonalAntidiagonalSlide(BitBoards bitboards, int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(bitboards, pieceBitboard, BitBoards.Diagonals[position/8 + position%8]);
            var vertical = MaskedSlide(bitboards, pieceBitboard, BitBoards.Antidiagonals[position/8 + 7 - position%8]);
            return horizontal | vertical;
        }

        private ulong MaskedSlide(BitBoards bitboards, ulong pieceBitboard, ulong mask)
        {
            var slide = (((bitboards.FilledSquares & mask) - 2 * pieceBitboard) ^ ((bitboards.FilledSquares & mask).Reverse() - 2 * pieceBitboard.Reverse()).Reverse()) & mask;
            return slide;
        }
    }
}
