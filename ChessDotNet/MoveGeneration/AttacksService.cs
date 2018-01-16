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
        public ISlideMoveGenerator HyperbolaQuintessence { get; set; }

        public AttacksService(ISlideMoveGenerator hyperbolaQuintessence)
        {
            HyperbolaQuintessence = hyperbolaQuintessence;
        }

        public ulong GetAllAttacked(Board board, bool? byWhite = null)
        {
            byWhite = byWhite ?? board.WhiteToMove;
            var pawnsAttack = GetAttackedByPawns(board, byWhite);
            var knightsAttack = GetAttackedByKnights(board, byWhite);

            //var bishopsAttack = GetAttackedByBishops(bitBoards, forWhite);
            //var rooksAttack = GetAttackedByRooks(bitBoards, forWhite);
            //var queensAttack = GetAttackedByQueens(bitBoards, forWhite);

            var bq = byWhite.Value ? board.BitBoard[ChessPiece.WhiteBishop] | board.BitBoard[ChessPiece.WhiteQueen] : board.BitBoard[ChessPiece.BlackBishop] | board.BitBoard[ChessPiece.BlackQueen];
            var bqAttack = GetAttackedBySlidingPieces(board.AllPieces, bq, true);

            var rq = byWhite.Value ? board.BitBoard[ChessPiece.WhiteRook] | board.BitBoard[ChessPiece.WhiteQueen] : board.BitBoard[ChessPiece.BlackRook] | board.BitBoard[ChessPiece.BlackQueen];
            var rqAttack = GetAttackedBySlidingPieces(board.AllPieces, rq, false);

            var kingsAttack = GetAttackedByKings(board, byWhite);

            var allAttacked = pawnsAttack | knightsAttack | bqAttack | rqAttack | kingsAttack;
            return allAttacked;
        }

        /*public ulong GetAttackedByBishops(Board board, bool? byWhite = null)
        {
            var bishops = (byWhite ?? board.WhiteToMove) ? board.BitBoard[ChessPiece.WhiteBishop] : board.BitBoard[ChessPiece.BlackBishop];
            return GetAttackedBySlidingPieces(board, bishops, HyperbolaQuintessence.DiagonalAntidiagonalSlide);
        }

        public ulong GetAttackedByRooks(Board board, bool? byWhite = null)
        {
            var rooks = (byWhite ?? board.WhiteToMove) ? board.BitBoard[ChessPiece.WhiteRook] : board.BitBoard[ChessPiece.BlackRook];
            return GetAttackedBySlidingPieces(board, rooks, HyperbolaQuintessence.HorizontalVerticalSlide);
        }

        public ulong GetAttackedByQueens(Board board, bool? byWhite = null)
        {
            var queens = (byWhite ?? board.WhiteToMove) ? board.BitBoard[ChessPiece.WhiteQueen] : board.BitBoard[ChessPiece.BlackQueen];
            return GetAttackedBySlidingPieces(board, queens, HyperbolaQuintessence.AllSlide);
        }*/

        public ulong GetAttackedByKings(Board board, bool? byWhite = null)
        {
            var kings = (byWhite ?? board.WhiteToMove) ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            return GetAttackedByJumpingPieces(board, kings, BitboardConstants.KingSpan, BitboardConstants.KingSpanPosition);
        }

        public ulong GetAttackedByKnights(Board board, bool? byWhite = null)
        {
            var knights = (byWhite ?? board.WhiteToMove) ? board.BitBoard[ChessPiece.WhiteKnight] : board.BitBoard[ChessPiece.BlackKnight];
            return GetAttackedByJumpingPieces(board, knights, BitboardConstants.KnightSpan, BitboardConstants.KnightSpanPosition);
        }

        public ulong GetAttackedByPawns(Board board, bool? byWhite = null)
        {
            ulong pawnsLeft;
            ulong pawnsRight;
            if (byWhite ?? board.WhiteToMove)
            {
                pawnsLeft = (board.BitBoard[ChessPiece.WhitePawn] << 7) & ~BitboardConstants.Files[7];
                pawnsRight = (board.BitBoard[ChessPiece.WhitePawn] << 9) & ~BitboardConstants.Files[0];
            }
            else
            {
                pawnsLeft = (board.BitBoard[ChessPiece.BlackPawn] >> 7) & ~BitboardConstants.Files[0];
                pawnsRight = (board.BitBoard[ChessPiece.BlackPawn] >> 9) & ~BitboardConstants.Files[7];
            }
            return pawnsLeft | pawnsRight;
        }

        private ulong GetAttackedBySlidingPieces(Bitboard allPieces, ulong slidingPieces, bool diagonal)
        {
            var allSlide = 0UL;
            while (slidingPieces != 0)
            {
                var i = slidingPieces.BitScanForward();
                var slide = diagonal ? HyperbolaQuintessence.DiagonalAntidiagonalSlide(allPieces, i) : HyperbolaQuintessence.HorizontalVerticalSlide(allPieces, i);
                allSlide |= slide;
                slidingPieces &= ~(1UL << i);
            }
            return allSlide;
        }

        private ulong GetAttackedByJumpingPieces(Board board, ulong jumpingPieces, ulong jumpMask, int jumpMaskCenter)
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