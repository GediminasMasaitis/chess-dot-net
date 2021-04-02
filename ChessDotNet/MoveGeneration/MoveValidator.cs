using System;
using System.Runtime.CompilerServices;
using ChessDotNet.Common;
using ChessDotNet.Data;

namespace ChessDotNet.MoveGeneration
{
    public class PinDetector
    {
        private readonly ISlideMoveGenerator _slideMoveGenerator;

        public PinDetector(ISlideMoveGenerator slideMoveGenerator)
        {
            _slideMoveGenerator = slideMoveGenerator;
        }

        public ulong GetPinned(Board board, byte color, byte pos)
        {
            var opponentColor = (byte)(color ^ 1);
            var pinned = 0UL;
            var ownPieces = color == ChessPiece.White ? board.WhitePieces : board.BlackPieces;

            var xrays = DiagonalAntidiagonalXray(board.AllPieces, ownPieces, pos);
            var pinners = xrays & (board.BitBoard[ChessPiece.Bishop | opponentColor] | board.BitBoard[ChessPiece.Queen | opponentColor]);

            while (pinners != 0)
            {
                int pinner = pinners.BitScanForward();
                pinned |= BitboardConstants.Between[pinner][pos] & ownPieces;
                pinners &= pinners - 1;
            }

            xrays = HorizontalVerticalXray(board.AllPieces, ownPieces, pos);
            pinners = xrays & (board.BitBoard[ChessPiece.Rook | opponentColor] | board.BitBoard[ChessPiece.Queen | opponentColor]);

            while (pinners != 0)
            {
                int pinner = pinners.BitScanForward();
                pinned |= BitboardConstants.Between[pinner][pos] & ownPieces;
                pinners &= pinners - 1;
            }
            return pinned;
        }

        private ulong DiagonalAntidiagonalXray(ulong allPieces, ulong ownPieces, byte position)
        {
            var attacks = _slideMoveGenerator.DiagonalAntidiagonalSlide(allPieces, position);
            ownPieces &= attacks;
            var xrayAttacks = attacks ^ _slideMoveGenerator.DiagonalAntidiagonalSlide(allPieces ^ ownPieces, position);
            return xrayAttacks;
        }

        private ulong HorizontalVerticalXray(ulong allPieces, ulong ownPieces, byte position)
        {
            var attacks = _slideMoveGenerator.HorizontalVerticalSlide(allPieces, position);
            ownPieces &= attacks;
            var xrayAttacks = attacks ^ _slideMoveGenerator.HorizontalVerticalSlide(allPieces ^ ownPieces, position);
            return xrayAttacks;
        }
    }

    public class MoveValidator
    {
        private readonly AttacksService _attacksService;
        private readonly ISlideMoveGenerator _slideMoveGenerator;

        public MoveValidator(AttacksService attacksService, ISlideMoveGenerator slideMoveGenerator)
        {
            _attacksService = attacksService;
            _slideMoveGenerator = slideMoveGenerator;
        }

        public void FilterMovesByKingSafety(Board board, Move[] moves, ref int moveCount)
        {

            var toRemove = 0;
            for (var i = 0; i < moveCount; i++)
            {
                var move = moves[i];
                var safe = IsKingSafeAfterMove(board, move);
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
        public bool IsKingSafeAfterMove2(Board board, Move move)
        {
            var kingMove = move.Piece == ChessPiece.King + board.ColorToMove;
            var checkers = board.Checkers;
            var checkCount = checkers.BitCount();

            if (checkCount > 1 && !kingMove)
            {
                return false;
            }

            var isPinned = (board.PinnedPieces & (1UL << move.From)) != 0;
            if (checkCount == 1 && !kingMove)
            {
                if (isPinned)
                {
                    return false;
                }

                var checkerPos = checkers.BitScanForward();
                var canMoveTo = BitboardConstants.Between[board.KingPositions[board.ColorToMove]][checkerPos] | checkers;
                var toBitboard = 1UL << move.To;
                if ((canMoveTo & toBitboard) == 0)
                {
                    return false;
                }

                return true;
            }

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