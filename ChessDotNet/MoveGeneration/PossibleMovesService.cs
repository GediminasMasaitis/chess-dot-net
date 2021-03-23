using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        
        public void GetAllPossibleMoves(Board board, Move[] moves, ref int moveCount)
        {
            Debug.Assert(moveCount == 0);
            GetAllPotentialMoves(board, moves, ref moveCount);
            FilterMovesByKingSafety(board, moves, ref moveCount);
        }

        public void GetAllPotentialMoves(Board board, Move[] moves, ref int moveCount)
        {
            var allowedSquareMask = ~0UL;

            GetPotentialKnightMoves(board, allowedSquareMask, moves, ref moveCount);
            GetPotentialBishopMoves(board, allowedSquareMask, moves, ref moveCount);
            GetPotentialRookMoves(board, allowedSquareMask, moves, ref moveCount);
            GetPotentialQueenMoves(board, allowedSquareMask, moves, ref moveCount);
            GetPotentialKingMoves(board, moves, ref moveCount);

            if (board.WhiteToMove)
            {
                GetPotentialWhitePawnCaptures(board, moves, ref moveCount);
                GetPotentialWhitePawnMoves(board, moves, ref moveCount);
            }
            else
            {
                GetPotentialBlackPawnCaptures(board, moves, ref moveCount);
                GetPotentialBlackPawnMoves(board, moves, ref moveCount);
            }

            //moves.Sort((m1, m2) => m1.Key2.CompareTo(m2.Key2));
        }

        public void GetAllPotentialCaptures(Board board, Move[] moves, ref int moveCount)
        {
            var allowedSquareMask = board.WhiteToMove ? board.BlackPieces : board.WhitePieces;

            GetPotentialKnightMoves(board, allowedSquareMask, moves, ref moveCount);
            GetPotentialBishopMoves(board, allowedSquareMask, moves, ref moveCount);
            GetPotentialRookMoves(board, allowedSquareMask, moves, ref moveCount);
            GetPotentialQueenMoves(board, allowedSquareMask, moves, ref moveCount);
            GetPotentialKingCaptures(board, allowedSquareMask, moves, ref moveCount);

            if (board.WhiteToMove)
            {
                GetPotentialWhitePawnCaptures(board, moves, ref moveCount);
            }
            else
            {
                GetPotentialBlackPawnCaptures(board, moves, ref moveCount);
            }

            //moves.RemoveAll(m => m.TakesPiece == 0);
        }

        private void GetPotentialWhitePawnCaptures(Board board, Move[] moves, ref int moveCount)
        {
            var takeLeft = (board.BitBoard[ChessPiece.WhitePawn] << 7) & ~BitboardConstants.Files[7] & board.BlackPieces;
            while (takeLeft != 0)
            {
                var i = takeLeft.BitScanForward();
                if (i > 55)
                {
                    GeneratePromotionMoves((Position)(i - 7), i, board.ArrayBoard[i], false, true, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i - 7), i, ChessPiece.WhitePawn, board.ArrayBoard[i]);
                    moves[moveCount++] = move;
                }
                takeLeft &= ~(1UL << i);
            }

            var takeRight = (board.BitBoard[ChessPiece.WhitePawn] << 9) & ~BitboardConstants.Files[0] & board.BlackPieces;
            while (takeRight != 0)
            {
                var i = takeRight.BitScanForward();
                if (i > 55)
                {
                    GeneratePromotionMoves((Position)(i - 9), i, board.ArrayBoard[i], false, true, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i - 9), i, ChessPiece.WhitePawn, board.ArrayBoard[i]);
                    moves[moveCount++] = move;
                }
                takeRight &= ~(1UL << i);
            }

            var enPassantLeft = (board.BitBoard[ChessPiece.WhitePawn] << 7) & ~BitboardConstants.Files[7] & board.EnPassantFile & BitboardConstants.Ranks[5] & board.BitBoard[ChessPiece.BlackPawn] << 8;
            while (enPassantLeft != 0)
            {
                var i = enPassantLeft.BitScanForward();
                var move = new Move((Position)(i - 7), i, ChessPiece.WhitePawn, board.ArrayBoard[i - 8], true);
                moves[moveCount++] = move;
                enPassantLeft &= ~(1UL << i);
            }


            var enPassantRight = (board.BitBoard[ChessPiece.WhitePawn] << 9) & ~BitboardConstants.Files[0] & board.EnPassantFile & BitboardConstants.Ranks[5] & board.BitBoard[ChessPiece.BlackPawn] << 8;
            while (enPassantRight != 0)
            {
                var i = enPassantRight.BitScanForward();
                var move = new Move((Position)(i - 9), i, ChessPiece.WhitePawn, board.ArrayBoard[i - 8], true);
                moves[moveCount++] = move;
                enPassantRight &= ~(1UL << i);
            }
        }

        private void GetPotentialWhitePawnMoves(Board board, Move[] moves, ref int moveCount)
        {
            var moveOne = (board.BitBoard[ChessPiece.WhitePawn] << 8) & board.EmptySquares;
            while (moveOne != 0)
            {
                var i = moveOne.BitScanForward();
                if (i > 55)
                {
                    GeneratePromotionMoves((Position)(i - 8), i, ChessPiece.Empty, false, true, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i - 8), i, ChessPiece.WhitePawn);
                    moves[moveCount++] = move;
                }
                moveOne &= ~(1UL << i);
            }

            var moveTwo = (board.BitBoard[ChessPiece.WhitePawn] << 16) & board.EmptySquares & board.EmptySquares << 8 & BitboardConstants.Ranks[3];
            while (moveTwo != 0)
            {
                var i = moveTwo.BitScanForward();
                var move = new Move((Position)(i - 16), i, ChessPiece.WhitePawn);
                moves[moveCount++] = move;
                moveTwo &= ~(1UL << i);
            }
        }

        private void GetPotentialBlackPawnCaptures(Board board, Move[] moves, ref int moveCount)
        {
            var takeLeft = (board.BitBoard[ChessPiece.BlackPawn] >> 7) & ~BitboardConstants.Files[0] & board.WhitePieces;
            while (takeLeft != 0)
            {
                var i = takeLeft.BitScanForward();
                if (i < 8)
                {
                    GeneratePromotionMoves((Position)(i + 7), i, board.ArrayBoard[i], false, false, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i + 7), i, ChessPiece.BlackPawn, board.ArrayBoard[i]);
                    moves[moveCount++] = move;
                }
                takeLeft &= ~(1UL << i);
            }

            var takeRight = (board.BitBoard[ChessPiece.BlackPawn] >> 9) & ~BitboardConstants.Files[7] & board.WhitePieces;
            while (takeRight != 0)
            {
                var i = takeRight.BitScanForward();
                if (i < 8)
                {
                    GeneratePromotionMoves((Position)(i + 9), i, board.ArrayBoard[i], false, false, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i + 9), i, ChessPiece.BlackPawn, board.ArrayBoard[i]);
                    moves[moveCount++] = move;
                }
                takeRight &= ~(1UL << i);
            }

            var enPassantLeft = (board.BitBoard[ChessPiece.BlackPawn] >> 7) & ~BitboardConstants.Files[0] & board.EnPassantFile & BitboardConstants.Ranks[2] & board.BitBoard[ChessPiece.WhitePawn] >> 8;
            while (enPassantLeft != 0)
            {
                var i = enPassantLeft.BitScanForward();
                var move = new Move((Position)(i + 7), i, ChessPiece.BlackPawn, board.ArrayBoard[i + 8], true);
                moves[moveCount++] = move;
                enPassantLeft &= ~(1UL << i);
            }


            var enPassantRight = (board.BitBoard[ChessPiece.BlackPawn] >> 9) & ~BitboardConstants.Files[7] & board.EnPassantFile & BitboardConstants.Ranks[2] & board.BitBoard[ChessPiece.WhitePawn] >> 8;
            while (enPassantRight != 0)
            {
                var i = enPassantRight.BitScanForward();
                var move = new Move((Position)(i + 9), i, ChessPiece.BlackPawn, board.ArrayBoard[i + 8], true);
                moves[moveCount++] = move;
                enPassantRight &= ~(1UL << i);
            }
        }

        private void GetPotentialBlackPawnMoves(Board board, Move[] moves, ref int moveCount)
        {
            var moveOne = (board.BitBoard[ChessPiece.BlackPawn] >> 8) & board.EmptySquares;
            while (moveOne != 0)
            {
                var i = moveOne.BitScanForward();
                if (i < 8)
                {
                    GeneratePromotionMoves((Position)(i + 8), i, board.ArrayBoard[i], false, false, moves, ref moveCount);
                }
                else
                {
                    var move = new Move((Position)(i + 8), i, ChessPiece.BlackPawn);
                    moves[moveCount++] = move;
                }
                moveOne &= ~(1UL << i);
            }

            var moveTwo = (board.BitBoard[ChessPiece.BlackPawn] >> 16) & board.EmptySquares & board.EmptySquares >> 8 & BitboardConstants.Ranks[4];
            while (moveTwo != 0)
            {
                var i = moveTwo.BitScanForward();
                var move = new Move((Position)(i + 16), i, ChessPiece.BlackPawn);
                moves[moveCount++] = move;
                moveTwo &= ~(1UL << i);
            }
        }

        private void GeneratePromotionMoves(Position from, Position to, Piece takesPiece, bool enPassant, bool forWhite, Move[] moves, ref int moveCount)
        {
            var piece = forWhite ? ChessPiece.WhitePawn : ChessPiece.BlackPawn;

            moves[moveCount++] = new Move(from, to, piece, takesPiece, enPassant, false, forWhite ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight);
            moves[moveCount++] = new Move(from, to, piece, takesPiece, enPassant, false, forWhite ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop);
            moves[moveCount++] = new Move(from, to, piece, takesPiece, enPassant, false, forWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook);
            moves[moveCount++] = new Move(from, to, piece, takesPiece, enPassant, false, forWhite ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen);
        }

        public void GetPotentialKingCaptures(Board board, Bitboard allowedSquareMask, Move[] moves, ref int moveCount)
        {
            var kings = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
            GetPotentialJumpingMoves(board, allowedSquareMask, kings, BitboardConstants.KingJumps, chessPiece, moves, ref moveCount);
        }

        public void GetPotentialKingMoves(Board board, Move[] moves, ref int moveCount)
        {
            var kings = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
            GetPotentialJumpingMoves(board, ~0UL, kings, BitboardConstants.KingJumps, chessPiece, moves, ref moveCount);
            GetPotentialCastlingMoves(board, moves, ref moveCount);
        }

        public void GetPotentialCastlingMoves(Board board, Move[] moves, ref int moveCount)
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
                    moves[moveCount++] = new Move(kingPos, (Position)(kingPos - 2), piece, ChessPiece.Empty, false, true);
                }
                if (canMaybeCastleKingSide && ((attackedByEnemy & kingSideCastleAttackMask) == 0))
                {
                    moves[moveCount++] = new Move(kingPos, (Position)(kingPos + 2), piece, ChessPiece.Empty, false, true);
                }
            }
        }

        public void GetPotentialKnightMoves(Board board, Bitboard allowedSquareMask, Move[] moves, ref int moveCount)
        {
            var knights = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKnight] : board.BitBoard[ChessPiece.BlackKnight];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight;
            GetPotentialJumpingMoves(board, allowedSquareMask, knights, BitboardConstants.KnightJumps, chessPiece, moves, ref moveCount);
        }

        private void GetPotentialJumpingMoves(Board board, Bitboard allowedSquareMask, ulong jumpingPieces, Bitboard[] jumpTable, Piece piece, Move[] moves, ref int moveCount)
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
                jumps &= allowedSquareMask;

                BitmaskToMoves(board, jumps, i, piece, moves, ref moveCount);

                jumpingPieces &= ~(1UL << i);
            }
        }

        public void GetPotentialRookMoves(Board board, Bitboard allowedSquareMask, Move[] moves, ref int moveCount)
        {
            var rooks = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteRook] : board.BitBoard[ChessPiece.BlackRook];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteRook : ChessPiece.BlackRook;
            GetPotentialSlidingPieceMoves(board, rooks, chessPiece, allowedSquareMask, moves, ref moveCount);
        }

        public void GetPotentialBishopMoves(Board board, Bitboard allowedSquareMask, Move[] moves, ref int moveCount)
        {
            var bishops = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteBishop] : board.BitBoard[ChessPiece.BlackBishop];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop;
            GetPotentialSlidingPieceMoves(board, bishops, chessPiece, allowedSquareMask, moves, ref moveCount);
        }

        public void GetPotentialQueenMoves(Board board, Bitboard allowedSquareMask, Move[] moves, ref int moveCount)
        {
            var bishops = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteQueen] : board.BitBoard[ChessPiece.BlackQueen];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
            GetPotentialSlidingPieceMoves(board, bishops, chessPiece, allowedSquareMask, moves, ref moveCount);
        }

        private void GetPotentialSlidingPieceMoves(Board board, ulong slidingPieces, Piece piece, Bitboard allowedSquareMask, Move[] moves, ref int moveCount)
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
                slide &= allowedSquareMask;
                BitmaskToMoves(board, slide, i, piece, moves, ref moveCount);
                slidingPieces &= ~(1UL << i);
            }
        }

        private void FilterMovesByKingSafety(Board board, Move[] moves, ref int moveCount)
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

        public bool IsKingSafeAfterMove(Board board, Move move)
        {
            Bitboard allPieces = board.AllPieces;
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
            var myKings = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            Position myKingPos;
            if (kingMove)
            {
                myKings &= ~fromBitboard;
                myKings |= toBitboard;
                myKingPos = move.To;
            }
            else
            {
                myKingPos = myKings.BitScanForward();
            }

            var invTakes = ~takesBitboard;

            Bitboard pawns;
            Bitboard knights;
            Bitboard bishops;
            Bitboard rooks;
            Bitboard queens;
            Bitboard kings;
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

            var pawnAttack = BitboardConstants.PawnJumps[move.WhiteToMoveNum, myKingPos]; //AttacksService.GetAttackedByPawns(myKings, board.WhiteToMove);
            if ((pawnAttack & pawns) != 0)
            {
                return false;
            }

            var diagonalAttack = AttacksService.SlideMoveGenerator.DiagonalAntidiagonalSlide(allPieces, myKingPos);
            if ((diagonalAttack & bishops) != 0)
            {
                return false;
            }
            if ((diagonalAttack & queens) != 0)
            {
                return false;
            }
            
            var verticalAttack = AttacksService.SlideMoveGenerator.HorizontalVerticalSlide(allPieces, myKingPos);
            if ((verticalAttack & rooks) != 0)
            {
                return false;
            }
            if ((verticalAttack & queens) != 0)
            {
                return false;
            }

            return true;
        }

        private void BitmaskToMoves(Board board, Bitboard bitmask, Position positionFrom, Piece piece, Move[] moves, ref int moveCount)
        {
            while (bitmask != 0)
            {
                var i = bitmask.BitScanForward();
                var move = new Move(positionFrom, i, piece, board.ArrayBoard[i]);
                moves[moveCount++] = move;
                bitmask &= ~(1UL << i);
            }
        }
    }
}
