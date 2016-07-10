using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet
{
    public class PossibleMovesService
    {
        public BitBoards BitBoards { get; set; }

        private ulong? AttackedByWhiteInner { get; set; }
        private ulong? AttackedByBlackInner { get; set; }

        public ulong AttackedByWhite
        {
            get
            {
                if (!AttackedByWhiteInner.HasValue)
                {
                    AttackedByWhiteInner = GetAllAttacked(true);
                }
                return AttackedByWhiteInner.Value;
            }
        }

        public ulong AttackedByBlack
        {
            get
            {
                if (!AttackedByBlackInner.HasValue)
                {
                    AttackedByBlackInner = GetAllAttacked(false);
                }
                return AttackedByBlackInner.Value;
            }
        }

        public PossibleMovesService(BitBoards bitBoards)
        {
            this.BitBoards = bitBoards;
        }

        public void BitBoardsChanged()
        {
            AttackedByWhiteInner = null;
            AttackedByBlackInner = null;
        }

        public IEnumerable<Move> GetAllPossibleMoves(bool isWhite)
        {
            var pawnMoves = GetPossiblePawnMoves(isWhite);
            var knightMoves = GetPossibleKnightMoves(isWhite);
            var bishopMoves = GetPossibleBishopMoves(isWhite);
            var rookMoves = GetPossibleRookMoves(isWhite);
            var queenMoves = GetPossibleQueenMoves(isWhite);
            var kingMoves = GetPossibleKingMoves(isWhite);
            var allMoves = pawnMoves.Concat(knightMoves).Concat(bishopMoves).Concat(rookMoves).Concat(queenMoves).Concat(kingMoves);
            return allMoves;
        }

        public IEnumerable<Move> GetPossiblePawnMoves(bool isWhite)
        {
            return isWhite ? GetPossibleWhitePawnMoves() : GetPossibleBlackPawnMoves();
        }

        private IEnumerable<Move> GetPossibleWhitePawnMoves()
        {
            var takeLeft = (BitBoards.WhitePawns << 7) & ~BitBoards.Files[7] & (BitBoards.BlackPieces | (BitBoards.EnPassantFile & BitBoards.BlackPawns << 8));
            var takeRight = (BitBoards.WhitePawns << 9) & ~BitBoards.Files[0] & (BitBoards.BlackPieces | (BitBoards.EnPassantFile & BitBoards.BlackPawns << 8));
            var moveOne = (BitBoards.WhitePawns << 8) & BitBoards.EmptySquares;
            var moveTwo = (BitBoards.WhitePawns << 16) & BitBoards.EmptySquares & BitBoards.EmptySquares << 8 & BitBoards.Ranks[3];

            for (byte i = 0; i < 64; i++)
            {
                if (takeLeft.HasBit(i))
                {
                    var move = new Move(i - 7, i, ChessPiece.WhitePawn);
                    yield return move;
                }

                if (takeRight.HasBit(i))
                {
                    var move = new Move(i - 9, i, ChessPiece.WhitePawn);
                    yield return move;
                }

                if (moveOne.HasBit(i))
                {
                    var move = new Move(i - 8, i, ChessPiece.WhitePawn);
                    yield return move;
                }

                if (moveTwo.HasBit(i))
                {
                    var move = new Move(i - 16, i, ChessPiece.WhitePawn);
                    yield return move;
                }
            }
        }

        private IEnumerable<Move> GetPossibleBlackPawnMoves()
        {
            var takeLeft = (BitBoards.BlackPawns >> 7) & ~BitBoards.Files[0] & (BitBoards.WhitePieces | (BitBoards.EnPassantFile & BitBoards.WhitePawns >> 8));
            var takeRight = (BitBoards.BlackPawns >> 9) & ~BitBoards.Files[7] & (BitBoards.WhitePieces | (BitBoards.EnPassantFile & BitBoards.WhitePawns >> 8));
            var moveOne = (BitBoards.BlackPawns >> 8) & BitBoards.EmptySquares;
            var moveTwo = (BitBoards.BlackPawns >> 16) & BitBoards.EmptySquares & BitBoards.EmptySquares >> 8 & BitBoards.Ranks[4];

            for (byte i = 0; i < 64; i++)
            {
                if (takeLeft.HasBit(i))
                {
                    var move = new Move(i + 7, i, ChessPiece.BlackPawn);
                    yield return move;
                }

                if (takeRight.HasBit(i))
                {
                    var move = new Move(i + 9, i, ChessPiece.BlackPawn);
                    yield return move;
                }

                if (moveOne.HasBit(i))
                {
                    var move = new Move(i + 8, i, ChessPiece.BlackPawn);
                    yield return move;
                }

                if (moveTwo.HasBit(i))
                {
                    var move = new Move(i + 16, i, ChessPiece.BlackPawn);
                    yield return move;
                }
            }
        }

        public IEnumerable<Move> GetPossibleKingMoves(bool forWhite)
        {
            var kings = forWhite ? BitBoards.WhiteKings : BitBoards.BlackKings;
            var chessPiece = forWhite ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
            var attackedSquares = forWhite ? AttackedByBlack : AttackedByWhite;
            return GetPossibleJumpingMoves(kings, BitBoards.KingSpan, BitBoards.KingSpanPosition, forWhite, chessPiece, ~attackedSquares);
        }

        public IEnumerable<Move> GetPossibleKnightMoves(bool forWhite)
        {
            var knights = forWhite ? BitBoards.WhiteNights : BitBoards.BlackNights;
            var chessPiece = forWhite ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight;
            return GetPossibleJumpingMoves(knights, BitBoards.KnightSpan, BitBoards.KnightSpanPosition, forWhite, chessPiece, ~0UL);
        }

        private IEnumerable<Move> GetPossibleJumpingMoves(ulong jumpingPieces, ulong jumpMask, int jumpMaskCenter, bool forWhite, ChessPiece piece, ulong safeJumpMask)
        {
            var ownPieces = forWhite ? BitBoards.WhitePieces : BitBoards.BlackPieces;
            for (var i = 0; i < 64; i++)
            {
                if (jumpingPieces.HasBit(i))
                {
                    ulong jumps;
                    if (i > jumpMaskCenter)
                    {
                        jumps = jumpMask << (i - jumpMaskCenter);
                    }
                    else
                    {
                        jumps = jumpMask >> (jumpMaskCenter - i);
                    }

                    jumps &= ~(i%8 < 4 ? BitBoards.Files[6] | BitBoards.Files[7] : BitBoards.Files[0] | BitBoards.Files[1]);
                    jumps &= ~ownPieces;
                    jumps &= safeJumpMask;

                    foreach (var move in BitmaskToMoves(jumps, i, piece))
                    {
                        yield return move;
                    }
                }
            }
        }

        public IEnumerable<Move> GetPossibleRookMoves(bool forWhite)
        {
            var rooks = forWhite ? BitBoards.WhiteRooks : BitBoards.BlackRooks;
            var chessPiece = forWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook;
            return GetPossibleSlidingPieceMoves(rooks, HorizontalVerticalSlide, forWhite, chessPiece);
        }

        public IEnumerable<Move> GetPossibleBishopMoves(bool forWhite)
        {
            var bishops = forWhite ? BitBoards.WhiteBishops : BitBoards.BlackBishops;
            var chessPiece = forWhite ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop;
            return GetPossibleSlidingPieceMoves(bishops, DiagonalAntidiagonalSlide, forWhite, chessPiece);
        }

        public IEnumerable<Move> GetPossibleQueenMoves(bool forWhite)
        {
            var bishops = forWhite ? BitBoards.WhiteQueens : BitBoards.BlackQueens;
            var chessPiece = forWhite ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
            return GetPossibleSlidingPieceMoves(bishops, AllSlide, forWhite, chessPiece);
        }

        private IEnumerable<Move> GetPossibleSlidingPieceMoves(ulong slidingPieces, Func<int, ulong> slideResolutionFunc, bool forWhite, ChessPiece piece)
        {
            var ownPieces = forWhite ? BitBoards.WhitePieces : BitBoards.BlackPieces;
            for (var i = 0; i < 64; i++)
            {
                if (slidingPieces.HasBit(i))
                {
                    var slide = slideResolutionFunc.Invoke(i);
                    slide &= ~ownPieces;
                    foreach (var move in BitmaskToMoves(slide, i, piece))
                    {
                        yield return move;
                    }
                }
            }
        }

        private static IEnumerable<Move> BitmaskToMoves(ulong bitmask, int positionFrom, ChessPiece piece)
        {
            for (var j = 0; j < 64; j++)
            {
                if (bitmask.HasBit(j))
                {
                    var move = new Move(positionFrom, j, piece);
                    yield return move;
                }
            }
        }

        public ulong GetAllAttacked(bool forWhite)
        {
            var pawnsAttack = GetAttackedByPawns(forWhite);
            var knightsAttack = GetAttackedByKnights(forWhite);
            var bishopsAttack = GetAttackedByBishops(forWhite);
            var rooksAttack = GetAttackedByRooks(forWhite);
            var queensAttack = GetAttackedByQueens(forWhite);
            var kingsAttack = GetAttackedByKings(forWhite);

            var allAttacked = pawnsAttack | knightsAttack | bishopsAttack | rooksAttack | queensAttack | kingsAttack;
            return allAttacked;
        }

        public ulong GetAttackedByBishops(bool forWhite)
        {
            var bishops = forWhite ? BitBoards.WhiteBishops : BitBoards.BlackBishops;
            return GetAttackedBySlidingPieces(bishops, DiagonalAntidiagonalSlide);
        }

        public ulong GetAttackedByRooks(bool forWhite)
        {
            var rooks = forWhite ? BitBoards.WhiteRooks : BitBoards.BlackRooks;
            return GetAttackedBySlidingPieces(rooks, HorizontalVerticalSlide);
        }

        public ulong GetAttackedByQueens(bool forWhite)
        {
            var queens = forWhite ? BitBoards.WhiteQueens : BitBoards.BlackQueens;
            return GetAttackedBySlidingPieces(queens, AllSlide);
        }

        private ulong GetAttackedBySlidingPieces(ulong slidingPieces, Func<int, ulong> slideResolutionFunc)
        {
            var allSlide = 0UL;
            for (var i = 0; i < 64; i++)
            {
                if (slidingPieces.HasBit(i))
                {
                    var slide = slideResolutionFunc.Invoke(i);
                    allSlide |= slide;
                }
            }
            return allSlide;
        }

        public ulong GetAttackedByKings(bool forWhite)
        {
            var kings = forWhite ? BitBoards.WhiteKings : BitBoards.BlackKings;
            return GetAttackedByJumpingPieces(kings, BitBoards.KingSpan, BitBoards.KingSpanPosition);
        }

        public ulong GetAttackedByKnights(bool forWhite)
        {
            var knights = forWhite ? BitBoards.WhiteNights : BitBoards.BlackNights;
            return GetAttackedByJumpingPieces(knights, BitBoards.KnightSpan, BitBoards.KnightSpanPosition);
        }

        public ulong GetAttackedByPawns(bool forWhite)
        {
            ulong pawnsLeft;
            ulong pawnsRight;
            if (forWhite)
            {
                pawnsLeft = (BitBoards.WhitePawns << 7) & ~BitBoards.Files[7];
                pawnsRight = (BitBoards.WhitePawns << 9) & ~BitBoards.Files[0];
            }
            else
            {
                pawnsLeft = (BitBoards.BlackPawns >> 7) & ~BitBoards.Files[0];
                pawnsRight = (BitBoards.BlackPawns >> 9) & ~BitBoards.Files[7];
            }
            return pawnsLeft | pawnsRight;
        }

        private ulong GetAttackedByJumpingPieces(ulong jumpingPieces, ulong jumpMask, int jumpMaskCenter)
        {
            ulong allJumps = 0;
            for (var i = 0; i < 64; i++)
            {
                if (jumpingPieces.HasBit(i))
                {
                    ulong jumps;
                    if (i > jumpMaskCenter)
                    {
                        jumps = jumpMask << (i - jumpMaskCenter);
                    }
                    else
                    {
                        jumps = jumpMask >> (jumpMaskCenter - i);
                    }

                    jumps &= ~(i % 8 < 4 ? BitBoards.Files[6] | BitBoards.Files[7] : BitBoards.Files[0] | BitBoards.Files[1]);
                    allJumps |= jumps;
                }
            }
            return allJumps;
        }

        private ulong AllSlide(int position)
        {
            var hv = HorizontalVerticalSlide(position);
            var dad = DiagonalAntidiagonalSlide(position);
            return hv | dad;
        }

        private ulong HorizontalVerticalSlide(int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(pieceBitboard, BitBoards.Ranks[position/8]);
            var vertical = MaskedSlide(pieceBitboard, BitBoards.Files[position%8]);
            return horizontal | vertical;
        }

        private ulong DiagonalAntidiagonalSlide(int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(pieceBitboard, BitBoards.Diagonals[position/8 + position%8]);
            var vertical = MaskedSlide(pieceBitboard, BitBoards.Antidiagonals[position/8 + 7 - position%8]);
            return horizontal | vertical;
        }

        private ulong MaskedSlide(ulong pieceBitboard, ulong mask)
        {
            var slide = (((BitBoards.FilledSquares & mask) - 2 * pieceBitboard) ^ ((BitBoards.FilledSquares & mask).Reverse() - 2 * pieceBitboard.Reverse()).Reverse()) & mask;
            return slide;
        }
    }
}
