﻿using System;
using System.Collections.Generic;
using System.Linq;
using ChessDotNet.Common;
using ChessDotNet.Data;

using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;

namespace ChessDotNet.MoveGeneration
{
    public class PossibleMovesService
    {
        public AttacksService AttacksService { get; set; }
        public ISlideMoveGenerator SlideMoveGenerator { get; set; }

        public PossibleMovesService(AttacksService attacksService, ISlideMoveGenerator slideMoveGenerator)
        {
            AttacksService = attacksService;
            SlideMoveGenerator = slideMoveGenerator;
        }

        public List<Move> GetAllPossibleMoves(Board board)
        {
            var moves = new List<Move>(218);
            GetAllPotentialMoves(board, moves);
            FilterMovesByKingSafety(board, moves);
            return moves;
        }

        public void GetAllPossibleMoves(Board board, List<Move> moves)
        {
            GetAllPotentialMoves(board, moves);
            FilterMovesByKingSafety(board, moves);
        }

        public List<Move> GetAllPotentialMoves(Board board, List<Move> moves = null)
        {
            moves ??= new List<Move>();

            if (board.WhiteToMove)
            {
                GetPotentialWhiteCaptures(board, moves);
                GetPotentialWhitePawnMoves(board, moves);
            }
            else
            {
                GetPotentialBlackPawnCaptures(board, moves);
                GetPotentialBlackPawnMoves(board, moves);
            }

            GetPotentialKnightMoves(board, false, moves);
            GetPotentialBishopMoves(board, moves);
            GetPotentialRookMoves(board, moves);
            GetPotentialQueenMoves(board, moves);
            GetPotentialKingMoves(board, moves);

            return moves;
        }

        public List<Move> GetAllPotentialCaptures(Board board, List<Move> moves = null)
        {
            moves ??= new List<Move>();

            if (board.WhiteToMove)
            {
                GetPotentialWhiteCaptures(board, moves);
            }
            else
            {
                GetPotentialBlackPawnCaptures(board, moves);
            }

            GetPotentialKnightMoves(board, true, moves);
            GetPotentialBishopMoves(board, moves);
            GetPotentialRookMoves(board, moves);
            GetPotentialQueenMoves(board, moves);
            GetPotentialKingCaptures(board, moves);

            moves.RemoveAll(m => m.TakesPiece == 0);

            return moves;
        }

        private void GetPotentialWhiteCaptures(Board board, List<Move> moves)
        {
            var takeLeft = (board.BitBoard[ChessPiece.WhitePawn] << 7) & ~BitboardConstants.Files[7] & board.BlackPieces;
            var takeRight = (board.BitBoard[ChessPiece.WhitePawn] << 9) & ~BitboardConstants.Files[0] & board.BlackPieces;

            var enPassantLeft = (board.BitBoard[ChessPiece.WhitePawn] << 7) & ~BitboardConstants.Files[7] & board.EnPassantFile & BitboardConstants.Ranks[5] & board.BitBoard[ChessPiece.BlackPawn] << 8;
            var enPassantRight = (board.BitBoard[ChessPiece.WhitePawn] << 9) & ~BitboardConstants.Files[0] & board.EnPassantFile & BitboardConstants.Ranks[5] & board.BitBoard[ChessPiece.BlackPawn] << 8;

            for (Position i = 0; i < 64; i++)
            {
                if ((takeLeft & (1UL << i)) != 0)
                {
                    if (i > 55)
                    {
                        GeneratePromotionMoves((Position)(i - 7), i, board.ArrayBoard[i], false, true, moves);
                    }
                    else
                    {
                        var move = new Move((Position)(i - 7), i, ChessPiece.WhitePawn, board.ArrayBoard[i]);
                        moves.Add(move);
                    }
                }

                if ((takeRight & (1UL << i)) != 0)
                {
                    if (i > 55)
                    {
                        GeneratePromotionMoves((Position)(i - 9), i, board.ArrayBoard[i], false, true, moves);
                    }
                    else
                    {
                        var move = new Move((Position)(i - 9), i, ChessPiece.WhitePawn, board.ArrayBoard[i]);
                        moves.Add(move);
                    }
                }

                if ((enPassantLeft & (1UL << i)) != 0)
                {
                    var move = new Move((Position)(i - 7), i, ChessPiece.WhitePawn, board.ArrayBoard[i - 8], true);
                    moves.Add(move);
                }

                if ((enPassantRight & (1UL << i)) != 0)
                {
                    var move = new Move((Position)(i - 9), i, ChessPiece.WhitePawn, board.ArrayBoard[i - 8], true);
                    moves.Add(move);
                }
            }
        }

        private void GetPotentialWhitePawnMoves(Board board, List<Move> moves)
        {
            var moveOne = (board.BitBoard[ChessPiece.WhitePawn] << 8) & board.EmptySquares;
            var moveTwo = (board.BitBoard[ChessPiece.WhitePawn] << 16) & board.EmptySquares & board.EmptySquares << 8 & BitboardConstants.Ranks[3];

            //var moves = new List<Move>();

            for (Position i = 0; i < 64; i++)
            {
                if ((moveOne & (1UL << i)) != 0)
                {
                    if (i > 55)
                    {
                        GeneratePromotionMoves((Position)(i - 8), i, ChessPiece.Empty, false, true, moves);
                    }
                    else
                    {
                        var move = new Move((Position)(i - 8), i, ChessPiece.WhitePawn);
                        moves.Add(move);
                    }
                }

                if ((moveTwo & (1UL << i)) != 0)
                {
                    var move = new Move((Position)(i - 16), i, ChessPiece.WhitePawn);
                    moves.Add(move);
                }
            }
        }

        private void GetPotentialBlackPawnCaptures(Board board, List<Move> moves)
        {
            var takeLeft = (board.BitBoard[ChessPiece.BlackPawn] >> 7) & ~BitboardConstants.Files[0] & board.WhitePieces;
            var takeRight = (board.BitBoard[ChessPiece.BlackPawn] >> 9) & ~BitboardConstants.Files[7] & board.WhitePieces;

            var enPassantLeft = (board.BitBoard[ChessPiece.BlackPawn] >> 7) & ~BitboardConstants.Files[0] & board.EnPassantFile & BitboardConstants.Ranks[2] & board.BitBoard[ChessPiece.WhitePawn] >> 8;
            var enPassantRight = (board.BitBoard[ChessPiece.BlackPawn] >> 9) & ~BitboardConstants.Files[7] & board.EnPassantFile & BitboardConstants.Ranks[2] & board.BitBoard[ChessPiece.WhitePawn] >> 8;

            for (byte i = 0; i < 64; i++)
            {
                if ((takeLeft & (1UL << i)) != 0)
                {
                    if (i < 8)
                    {
                        GeneratePromotionMoves((Position)(i + 7), i, board.ArrayBoard[i], false, false, moves);
                    }
                    else
                    {
                        var move = new Move((Position)(i + 7), i, ChessPiece.BlackPawn, board.ArrayBoard[i]);
                        moves.Add(move);
                    }
                }

                if ((takeRight & (1UL << i)) != 0)
                {
                    if (i < 8)
                    {
                        GeneratePromotionMoves((Position)(i + 9), i, board.ArrayBoard[i], false, false, moves);
                    }
                    else
                    {
                        var move = new Move((Position)(i + 9), i, ChessPiece.BlackPawn, board.ArrayBoard[i]);
                        moves.Add(move);
                    }
                }

                if ((enPassantLeft & (1UL << i)) != 0)
                {
                    var move = new Move((Position)(i + 7), i, ChessPiece.BlackPawn, board.ArrayBoard[i + 8], true);
                    moves.Add(move);
                }

                if ((enPassantRight & (1UL << i)) != 0)
                {
                    var move = new Move((Position)(i + 9), i, ChessPiece.BlackPawn, board.ArrayBoard[i + 8], true);
                    moves.Add(move);
                }
            }
        }

        private void GetPotentialBlackPawnMoves(Board board, List<Move> moves)
        {
            var moveOne = (board.BitBoard[ChessPiece.BlackPawn] >> 8) & board.EmptySquares;
            var moveTwo = (board.BitBoard[ChessPiece.BlackPawn] >> 16) & board.EmptySquares & board.EmptySquares >> 8 & BitboardConstants.Ranks[4];

            for (byte i = 0; i < 64; i++)
            {
                if ((moveOne & (1UL << i)) != 0)
                {
                    if (i < 8)
                    {
                        GeneratePromotionMoves((Position)(i + 8), i, board.ArrayBoard[i], false, false, moves);
                    }
                    else
                    {
                        var move = new Move((Position)(i + 8), i, ChessPiece.BlackPawn);
                        moves.Add(move);
                    }
                }

                if ((moveTwo & (1UL << i)) != 0)
                {
                    var move = new Move((Position)(i + 16), i, ChessPiece.BlackPawn);
                    moves.Add(move);
                }
            }
        }

        private void GeneratePromotionMoves(Position from, Position to, Piece takesPiece, bool enPassant, bool forWhite, List<Move> moves)
        {
            var piece = forWhite ? ChessPiece.WhitePawn : ChessPiece.BlackPawn;
            /*var moves = new List<Move>
            {
                new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight),
                new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop),
                new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook),
                new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen),
            };*/
            moves.Add(new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight));
            moves.Add(new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop));
            moves.Add(new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook));
            moves.Add(new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen));
        }

        public List<Move> GetPossibleKingMoves(Board board)
        {
            var moves = new List<Move>();
            GetPotentialKingMoves(board, moves);
            var validMoves = FilterMovesByKingSafety(board, moves);
            return validMoves;
        }

        public void GetPotentialKingCaptures(Board board, List<Move> moves)
        {
            var kings = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
            GetPotentialJumpingMoves(board, true, kings, BitboardConstants.KingSpan, BitboardConstants.KingSpanPosition, chessPiece, moves);
        }

        public void GetPotentialKingMoves(Board board, List<Move> moves)
        {
            var kings = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
            GetPotentialJumpingMoves(board, false, kings, BitboardConstants.KingSpan, BitboardConstants.KingSpanPosition, chessPiece, moves);
            GetPotentialCastlingMoves(board, moves);
        }

        public void GetPotentialCastlingMoves(Board board, List<Move> moves)
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
                var attackedByEnemy = AttacksService.GetAllAttacked(board, !board.WhiteToMove);
                if (canMaybeCastleQueenSide && ((attackedByEnemy & queenSideCastleAttackMask) == 0))
                {
                    moves.Add(new Move(kingPos, (Position)(kingPos - 2), piece));
                }
                if (canMaybeCastleKingSide && ((attackedByEnemy & kingSideCastleAttackMask) == 0))
                {
                    moves.Add(new Move(kingPos, (Position)(kingPos + 2), piece));
                }
            }
        }

        public List<Move> GetPossibleKnightMoves(Board board, bool capturesOnly, List<Move> moves)
        {
            GetPotentialKnightMoves(board, capturesOnly, moves);
            var validMoves = FilterMovesByKingSafety(board, moves);
            return validMoves;
        }

        public void GetPotentialKnightMoves(Board board, bool capturesOnly, List<Move> moves)
        {
            var knights = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKnight] : board.BitBoard[ChessPiece.BlackKnight];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight;
            GetPotentialJumpingMoves(board, capturesOnly, knights, BitboardConstants.KnightSpan, BitboardConstants.KnightSpanPosition, chessPiece, moves);
        }

        private void GetPotentialJumpingMoves(Board board, bool capturesOnly, ulong jumpingPieces, ulong jumpMask, int jumpMaskCenter, Piece piece, List<Move> moves)
        {
            var ownPieces = board.WhiteToMove ? board.WhitePieces : board.BlackPieces;
            while (jumpingPieces != 0)
            {
                Position i = jumpingPieces.BitScanForward();
                Bitboard jumps;
                if (i > jumpMaskCenter)
                {
                    jumps = jumpMask << (i - jumpMaskCenter);
                }
                else
                {
                    jumps = jumpMask >> (jumpMaskCenter - i);
                }

                jumps &= ~(i % 8 < 4 ? BitboardConstants.Files[6] | BitboardConstants.Files[7] : BitboardConstants.Files[0] | BitboardConstants.Files[1]);
                jumps &= ~ownPieces;
                if (capturesOnly)
                {
                    var enemyPieces = board.WhiteToMove ? board.BlackPieces : board.WhitePieces;
                    jumps &= ~enemyPieces;
                }

                BitmaskToMoves(board, jumps, i, piece, moves);

                jumpingPieces &= ~(1UL << i);
            }
        }

        public List<Move> GetPossibleRookMoves(Board board, List<Move> moves)
        {
            GetPotentialRookMoves(board, moves);
            var validMoves = FilterMovesByKingSafety(board, moves);
            return validMoves;
        }

        public void GetPotentialRookMoves(Board board, List<Move> moves)
        {
            var rooks = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteRook] : board.BitBoard[ChessPiece.BlackRook];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteRook : ChessPiece.BlackRook;
            GetPotentialSlidingPieceMoves(board, rooks, chessPiece, moves);
        }

        public List<Move> GetPossibleBishopMoves(Board board, List<Move> moves)
        {
            GetPotentialBishopMoves(board, moves);
            var validMoves = FilterMovesByKingSafety(board, moves);
            return validMoves;
        }

        public void GetPotentialBishopMoves(Board board, List<Move> moves)
        {
            var bishops = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteBishop] : board.BitBoard[ChessPiece.BlackBishop];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop;
            GetPotentialSlidingPieceMoves(board, bishops, chessPiece, moves);
        }

        public List<Move> GetPossibleQueenMoves(Board board, List<Move> moves)
        {
            GetPotentialQueenMoves(board, moves);
            var validMoves = FilterMovesByKingSafety(board, moves);
            return validMoves;
        }

        public void GetPotentialQueenMoves(Board board, List<Move> moves)
        {
            var bishops = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteQueen] : board.BitBoard[ChessPiece.BlackQueen];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
            GetPotentialSlidingPieceMoves(board, bishops, chessPiece, moves);
        }

        private void GetPotentialSlidingPieceMoves(Board board, ulong slidingPieces, Piece piece, List<Move> moves)
        {
            var ownPieces = board.WhiteToMove ? board.WhitePieces : board.BlackPieces;
            while (slidingPieces != 0)
            {
                var i = slidingPieces.BitScanForward();
                //var slide = slideResolutionFunc.Invoke(board.AllPieces, i);
                Bitboard slide;
                switch (piece)
                {
                    case ChessPiece.WhiteRook:
                    case ChessPiece.BlackRook:
                        slide = SlideMoveGenerator.HorizontalVerticalSlide(board.AllPieces, i);
                        break;
                    case ChessPiece.WhiteBishop:
                    case ChessPiece.BlackBishop:
                        slide = SlideMoveGenerator.DiagonalAntidiagonalSlide(board.AllPieces, i);
                        break;
                    case ChessPiece.WhiteQueen:
                    case ChessPiece.BlackQueen:
                        slide = SlideMoveGenerator.AllSlide(board.AllPieces, i);
                        break;
                    default:
                        throw new Exception($"Attempted to generate slide attacks for a non-sliding piece: {piece}");
                }
                slide &= ~ownPieces;
                BitmaskToMoves(board, slide, i, piece, moves);
                slidingPieces &= ~(1UL << i);
            }
        }

        private List<Move> FilterMovesByKingSafety(Board board, List<Move> moves)
        {
            var toRemove = 0;
            for (var i = 0; i < moves.Count; i++)
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

            if (toRemove > 0)
            {
                moves.RemoveRange(moves.Count - toRemove, toRemove);
            }

            return moves;
        }

        public Board DoMoveIfKingSafe(Board board, Move move)
        {
            var isSafe = IsKingSafeAfterMove(board, move);
            if (isSafe)
            {
                var boardAfterMove = board.DoMove(move);
                return boardAfterMove;
            }
            return null;
        }

        public bool IsKingSafeAfterMove(Board board, Move move)
        {
            Bitboard allPieces = board.AllPieces;
            var inverseFromBitboard = ~(1UL << move.From);
            var toBitboard = 1UL << move.To;
            allPieces &= inverseFromBitboard;
            allPieces |= toBitboard;
            if (move.EnPassant)
            {
                var enPassantedBitboard = board.WhiteToMove ? toBitboard >> 8 : toBitboard << 8;
                allPieces &= ~enPassantedBitboard;
            }
            var enemyAttackedAfterMove = AttacksService.GetAllAttacked(board, !board.WhiteToMove, allPieces, ~toBitboard);

            var myKings = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            if ((board.WhiteToMove && move.Piece == ChessPiece.WhiteKing) ||
                (!board.WhiteToMove && move.Piece == ChessPiece.BlackKing))
            {
                myKings &= inverseFromBitboard;
                myKings |= toBitboard;
            }


            var isSafe = (enemyAttackedAfterMove & myKings) == 0;
            return isSafe;
        }

        private void BitmaskToMoves(Board board, Bitboard bitmask, Position positionFrom, Piece piece, List<Move> moves)
        {
            for (Position i = 0; i < 64; i++)
            {
                if ((bitmask & (1UL << i)) != 0)
                {
                    var move = new Move(positionFrom, i, piece, board.ArrayBoard[i]);
                    moves.Add(move);
                }
            }
        }
    }
}
