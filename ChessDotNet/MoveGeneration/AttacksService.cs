using System;
using ChessDotNet.Common;
using ChessDotNet.Data;

using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Int32;
using Piece = System.Int32;

namespace ChessDotNet.MoveGeneration
{
    public class AttacksService
    {
        public ISlideMoveGenerator SlideMoveGenerator { get; set; }

        public AttacksService(ISlideMoveGenerator slideMoveGenerator)
        {
            SlideMoveGenerator = slideMoveGenerator;
        }

        public Bitboard GetAllAttacked(Board board, bool? whiteToMoveOverride = null, Bitboard? allPiecesOverride = null, Bitboard canAttackFrom = ~0UL)
        {
            var whiteToMove = whiteToMoveOverride ?? board.WhiteToMove;
            var allPieces = allPiecesOverride ?? board.AllPieces;

            Bitboard pawns;
            Bitboard knights;
            Bitboard kings;
            Bitboard bq;
            Bitboard rq;
            if (whiteToMove)
            {
                pawns = board.BitBoard[ChessPiece.WhitePawn];
                bq = board.BitBoard[ChessPiece.WhiteBishop] | board.BitBoard[ChessPiece.WhiteQueen];
                rq = board.BitBoard[ChessPiece.WhiteRook] | board.BitBoard[ChessPiece.WhiteQueen];
                knights = board.BitBoard[ChessPiece.WhiteKnight];
                kings = board.BitBoard[ChessPiece.WhiteKing];
            }
            else
            {
                pawns = board.BitBoard[ChessPiece.BlackPawn];
                bq = board.BitBoard[ChessPiece.BlackBishop] | board.BitBoard[ChessPiece.BlackQueen];
                rq = board.BitBoard[ChessPiece.BlackRook] | board.BitBoard[ChessPiece.BlackQueen];
                knights = board.BitBoard[ChessPiece.BlackKnight];
                kings = board.BitBoard[ChessPiece.BlackKing];
            }

            pawns &= canAttackFrom;
            bq &= canAttackFrom;
            rq &= canAttackFrom;
            knights &= canAttackFrom;
            kings &= canAttackFrom;

            var pawnsAttack = GetAttackedByPawns(pawns, whiteToMove);
            var knightsAttack = GetAttackedByKnights(knights);

            //var bishopsAttack = GetAttackedByBishops(bitBoards, forWhite);
            //var rooksAttack = GetAttackedByRooks(bitBoards, forWhite);
            //var queensAttack = GetAttackedByQueens(bitBoards, forWhite);

            
            var bqAttack = GetAttackedBySlidingPieces(allPieces, bq, true);
            var rqAttack = GetAttackedBySlidingPieces(allPieces, rq, false);

            var kingsAttack = GetAttackedByKings(kings);

            var allAttacked = pawnsAttack | knightsAttack | bqAttack | rqAttack | kingsAttack;
            return allAttacked;
        }

        /*public ulong GetAttackedByBishops(Board board, bool? byWhite = null)
        {
            var bishops = (byWhite ?? board.WhiteToMove) ? board.BitBoard[ChessPiece.WhiteBishop] : board.BitBoard[ChessPiece.BlackBishop];
            return GetAttackedBySlidingPieces(board, bishops, SlideMoveGenerator.DiagonalAntidiagonalSlide);
        }

        public ulong GetAttackedByRooks(Board board, bool? byWhite = null)
        {
            var rooks = (byWhite ?? board.WhiteToMove) ? board.BitBoard[ChessPiece.WhiteRook] : board.BitBoard[ChessPiece.BlackRook];
            return GetAttackedBySlidingPieces(board, rooks, SlideMoveGenerator.HorizontalVerticalSlide);
        }

        public ulong GetAttackedByQueens(Board board, bool? byWhite = null)
        {
            var queens = (byWhite ?? board.WhiteToMove) ? board.BitBoard[ChessPiece.WhiteQueen] : board.BitBoard[ChessPiece.BlackQueen];
            return GetAttackedBySlidingPieces(board, queens, SlideMoveGenerator.AllSlide);
        }*/

        public ulong GetAttackedByKings(Bitboard kings)
        {
            return GetAttackedByJumpingPieces(kings, BitboardConstants.KingSpan, BitboardConstants.KingSpanPosition);
        }

        public ulong GetAttackedByKnights(Bitboard knights)
        {
            return GetAttackedByJumpingPieces(knights, BitboardConstants.KnightSpan, BitboardConstants.KnightSpanPosition);
        }

        public ulong GetAttackedByPawns(Bitboard myPawns, bool whiteToMove)
        {
            ulong pawnsLeft;
            ulong pawnsRight;
            if (whiteToMove)
            {
                pawnsLeft = (myPawns << 7) & ~BitboardConstants.Files[7];
                pawnsRight = (myPawns << 9) & ~BitboardConstants.Files[0];
            }
            else
            {
                pawnsLeft = (myPawns >> 7) & ~BitboardConstants.Files[0];
                pawnsRight = (myPawns >> 9) & ~BitboardConstants.Files[7];
            }
            return pawnsLeft | pawnsRight;
        }

        private ulong GetAttackedBySlidingPieces(Bitboard allPieces, Bitboard slidingPieces, bool diagonal)
        {
            var allSlide = 0UL;
            while (slidingPieces != 0)
            {
                var i = slidingPieces.BitScanForward();
                var slide = diagonal ? SlideMoveGenerator.DiagonalAntidiagonalSlide(allPieces, i) : SlideMoveGenerator.HorizontalVerticalSlide(allPieces, i);
                allSlide |= slide;
                slidingPieces &= ~(1UL << i);
            }
            return allSlide;
        }

        private ulong GetAttackedByJumpingPieces(Bitboard jumpingPieces, Bitboard jumpMask, Position jumpMaskCenter)
        {
            ulong allJumps = 0;
            while (jumpingPieces != 0)
            {
                var i = jumpingPieces.BitScanForward();
                ulong jumps;
                if (i > jumpMaskCenter)
                {
                    jumps = jumpMask << (i - jumpMaskCenter);
                }
                else
                {
                    jumps = jumpMask >> (jumpMaskCenter - i);
                }

                jumps &= ~(i % 8 < 4 ? BitboardConstants.Files[6] | BitboardConstants.Files[7] : BitboardConstants.Files[0] | BitboardConstants.Files[1]);
                allJumps |= jumps;
                jumpingPieces &= ~(1UL << i);
            }
            return allJumps;
        }
    }
}