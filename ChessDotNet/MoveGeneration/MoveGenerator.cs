using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessDotNet.Common;
using ChessDotNet.Data;

using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;

namespace ChessDotNet.MoveGeneration
{
    public class MoveGenerator
    {
        private readonly AttacksService _attacksService;
        private readonly ISlideMoveGenerator _slideMoveGenerator;
        private readonly PinDetector _pinDetector;
        private readonly MoveValidator _validator;

        public MoveGenerator(AttacksService attacksService, ISlideMoveGenerator slideMoveGenerator, PinDetector pinDetector, MoveValidator validator)
        {
            _attacksService = attacksService;
            _slideMoveGenerator = slideMoveGenerator;
            _pinDetector = pinDetector;
            _validator = validator;
        }

        public void GetAllLegalMoves(Board board, Move[] moves, ref int moveCount)
        {
            Debug.Assert(moveCount == 0);
            var checkers = _attacksService.GetCheckers(board);
            var pinned = _pinDetector.GetPinned(board, board.ColorToMove, board.KingPositions[board.ColorToMove]);
            GetAllPotentialMoves(board, moves, ref moveCount, checkers, pinned);
            _validator.FilterMovesByKingSafety(board, moves, ref moveCount, checkers, pinned);
        }

        private void ReorderMoves(Move[] moves, int moveCount)
        {
            var moves2 = moves.Take(moveCount).OrderBy(x => x.Key2).ToArray();
            for (var i = 0; i < moves2.Length; i++)
            {
                moves[i] = moves2[i];
            }
        }

        public void GetAllPotentialMoves1(Board board, Move[] moves, ref int moveCount, Bitboard checkers, Bitboard pinned)
        {
            var allowedFrom = ~0UL;
            var allowedTo = ~0UL;
            
            GetPotentialKnightMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialBishopMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialRookMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialQueenMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialKingMoves(board, moves, ref moveCount);

            if (board.WhiteToMove)
            {
                GetPotentialWhitePawnCaptures(board, allowedFrom, allowedTo, moves, ref moveCount);
                GetPotentialWhitePawnMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            }
            else
            {
                GetPotentialBlackPawnCaptures(board, allowedFrom, allowedTo, moves, ref moveCount);
                GetPotentialBlackPawnMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            }

            //moves.Sort((m1, m2) => m1.Key2.CompareTo(m2.Key2));
        }

        public void GetAllPotentialMoves(Board board, Move[] moves, ref int moveCount, Bitboard checkers, Bitboard pinned)
        {
            var checkCount = checkers.PopCount();
            if (checkCount > 1)
            {
                GetPotentialKingMoves(board, moves, ref moveCount);
                return;
            }

            var allowedFrom = ~0UL;
            var allowedTo = ~0UL;

            if (checkCount == 1)
            {
                allowedFrom = ~pinned;

                var checkerPos = checkers.BitScanForward();
                allowedTo = BitboardConstants.Between[board.KingPositions[board.ColorToMove]][checkerPos] | checkers;
            }
            else
            {
                GetPotentialCastlingMoves(board, moves, ref moveCount);
            }

            GetPotentialKnightMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialBishopMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialRookMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialQueenMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialKingMoves(board, moves, ref moveCount);

            if (board.WhiteToMove)
            {
                GetPotentialWhitePawnCaptures(board, allowedFrom, allowedTo, moves, ref moveCount);
                GetPotentialWhitePawnMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            }
            else
            {
                GetPotentialBlackPawnCaptures(board, allowedFrom, allowedTo, moves, ref moveCount);
                GetPotentialBlackPawnMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            }

            //moves.Sort((m1, m2) => m1.Key2.CompareTo(m2.Key2));
        }

        public void GetAllPotentialCaptures(Board board, Move[] moves, ref int moveCount, Bitboard checkers, Bitboard pinned)
        {
            var allowedFrom = ~0UL;
            var allowedToKing = board.WhiteToMove ? board.BlackPieces : board.WhitePieces;
            var allowedTo = allowedToKing;

            var checkCount = checkers.PopCount();
            if (checkCount > 1)
            {
                GetPotentialKingCaptures(board, allowedToKing, moves, ref moveCount);
                return;
            }

            if (checkCount == 1)
            {
                allowedFrom = ~pinned;
                allowedTo = checkers;
            }

            GetPotentialKnightMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialBishopMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialRookMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialQueenMoves(board, allowedFrom, allowedTo, moves, ref moveCount);
            GetPotentialKingCaptures(board, allowedToKing, moves, ref moveCount);

            if (board.WhiteToMove)
            {
                GetPotentialWhitePawnCaptures(board, allowedFrom, allowedTo, moves, ref moveCount);
            }
            else
            {
                GetPotentialBlackPawnCaptures(board, allowedFrom, allowedTo, moves, ref moveCount);
            }

            //moves.RemoveAll(m => m.TakesPiece == 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialWhitePawnCaptures(Board board, Bitboard allowedFrom, Bitboard allowedTo, Move[] moves, ref int moveCount)
        {
            var pawns = board.BitBoard[ChessPiece.WhitePawn] & allowedFrom;
            var takeLeft = (pawns << 7) & ~BitboardConstants.Files[7] & board.BlackPieces & allowedTo;
            while (takeLeft != 0)
            {
                var i = takeLeft.BitScanForward();
                if (i > 55)
                {
                    GeneratePromotionMoves((Position)(i - 7), i, board.ArrayBoard[i], false, ChessPiece.White, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i - 7), i, ChessPiece.WhitePawn, board.ArrayBoard[i]);
                    moves[moveCount++] = move;
                }
                takeLeft &= takeLeft - 1;
            }

            var takeRight = (pawns << 9) & ~BitboardConstants.Files[0] & board.BlackPieces & allowedTo;
            while (takeRight != 0)
            {
                var i = takeRight.BitScanForward();
                if (i > 55)
                {
                    GeneratePromotionMoves((Position)(i - 9), i, board.ArrayBoard[i], false, ChessPiece.White, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i - 9), i, ChessPiece.WhitePawn, board.ArrayBoard[i]);
                    moves[moveCount++] = move;
                }
                takeRight &= takeRight - 1;
            }

            var enPassantLeft = (pawns << 7) & ~BitboardConstants.Files[7] & board.EnPassantFile & BitboardConstants.Ranks[5] & board.BitBoard[ChessPiece.BlackPawn] << 8;
            while (enPassantLeft != 0)
            {
                var i = enPassantLeft.BitScanForward();
                var move = new Move((Position)(i - 7), i, ChessPiece.WhitePawn, board.ArrayBoard[i - 8], true);
                moves[moveCount++] = move;
                enPassantLeft &= enPassantLeft - 1;
            }


            var enPassantRight = (pawns << 9) & ~BitboardConstants.Files[0] & board.EnPassantFile & BitboardConstants.Ranks[5] & board.BitBoard[ChessPiece.BlackPawn] << 8;
            while (enPassantRight != 0)
            {
                var i = enPassantRight.BitScanForward();
                var move = new Move((Position)(i - 9), i, ChessPiece.WhitePawn, board.ArrayBoard[i - 8], true);
                moves[moveCount++] = move;
                enPassantRight &= enPassantRight - 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialWhitePawnMoves(Board board, Bitboard allowedFrom, Bitboard allowedTo, Move[] moves, ref int moveCount)
        {
            var pawns = board.BitBoard[ChessPiece.WhitePawn] & allowedFrom;
            var moveOne = (pawns << 8) & board.EmptySquares & allowedTo;
            while (moveOne != 0)
            {
                var i = moveOne.BitScanForward();
                if (i > 55)
                {
                    GeneratePromotionMoves((Position)(i - 8), i, ChessPiece.Empty, false, ChessPiece.White, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i - 8), i, ChessPiece.WhitePawn);
                    moves[moveCount++] = move;
                }
                moveOne &= moveOne - 1;
            }

            var moveTwo = allowedTo & (pawns << 16) & board.EmptySquares & board.EmptySquares << 8 & BitboardConstants.Ranks[3];
            while (moveTwo != 0)
            {
                var i = moveTwo.BitScanForward();
                var move = new Move((Position)(i - 16), i, ChessPiece.WhitePawn);
                moves[moveCount++] = move;
                moveTwo &= moveTwo - 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialBlackPawnCaptures(Board board, Bitboard allowedFrom, Bitboard allowedTo, Move[] moves, ref int moveCount)
        {
            var pawns = board.BitBoard[ChessPiece.BlackPawn] & allowedFrom;
            var takeLeft = (pawns >> 7) & ~BitboardConstants.Files[0] & board.WhitePieces & allowedTo;
            while (takeLeft != 0)
            {
                var i = takeLeft.BitScanForward();
                if (i < 8)
                {
                    GeneratePromotionMoves((Position)(i + 7), i, board.ArrayBoard[i], false, ChessPiece.Black, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i + 7), i, ChessPiece.BlackPawn, board.ArrayBoard[i]);
                    moves[moveCount++] = move;
                }
                takeLeft &= takeLeft - 1;
            }

            var takeRight = (pawns >> 9) & ~BitboardConstants.Files[7] & board.WhitePieces & allowedTo;
            while (takeRight != 0)
            {
                var i = takeRight.BitScanForward();
                if (i < 8)
                {
                    GeneratePromotionMoves((Position)(i + 9), i, board.ArrayBoard[i], false, ChessPiece.Black, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i + 9), i, ChessPiece.BlackPawn, board.ArrayBoard[i]);
                    moves[moveCount++] = move;
                }
                takeRight &= takeRight - 1;
            }

            var enPassantLeft = (pawns >> 7) & ~BitboardConstants.Files[0] & board.EnPassantFile & BitboardConstants.Ranks[2] & board.BitBoard[ChessPiece.WhitePawn] >> 8;
            while (enPassantLeft != 0)
            {
                var i = enPassantLeft.BitScanForward();
                var move = new Move((Position)(i + 7), i, ChessPiece.BlackPawn, board.ArrayBoard[i + 8], true);
                moves[moveCount++] = move;
                enPassantLeft &= enPassantLeft - 1;
            }


            var enPassantRight = (pawns >> 9) & ~BitboardConstants.Files[7] & board.EnPassantFile & BitboardConstants.Ranks[2] & board.BitBoard[ChessPiece.WhitePawn] >> 8;
            while (enPassantRight != 0)
            {
                var i = enPassantRight.BitScanForward();
                var move = new Move((Position)(i + 9), i, ChessPiece.BlackPawn, board.ArrayBoard[i + 8], true);
                moves[moveCount++] = move;
                enPassantRight &= enPassantRight - 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialBlackPawnMoves(Board board, Bitboard allowedFrom, Bitboard allowedTo, Move[] moves, ref int moveCount)
        {
            var pawns = board.BitBoard[ChessPiece.BlackPawn] & allowedFrom;
            var moveOne = (pawns >> 8) & board.EmptySquares & allowedTo;
            while (moveOne != 0)
            {
                var i = moveOne.BitScanForward();
                if (i < 8)
                {
                    GeneratePromotionMoves((Position)(i + 8), i, board.ArrayBoard[i], false, ChessPiece.Black, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i + 8), i, ChessPiece.BlackPawn);
                    moves[moveCount++] = move;
                }
                moveOne &= moveOne - 1;
            }

            var moveTwo = allowedTo & (pawns >> 16) & board.EmptySquares & board.EmptySquares >> 8 & BitboardConstants.Ranks[4];
            while (moveTwo != 0)
            {
                var i = moveTwo.BitScanForward();
                var move = new Move((Position)(i + 16), i, ChessPiece.BlackPawn);
                moves[moveCount++] = move;
                moveTwo &= moveTwo - 1;
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GeneratePromotionMoves(Position from, Position to, Piece takesPiece, bool enPassant, Piece colorToMove, Move[] moves, ref int moveCount)
        {
            var piece = (Piece)(ChessPiece.Pawn | colorToMove);

            moves[moveCount++] = new Move(from, to, piece, takesPiece, enPassant, false, (Piece)(ChessPiece.Queen | colorToMove));
            moves[moveCount++] = new Move(from, to, piece, takesPiece, enPassant, false, (Piece)(ChessPiece.Knight | colorToMove));
            moves[moveCount++] = new Move(from, to, piece, takesPiece, enPassant, false, (Piece)(ChessPiece.Rook | colorToMove));
            moves[moveCount++] = new Move(from, to, piece, takesPiece, enPassant, false, (Piece)(ChessPiece.Bishop | colorToMove));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialKingCaptures(Board board, Bitboard allowedTo, Move[] moves, ref int moveCount)
        {
            var kings = board.BitBoard[ChessPiece.King | board.ColorToMove];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
            GetPotentialJumpingMoves(board, allowedTo, kings, BitboardConstants.KingJumps, chessPiece, moves, ref moveCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialKingMoves(Board board, Move[] moves, ref int moveCount)
        {
            var kings = board.BitBoard[ChessPiece.King | board.ColorToMove];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
            GetPotentialJumpingMoves(board, ~0UL, kings, BitboardConstants.KingJumps, chessPiece, moves, ref moveCount);

            //var requiredPermissions = board.WhiteToMove ? CastlingPermission.White : CastlingPermission.Black;
            //if ((board.CastlingPermissions & requiredPermissions) != CastlingPermission.None)
            //{
                
            //}
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialCastlingMoves(Board board, Move[] moves, ref int moveCount)
        {
            var isWhite = board.WhiteToMove;
            Position kingPos;
            ulong queenSideCastleMask;
            ulong kingSideCastleMask;
            ulong queenSideCastleAttackMask;
            ulong kingSideCastleAttackMask;
            Piece piece;
            bool castlingPermissionQueenSide;
            bool castlingPermissionKingSide;

            if (isWhite)
            {
                castlingPermissionQueenSide = (board.CastlingPermissions & CastlingPermission.WhiteQueen) != CastlingPermission.None;
                castlingPermissionKingSide = (board.CastlingPermissions & CastlingPermission.WhiteKing) != CastlingPermission.None;
                //kingPos = board.KingPositions[ChessPiece.White];
                kingPos = board.BitBoard[ChessPiece.WhiteKing].BitScanForward();
                queenSideCastleMask = BitboardConstants.WhiteQueenSideCastleMask;
                kingSideCastleMask = BitboardConstants.WhiteKingSideCastleMask;
                queenSideCastleAttackMask = BitboardConstants.WhiteQueenSideCastleAttackMask;
                kingSideCastleAttackMask = BitboardConstants.WhiteKingSideCastleAttackMask;
                piece = ChessPiece.WhiteKing;
            }
            else
            {
                castlingPermissionQueenSide = (board.CastlingPermissions & CastlingPermission.BlackQueen) != CastlingPermission.None;
                castlingPermissionKingSide = (board.CastlingPermissions & CastlingPermission.BlackKing) != CastlingPermission.None;
                //kingPos = board.KingPositions[ChessPiece.Black];
                kingPos = board.BitBoard[ChessPiece.BlackKing].BitScanForward();
                queenSideCastleMask = BitboardConstants.BlackQueenSideCastleMask;
                kingSideCastleMask = BitboardConstants.BlackKingSideCastleMask;
                queenSideCastleAttackMask = BitboardConstants.BlackQueenSideCastleAttackMask;
                kingSideCastleAttackMask = BitboardConstants.BlackKingSideCastleAttackMask;
                piece = ChessPiece.BlackKing;
            }

            var canMaybeCastleQueenSide = castlingPermissionQueenSide && ((board.AllPieces & queenSideCastleMask) == 0);
            var canMaybeCastleKingSide = castlingPermissionKingSide && (board.AllPieces & kingSideCastleMask) == 0;

            if (canMaybeCastleQueenSide | canMaybeCastleKingSide)
            {
                var attackedByEnemy = _attacksService.GetAllAttacked(board, !board.WhiteToMove);
                //if (canMaybeCastleQueenSide && !AttacksService.IsBitboardAttacked(board, queenSideCastleAttackMask, !board.WhiteToMove))
                if (canMaybeCastleQueenSide && ((attackedByEnemy & queenSideCastleAttackMask) == 0))
                {
                    moves[moveCount++] = new Move(kingPos, (Position)(kingPos - 2), piece, ChessPiece.Empty, false, true);
                }
                //if (canMaybeCastleKingSide && !AttacksService.IsBitboardAttacked(board, kingSideCastleAttackMask, !board.WhiteToMove))
                if (canMaybeCastleKingSide && ((attackedByEnemy & kingSideCastleAttackMask) == 0))
                {
                    moves[moveCount++] = new Move(kingPos, (Position)(kingPos + 2), piece, ChessPiece.Empty, false, true);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialKnightMoves(Board board, Bitboard allowedFrom, Bitboard allowedTo, Move[] moves, ref int moveCount)
        {
            var knights = board.BitBoard[ChessPiece.Knight | board.ColorToMove] & allowedFrom;
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight;
            GetPotentialJumpingMoves(board, allowedTo, knights, BitboardConstants.KnightJumps, chessPiece, moves, ref moveCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialJumpingMoves(Board board, Bitboard allowedTo, ulong jumpingPieces, Bitboard[] jumpTable, Piece piece, Move[] moves, ref int moveCount)
        {
            var ownPieces = board.WhiteToMove ? board.WhitePieces : board.BlackPieces;
            while (jumpingPieces != 0)
            {
                Position i = jumpingPieces.BitScanForward();
                var jumps = jumpTable[i];
                //Bitboard jumps;
                //if (i > jumpMaskCenter)
                //{
                //    jumps = jumpMask << (i - jumpMaskCenter);
                //}
                //else
                //{
                //    jumps = jumpMask >> (jumpMaskCenter - i);
                //}

                //jumps &= ~(i % 8 < 4 ? BitboardConstants.Files[6] | BitboardConstants.Files[7] : BitboardConstants.Files[0] | BitboardConstants.Files[1]);
                jumps &= ~ownPieces;
                jumps &= allowedTo;

                BitmaskToMoves(board, jumps, i, piece, moves, ref moveCount);

                jumpingPieces &= jumpingPieces - 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialRookMoves(Board board, Bitboard allowedFrom, Bitboard allowedTo, Move[] moves, ref int moveCount)
        {
            var piece = (Piece)(ChessPiece.Rook | board.ColorToMove);
            var piecesBitmask = board.BitBoard[piece] & allowedFrom;
            var ownPieces = board.WhiteToMove ? board.WhitePieces : board.BlackPieces;
            while (piecesBitmask != 0)
            {
                var position = piecesBitmask.BitScanForward();
                Bitboard slide = _slideMoveGenerator.HorizontalVerticalSlide(board.AllPieces, position);
                slide &= ~ownPieces;
                slide &= allowedTo;
                BitmaskToMoves(board, slide, position, piece, moves, ref moveCount);
                piecesBitmask &= piecesBitmask - 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialBishopMoves(Board board, Bitboard allowedFrom,  Bitboard allowedTo, Move[] moves, ref int moveCount)
        {
            var piece = (Piece)(ChessPiece.Bishop | board.ColorToMove);
            var piecesBitmask = board.BitBoard[piece] & allowedFrom;
            var ownPieces = board.WhiteToMove ? board.WhitePieces : board.BlackPieces;
            while (piecesBitmask != 0)
            {
                var position = piecesBitmask.BitScanForward();
                Bitboard slide = _slideMoveGenerator.DiagonalAntidiagonalSlide(board.AllPieces, position);
                slide &= ~ownPieces;
                slide &= allowedTo;
                BitmaskToMoves(board, slide, position, piece, moves, ref moveCount);
                piecesBitmask &= piecesBitmask - 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPotentialQueenMoves(Board board, Bitboard allowedFrom, Bitboard allowedTo, Move[] moves, ref int moveCount)
        {
            var piece = (Piece)(ChessPiece.Queen | board.ColorToMove);
            var piecesBitmask = board.BitBoard[piece] & allowedFrom;
            var ownPieces = board.WhiteToMove ? board.WhitePieces : board.BlackPieces;
            while (piecesBitmask != 0)
            {
                var position = piecesBitmask.BitScanForward();
                Bitboard slide = _slideMoveGenerator.AllSlide(board.AllPieces, position);
                slide &= ~ownPieces;
                slide &= allowedTo;
                BitmaskToMoves(board, slide, position, piece, moves, ref moveCount);
                piecesBitmask &= piecesBitmask - 1;
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BitmaskToMoves(Board board, Bitboard bitmask, Position positionFrom, Piece piece, Move[] moves, ref int moveCount)
        {
            while (bitmask != 0)
            {
                var i = bitmask.BitScanForward();
                var move = new Move(positionFrom, i, piece, board.ArrayBoard[i]);
                moves[moveCount++] = move;
                bitmask &= bitmask - 1;
            }
        }
    }
}
