using System;
using System.Runtime.CompilerServices;
using ChessDotNet.Common;
using ChessDotNet.Data;

namespace ChessDotNet.MoveGeneration
{
    public class MoveValidator
    {
        private readonly AttacksService _attacksService;
        private readonly ISlideMoveGenerator _slideMoveGenerator;
        private readonly PinDetector _pinDetector;

        public MoveValidator(AttacksService attacksService, ISlideMoveGenerator slideMoveGenerator, PinDetector pinDetector)
        {
            _attacksService = attacksService;
            _slideMoveGenerator = slideMoveGenerator;
            _pinDetector = pinDetector;
        }

        public void FilterMovesByKingSafety(Board board, Move[] moves, ref int moveCount, ulong checkers, ulong pinned)
        {
            //var checkers = _attacksService.GetAttackersOfSide(board, board.KingPositions[board.ColorToMove], !board.WhiteToMove, board.AllPieces);
            //var pinnedPieces = _pinDetector.GetPinned(board, board.ColorToMove, board.KingPositions[board.ColorToMove]);

            var toRemove = 0;
            for (var i = 0; i < moveCount; i++)
            {
                var move = moves[i];
                var safe = IsKingSafeAfterMove2(board, move, checkers, pinned);
                if (safe)
                {
                    if (toRemove > 0)
                    {
                        moves[i - toRemove] = move;
                    }
                }
                else
                {
                    toRemove++;
                }
            }

            moveCount -= toRemove;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsKingSafeAfterMove2(Board board, Move move, ulong checkers, ulong pinnedPieces)
        {
            var kingMove = move.Piece == ChessPiece.King + board.ColorToMove;
            //var checkers = board.Checkers;
            //var pinnedPieces = board.PinnedPieces;
            //var checkCount = checkers.PopCount();

            //if (checkCount > 1 && !kingMove)
            //{
            //    return false;
            //}

            
            var isPinned = (pinnedPieces & (1UL << move.From)) != 0;
            //var toBitboard = 1UL << move.To;
            //if (checkCount == 1 && !kingMove)
            //{
            //    if (isPinned)
            //    {
            //        return false;
            //    }

            //    var checkerPos = checkers.BitScanForward();
            //    var canMoveTo = BitboardConstants.Between[board.KingPositions[board.ColorToMove]][checkerPos] | checkers;
                
            //    if ((canMoveTo & toBitboard) == 0)
            //    {
            //        return false;
            //    }

            //    return true;
            //}

            //if (isPinned)
            //{
            //    var canMoveTo = BitboardConstants.Between[board.KingPositions[board.ColorToMove]][move.From];
            //    if ((canMoveTo & toBitboard) != 0)
            //    {
            //        return true;
            //    }
            //}

            if
            (
                move.EnPassant
                || kingMove
                || isPinned
            )
            {
                return IsKingSafeAfterMove(board, move);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsKingSafeAfterMove(Board board, Move move)
        {
            var allPieces = board.AllPieces;
            var fromBitboard = 1UL << move.From;
            var toBitboard = 1UL << move.To;

            allPieces &= ~fromBitboard;
            allPieces |= toBitboard;

            var takesBitboard = toBitboard;
            if (move.EnPassant)
            {
                var enPassantedBitboard = board.WhiteToMove ? toBitboard >> 8 : toBitboard << 8;
                allPieces &= ~enPassantedBitboard;
                takesBitboard |= enPassantedBitboard;
            }

            var kingMove = move.Piece == ChessPiece.WhiteKing || move.Piece == ChessPiece.BlackKing;
            Byte myKingPos = kingMove ? move.To : board.KingPositions[board.ColorToMove];

            //Position myKingPos = board.KingPositions[board.ColorToMove];

            var invTakes = ~takesBitboard;

            UInt64 pawns;
            UInt64 knights;
            UInt64 bishops;
            UInt64 rooks;
            UInt64 queens;
            UInt64 kings;
            if (board.WhiteToMove)
            {
                pawns = board.BitBoard[ChessPiece.BlackPawn] & invTakes;
                knights = board.BitBoard[ChessPiece.BlackKnight] & invTakes;
                bishops = board.BitBoard[ChessPiece.BlackBishop] & invTakes;
                rooks = board.BitBoard[ChessPiece.BlackRook] & invTakes;
                queens = board.BitBoard[ChessPiece.BlackQueen] & invTakes;
                kings = board.BitBoard[ChessPiece.BlackKing] & invTakes;
            }
            else
            {
                pawns = board.BitBoard[ChessPiece.WhitePawn] & invTakes;
                knights = board.BitBoard[ChessPiece.WhiteKnight] & invTakes;
                bishops = board.BitBoard[ChessPiece.WhiteBishop] & invTakes;
                rooks = board.BitBoard[ChessPiece.WhiteRook] & invTakes;
                queens = board.BitBoard[ChessPiece.WhiteQueen] & invTakes;
                kings = board.BitBoard[ChessPiece.WhiteKing] & invTakes;
            }

            var knightAttack = BitboardConstants.KnightJumps[myKingPos];
            if ((knightAttack & knights) != 0)
            {
                return false;
            }

            var kingAttack = BitboardConstants.KingJumps[myKingPos];
            if ((kingAttack & kings) != 0)
            {
                return false;
            }

            var pawnAttack = BitboardConstants.PawnJumps[move.ColorToMove, myKingPos]; //AttacksService.GetAttackedByPawns(myKings, board.WhiteToMove);
            if ((pawnAttack & pawns) != 0)
            {
                return false;
            }

            var diagonalAttack = _slideMoveGenerator.DiagonalAntidiagonalSlide(allPieces, myKingPos);
            if ((diagonalAttack & (bishops | queens)) != 0)
            {
                return false;
            }

            var verticalAttack = _slideMoveGenerator.HorizontalVerticalSlide(allPieces, myKingPos);
            if ((verticalAttack & (rooks | queens)) != 0)
            {
                return false;
            }

            return true;
        }
    }
}