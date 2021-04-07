using System;
using System.Runtime.CompilerServices;
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

        private ulong GetAttackedByKings(Bitboard kings)
        {
            ulong allJumps = 0;
            while (kings != 0)
            {
                var i = kings.BitScanForward();
                ulong jumps = BitboardConstants.KingJumps[i];
                allJumps |= jumps;
                kings &= kings - 1;
            }
            return allJumps;
        }

        private ulong GetAttackedByKnights(Bitboard knights)
        {
            ulong allJumps = 0;
            while (knights != 0)
            {
                var i = knights.BitScanForward();
                ulong jumps = BitboardConstants.KnightJumps[i];
                allJumps |= jumps;
                knights &= knights - 1;
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

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong GetAttackedBySlidingPieces(Bitboard allPieces, Bitboard slidingPieces, bool diagonal)
        {
            var allSlide = 0UL;
            while (slidingPieces != 0)
            {
                var i = slidingPieces.BitScanForward();
                var slide = diagonal ? SlideMoveGenerator.DiagonalAntidiagonalSlide(allPieces, i) : SlideMoveGenerator.HorizontalVerticalSlide(allPieces, i);
                allSlide |= slide;
                slidingPieces &= slidingPieces - 1;
            }
            return allSlide;
        }

        public bool IsBitboardAttacked(Board board, Bitboard bitboard, bool byWhite)
        {
            while (bitboard != 0)
            {
                var position = bitboard.BitScanForward();
                var attacked = IsPositionAttacked(board, position, byWhite);
                if (attacked)
                {
                    return true;
                }
                bitboard &= bitboard - 1;
            }

            return false;
        }

        public Bitboard GetSlidingAttackersOf(Board board, Position position, Bitboard allPieces)
        {
            Bitboard result = 0UL;

            var bishops = board.BitBoard[ChessPiece.WhiteBishop] | board.BitBoard[ChessPiece.BlackBishop];
            var rooks = board.BitBoard[ChessPiece.WhiteRook] | board.BitBoard[ChessPiece.BlackRook];
            var queens = board.BitBoard[ChessPiece.WhiteQueen] | board.BitBoard[ChessPiece.BlackQueen];

            var diagonalAttack = SlideMoveGenerator.DiagonalAntidiagonalSlide(allPieces, position);
            result |= diagonalAttack & bishops;
            result |= diagonalAttack & queens;

            var verticalAttack = SlideMoveGenerator.HorizontalVerticalSlide(allPieces, position);
            result |= verticalAttack & rooks;
            result |= verticalAttack & queens;

            return result;
        }

        public Bitboard GetAttackersOf(Board board, Position position, Bitboard allPieces)
        {
            Bitboard result = 0UL;

            var whitePawns = board.BitBoard[ChessPiece.WhitePawn];
            var blackPawns = board.BitBoard[ChessPiece.BlackPawn];
            var knights = board.BitBoard[ChessPiece.WhiteKnight] | board.BitBoard[ChessPiece.BlackKnight];
            var bishops = board.BitBoard[ChessPiece.WhiteBishop] | board.BitBoard[ChessPiece.BlackBishop];
            var rooks = board.BitBoard[ChessPiece.WhiteRook] | board.BitBoard[ChessPiece.BlackRook];
            var queens = board.BitBoard[ChessPiece.WhiteQueen] | board.BitBoard[ChessPiece.BlackQueen];
            var kings = board.BitBoard[ChessPiece.WhiteKing] | board.BitBoard[ChessPiece.BlackKing];

            var knightAttack = BitboardConstants.KnightJumps[position];
            result |= knightAttack & knights;

            var kingAttack = BitboardConstants.KingJumps[position];
            result |= kingAttack & kings;

            var whitePawnAttack = BitboardConstants.PawnJumps[ChessPiece.Black, position];
            result |= whitePawnAttack & whitePawns;

            var blackPawnAttack = BitboardConstants.PawnJumps[ChessPiece.White, position];
            result |= blackPawnAttack & blackPawns;

            var diagonalAttack = SlideMoveGenerator.DiagonalAntidiagonalSlide(allPieces, position);
            result |= diagonalAttack & bishops;
            result |= diagonalAttack & queens;

            var verticalAttack = SlideMoveGenerator.HorizontalVerticalSlide(allPieces, position);
            result |= verticalAttack & rooks;
            result |= verticalAttack & queens;

            return result;
        }

        public Bitboard GetCheckers(Board board)
        {
            var checkers = GetAttackersOfSide(board, board.KingPositions[board.ColorToMove], !board.WhiteToMove, board.AllPieces);
            return checkers;
        }

        public Bitboard GetAttackersOfSide(Board board, Position position, bool byWhite, Bitboard allPieces)
        {
            Bitboard result = 0UL;

            Bitboard pawns;
            Bitboard knights;
            Bitboard bishops;
            Bitboard rooks;
            Bitboard queens;
            Bitboard kings;
            if (byWhite)
            {
                pawns = board.BitBoard[ChessPiece.WhitePawn];
                knights = board.BitBoard[ChessPiece.WhiteKnight];
                bishops = board.BitBoard[ChessPiece.WhiteBishop];
                rooks = board.BitBoard[ChessPiece.WhiteRook];
                queens = board.BitBoard[ChessPiece.WhiteQueen];
                kings = board.BitBoard[ChessPiece.WhiteKing];
            }
            else
            {
                pawns = board.BitBoard[ChessPiece.BlackPawn];
                knights = board.BitBoard[ChessPiece.BlackKnight];
                bishops = board.BitBoard[ChessPiece.BlackBishop];
                rooks = board.BitBoard[ChessPiece.BlackRook];
                queens = board.BitBoard[ChessPiece.BlackQueen];
                kings = board.BitBoard[ChessPiece.BlackKing];
            }

            var knightAttack = BitboardConstants.KnightJumps[position];
            result |= knightAttack & knights;

            var kingAttack = BitboardConstants.KingJumps[position];
            result |= kingAttack & kings;
            
            var pawnIndex = byWhite ? ChessPiece.Black : ChessPiece.White;
            var pawnAttack = BitboardConstants.PawnJumps[pawnIndex, position];
            result |= pawnAttack & pawns;

            var diagonalAttack = SlideMoveGenerator.DiagonalAntidiagonalSlide(allPieces, position);
            result |= diagonalAttack & bishops;
            result |= diagonalAttack & queens;

            var verticalAttack = SlideMoveGenerator.HorizontalVerticalSlide(allPieces, position);
            result |= verticalAttack & rooks;
            result |= verticalAttack & queens;

            return result;
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

            var pawnIndex = byWhite ? ChessPiece.Black : ChessPiece.White;
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