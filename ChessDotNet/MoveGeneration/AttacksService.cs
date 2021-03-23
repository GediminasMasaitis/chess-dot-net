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
                pawns = board.BitBoard[ChessPiece.WhitePawn] & allPieces;
                bq = board.BitBoard[ChessPiece.WhiteBishop] | board.BitBoard[ChessPiece.WhiteQueen];
                rq = board.BitBoard[ChessPiece.WhiteRook] | board.BitBoard[ChessPiece.WhiteQueen];
                knights = board.BitBoard[ChessPiece.WhiteKnight];
                kings = board.BitBoard[ChessPiece.WhiteKing];
            }
            else
            {
                pawns = board.BitBoard[ChessPiece.BlackPawn] & allPieces;
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
            
            var bqAttack = GetAttackedBySlidingPieces(allPieces, bq, true);
            var rqAttack = GetAttackedBySlidingPieces(allPieces, rq, false);

            var kingsAttack = GetAttackedByKings(kings);

            var allAttacked = pawnsAttack | knightsAttack | bqAttack | rqAttack | kingsAttack;
            return allAttacked;
        }

        public ulong GetAttackedByKings(Bitboard kings)
        {
            ulong allJumps = 0;
            while (kings != 0)
            {
                var i = kings.BitScanForward();
                ulong jumps = BitboardConstants.KingJumps[i];
                allJumps |= jumps;
                kings &= ~(1UL << i);
            }
            return allJumps;
        }

        public ulong GetAttackedByKnights(Bitboard knights)
        {
            ulong allJumps = 0;
            while (knights != 0)
            {
                var i = knights.BitScanForward();
                ulong jumps = BitboardConstants.KnightJumps[i];
                allJumps |= jumps;
                knights &= ~(1UL << i);
            }
            return allJumps;
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

        public bool IsPositionAttacked(Board board, Position position, bool byWhite)
        {
            Bitboard allPieces = board.AllPieces;

            var invTakes = ~0UL;

            Bitboard pawns;
            Bitboard knights;
            Bitboard bishops;
            Bitboard rooks;
            Bitboard queens;
            Bitboard kings;
            if (byWhite)
            {
                pawns = board.BitBoard[ChessPiece.WhitePawn] & invTakes;
                knights = board.BitBoard[ChessPiece.WhiteKnight] & invTakes;
                bishops = board.BitBoard[ChessPiece.WhiteBishop] & invTakes;
                rooks = board.BitBoard[ChessPiece.WhiteRook] & invTakes;
                queens = board.BitBoard[ChessPiece.WhiteQueen] & invTakes;
                kings = board.BitBoard[ChessPiece.WhiteKing] & invTakes;
            }
            else
            {
                pawns = board.BitBoard[ChessPiece.BlackPawn] & invTakes;
                knights = board.BitBoard[ChessPiece.BlackKnight] & invTakes;
                bishops = board.BitBoard[ChessPiece.BlackBishop] & invTakes;
                rooks = board.BitBoard[ChessPiece.BlackRook] & invTakes;
                queens = board.BitBoard[ChessPiece.BlackQueen] & invTakes;
                kings = board.BitBoard[ChessPiece.BlackKing] & invTakes;
            }

            var knightAttack = BitboardConstants.KnightJumps[position];
            if ((knightAttack & knights) != 0)
            {
                return true;
            }

            var kingAttack = BitboardConstants.KingJumps[position];
            if ((kingAttack & kings) != 0)
            {
                return true;
            }

            var pawnIndex = byWhite ? 0 : 1;
            var pawnAttack = BitboardConstants.PawnJumps[pawnIndex, position];
            if ((pawnAttack & pawns) != 0)
            {
                return true;
            }

            var diagonalAttack = SlideMoveGenerator.DiagonalAntidiagonalSlide(allPieces, position);
            if ((diagonalAttack & bishops) != 0)
            {
                return true;
            }
            if ((diagonalAttack & queens) != 0)
            {
                return true;
            }

            var verticalAttack = SlideMoveGenerator.HorizontalVerticalSlide(allPieces, position);
            if ((verticalAttack & rooks) != 0)
            {
                return true;
            }
            if ((verticalAttack & queens) != 0)
            {
                return true;
            }

            return false;
        }
    }
}