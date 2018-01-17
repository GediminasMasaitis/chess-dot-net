using System;
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
        public bool MultiThreadKingSafety { get; set; }

        public PossibleMovesService(AttacksService attacksService, ISlideMoveGenerator slideMoveGenerator)
        {
            AttacksService = attacksService;
            SlideMoveGenerator = slideMoveGenerator;
            MultiThreadKingSafety = false;
        }

        public IList<Move> GetAllPossibleMoves(Board board)
        {
            var potentialMoves = GetAllPotentialMoves(board);
            var validMoves = FilterMovesByKingSafety(board, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetAllPotentialMoves(Board board)
        {
            var pawnMoves = GetPotentialPawnMoves(board);
            var knightMoves = GetPotentialKnightMoves(board);
            var bishopMoves = GetPotentialBishopMoves(board);
            var rookMoves = GetPotentialRookMoves(board);
            var queenMoves = GetPotentialQueenMoves(board);
            var kingMoves = GetPotentialKingMoves(board);

            var allMoves = new List<Move>(pawnMoves.Count + kingMoves.Count + bishopMoves.Count + rookMoves.Count + queenMoves.Count + kingMoves.Count);
            allMoves.AddRange(pawnMoves);
            allMoves.AddRange(knightMoves);
            allMoves.AddRange(bishopMoves);
            allMoves.AddRange(rookMoves);
            allMoves.AddRange(queenMoves);
            allMoves.AddRange(kingMoves);

            //var allMoves = pawnMoves.Concat(knightMoves).Concat(bishopMoves).Concat(rookMoves).Concat(queenMoves).Concat(kingMoves);
            return allMoves;
        }

        public IList<Move> GetPossiblePawnMoves(Board board)
        {
            var potentialMoves = GetPotentialPawnMoves(board);
            var validMoves = FilterMovesByKingSafety(board, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialPawnMoves(Board board)
        {
            return board.WhiteToMove ? GetPotentialWhitePawnMoves(board) : GetPotentialBlackPawnMoves(board);
        }

        private IList<Move> GetPotentialWhitePawnMoves(Board board)
        {
            var takeLeft = (board.BitBoard[ChessPiece.WhitePawn] << 7) & ~BitboardConstants.Files[7] & board.BlackPieces;
            var takeRight = (board.BitBoard[ChessPiece.WhitePawn] << 9) & ~BitboardConstants.Files[0] & board.BlackPieces;

            var enPassantLeft = (board.BitBoard[ChessPiece.WhitePawn] << 7) & ~BitboardConstants.Files[7] & board.EnPassantFile & BitboardConstants.Ranks[5] & board.BitBoard[ChessPiece.BlackPawn] << 8;
            var enPassantRight = (board.BitBoard[ChessPiece.WhitePawn] << 9) & ~BitboardConstants.Files[0] & board.EnPassantFile & BitboardConstants.Ranks[5] & board.BitBoard[ChessPiece.BlackPawn] << 8;

            var moveOne = (board.BitBoard[ChessPiece.WhitePawn] << 8) & board.EmptySquares;
            var moveTwo = (board.BitBoard[ChessPiece.WhitePawn] << 16) & board.EmptySquares & board.EmptySquares << 8 & BitboardConstants.Ranks[3];

            var moves = new List<Move>();

            for (Position i = 0; i < 64; i++)
            {
                if ((takeLeft & (1UL << i)) != 0)
                {
                    if (i > 55)
                    {
                        var promotionMoves = GeneratePromotionMoves((Position)(i - 7), i, board.ArrayBoard[i], false, true);
                        moves.AddRange(promotionMoves);
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
                        var promotionMoves = GeneratePromotionMoves((Position)(i - 9), i, board.ArrayBoard[i], false, true);
                        moves.AddRange(promotionMoves);
                    }
                    else
                    {
                        var move = new Move((Position)(i - 9), i, ChessPiece.WhitePawn, board.ArrayBoard[i]);
                        moves.Add(move);
                    }
                }

                if ((enPassantLeft & (1UL << i)) != 0)
                {
                    var move = new Move((Position)(i - 7), i, ChessPiece.WhitePawn, board.ArrayBoard[i-8], true);
                    moves.Add(move);
                }

                if ((enPassantRight & (1UL << i)) != 0)
                {
                    var move = new Move((Position)(i - 9), i, ChessPiece.WhitePawn, board.ArrayBoard[i-8], true);
                    moves.Add(move);
                }

                if ((moveOne & (1UL << i)) != 0)
                {
                    if (i > 55)
                    {
                        var promotionMoves = GeneratePromotionMoves((Position)(i - 8), i, board.ArrayBoard[i], false, true);
                        moves.AddRange(promotionMoves);
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
            return moves;
        }

        private IList<Move> GetPotentialBlackPawnMoves(Board board)
        {
            var takeLeft = (board.BitBoard[ChessPiece.BlackPawn] >> 7) & ~BitboardConstants.Files[0] & board.WhitePieces;
            var takeRight = (board.BitBoard[ChessPiece.BlackPawn] >> 9) & ~BitboardConstants.Files[7] & board.WhitePieces;

            var enPassantLeft = (board.BitBoard[ChessPiece.BlackPawn] >> 7) & ~BitboardConstants.Files[0] & board.EnPassantFile & BitboardConstants.Ranks[2] & board.BitBoard[ChessPiece.WhitePawn] >> 8;
            var enPassantRight = (board.BitBoard[ChessPiece.BlackPawn] >> 9) & ~BitboardConstants.Files[7] & board.EnPassantFile & BitboardConstants.Ranks[2] & board.BitBoard[ChessPiece.WhitePawn] >> 8;

            var moveOne = (board.BitBoard[ChessPiece.BlackPawn] >> 8) & board.EmptySquares;
            var moveTwo = (board.BitBoard[ChessPiece.BlackPawn] >> 16) & board.EmptySquares & board.EmptySquares >> 8 & BitboardConstants.Ranks[4];

            var moves = new List<Move>();

            for (byte i = 0; i < 64; i++)
            {
                if ((takeLeft & (1UL << i)) != 0)
                {
                    if (i < 8)
                    {
                        var promotionMoves = GeneratePromotionMoves((Position)(i + 7), i, board.ArrayBoard[i], false, false);
                        moves.AddRange(promotionMoves);
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
                        var promotionMoves = GeneratePromotionMoves((Position)(i + 9), i, board.ArrayBoard[i], false, false);
                        moves.AddRange(promotionMoves);
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

                if ((moveOne & (1UL << i)) != 0)
                {
                    if (i < 8)
                    {
                        var promotionMoves = GeneratePromotionMoves((Position)(i + 8), i, board.ArrayBoard[i], false, false);
                        moves.AddRange(promotionMoves);
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
            return moves;
        }

        private IList<Move> GeneratePromotionMoves(Position from, Position to, Piece takesPiece, bool enPassant, bool forWhite)
        {
            var piece = forWhite ? ChessPiece.WhitePawn : ChessPiece.BlackPawn;
            var moves = new List<Move>
            {
                new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight),
                new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop),
                new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook),
                new Move(from, to, piece, takesPiece, enPassant, forWhite ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen),
            };
            return moves;
        }

        public IList<Move> GetPossibleKingMoves(Board board)
        {
            var potentialMoves = GetPotentialKingMoves(board);
            var validMoves = FilterMovesByKingSafety(board, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialKingMoves(Board board)
        {
            var kings = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
            var normalMoves = GetPotentialJumpingMoves(board, kings, BitboardConstants.KingSpan, BitboardConstants.KingSpanPosition, chessPiece);
            var castlingMoves = GetPotentialCastlingMoves(board);
            var allMoves = normalMoves.Union(castlingMoves).ToList();
            return allMoves;
        }

        public IList<Move> GetPotentialCastlingMoves(Board board)
        {
            var castlingMoves = new List<Move>();
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
                castlingPermissionQueenSide = board.CastlingPermissions[CastlePermission.WhiteQueenSide];
                castlingPermissionKingSide = board.CastlingPermissions[CastlePermission.WhiteKingSide];
                kingPos = board.BitBoard[ChessPiece.WhiteKing].BitScanForward();
                queenSideCastleMask = BitboardConstants.WhiteQueenSideCastleMask;
                kingSideCastleMask = BitboardConstants.WhiteKingSideCastleMask;
                queenSideCastleAttackMask = BitboardConstants.WhiteQueenSideCastleAttackMask;
                kingSideCastleAttackMask = BitboardConstants.WhiteKingSideCastleAttackMask;
                piece = ChessPiece.WhiteKing;
            }
            else
            {
                castlingPermissionQueenSide = board.CastlingPermissions[CastlePermission.BlackQueenSide];
                castlingPermissionKingSide = board.CastlingPermissions[CastlePermission.BlackKingSide];
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
                    castlingMoves.Add(new Move(kingPos, (Position)(kingPos - 2), piece));
                }
                if (canMaybeCastleKingSide && ((attackedByEnemy & kingSideCastleAttackMask) == 0))
                {
                    castlingMoves.Add(new Move(kingPos, (Position)(kingPos + 2), piece));
                }
            }

            return castlingMoves;
        }

        public IList<Move> GetPossibleKnightMoves(Board board)
        {
            var potentialMoves = GetPotentialKnightMoves(board);
            var validMoves = FilterMovesByKingSafety(board, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialKnightMoves(Board board)
        {
            var knights = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKnight] : board.BitBoard[ChessPiece.BlackKnight];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight;
            return GetPotentialJumpingMoves(board, knights, BitboardConstants.KnightSpan, BitboardConstants.KnightSpanPosition, chessPiece);
        }

        private IList<Move> GetPotentialJumpingMoves(Board board, ulong jumpingPieces, ulong jumpMask, int jumpMaskCenter, Piece piece)
        {
            var ownPieces = board.WhiteToMove ? board.WhitePieces : board.BlackPieces;
            var moves = new List<Move>();
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

                jumps &= ~(i%8 < 4 ? BitboardConstants.Files[6] | BitboardConstants.Files[7] : BitboardConstants.Files[0] | BitboardConstants.Files[1]);
                jumps &= ~ownPieces;

                foreach (var move in BitmaskToMoves(board, jumps, i, piece))
                {
                    moves.Add(move);
                }

                jumpingPieces &= ~(1UL << i);
            }
            return moves;
        }

        public IList<Move> GetPossibleRookMoves(Board board)
        {
            var potentialMoves = GetPotentialRookMoves(board);
            var validMoves = FilterMovesByKingSafety(board, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialRookMoves(Board board)
        {
            var rooks = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteRook] : board.BitBoard[ChessPiece.BlackRook];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteRook : ChessPiece.BlackRook;
            return GetPotentialSlidingPieceMoves(board, rooks, chessPiece);
        }

        public IList<Move> GetPossibleBishopMoves(Board board)
        {
            var potentialMoves = GetPotentialBishopMoves(board);
            var validMoves = FilterMovesByKingSafety(board, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialBishopMoves(Board board)
        {
            var bishops = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteBishop] : board.BitBoard[ChessPiece.BlackBishop];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop;
            return GetPotentialSlidingPieceMoves(board, bishops, chessPiece);
        }

        public IList<Move> GetPossibleQueenMoves(Board board)
        {
            var potentialMoves = GetPotentialQueenMoves(board);
            var validMoves = FilterMovesByKingSafety(board, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialQueenMoves(Board board)
        {
            var bishops = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteQueen] : board.BitBoard[ChessPiece.BlackQueen];
            var chessPiece = board.WhiteToMove ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
            return GetPotentialSlidingPieceMoves(board, bishops, chessPiece);
        }

        private IList<Move> GetPotentialSlidingPieceMoves(Board board, ulong slidingPieces, Piece piece)
        {
            var ownPieces = board.WhiteToMove ? board.WhitePieces : board.BlackPieces;
            var moves = new List<Move>();
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
                foreach (var move in BitmaskToMoves(board, slide, i, piece))
                {
                    moves.Add(move);
                }
                slidingPieces &= ~(1UL << i);
            }
            return moves;
        }

        private IList<Move> FilterMovesByKingSafety(Board board, IList<Move> moves)
        {
            if (MultiThreadKingSafety)
            {
                return moves.AsParallel().Where(x => IsKingSafeAfterMove(board, x)).ToList();
            }
            else
            {
                var filteredMoves = new List<Move>(moves.Count);
                foreach (var move in moves)
                {
                    var safe = IsKingSafeAfterMove(board, move);
                    if (safe)
                    {
                        filteredMoves.Add(move);
                    }
                }
                return filteredMoves;
                //return moves.Where(x => IsKingSafeAfterMove(board, x)).ToList();
            }
        }

        public Board DoMoveIfKingSafe(Board board, Move move)
        {
            //return DoMoveIfKingSafeNew(board, move);
            return DoMoveIfKingSafeOld(board, move);
        }

        private Board DoMoveIfKingSafeOld(Board board, Move move)
        {
            var boardAfterMove = board.DoMove(move);
            var enemyAttackedAfterMove = AttacksService.GetAllAttacked(boardAfterMove);
            var myKings = board.WhiteToMove
                ? boardAfterMove.BitBoard[ChessPiece.WhiteKing]
                : boardAfterMove.BitBoard[ChessPiece.BlackKing];
            var isSafe = (enemyAttackedAfterMove & myKings) == 0;
            return isSafe ? boardAfterMove : null;
        }

        private Board DoMoveIfKingSafeNew(Board board, Move move)
        {
            var isSafe = IsKingSafeAfterMoveNew(board, move);
            if (isSafe)
            {
                var boardAfterMove = board.DoMove(move);
                return boardAfterMove;
            }
            return null;
        }

        public bool IsKingSafeAfterMove(Board board, Move move)
        {
            //return true;
            //return IsKingSafeAfterMoveOld(board, move);
            return IsKingSafeAfterMoveNew(board, move);

            var n = IsKingSafeAfterMoveNew(board, move);
            var o = IsKingSafeAfterMoveOld(board, move);

            if (n != o)
            {
                var n1 = IsKingSafeAfterMoveNew(board, move);
                var o1 = IsKingSafeAfterMoveOld(board, move);
            }

            return n;
        }

        private bool IsKingSafeAfterMoveOld(Board board, Move move)
        {
            var afterMove = DoMoveIfKingSafeOld(board, move);
            return afterMove != null;
        }

        private bool IsKingSafeAfterMoveNew(Board board, Move move)
        {
            Bitboard allPieces = board.AllPieces;
            var frombb = ~(1UL << move.From);
            var tobb = 1UL << move.To;
            allPieces &= frombb;
            allPieces |= tobb;
            var enemyAttackedAfterMove = AttacksService.GetAllAttacked(board, !board.WhiteToMove, allPieces, ~tobb);

            var myKings = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            if ((board.WhiteToMove && move.Piece == ChessPiece.WhiteKing) ||
                (!board.WhiteToMove && move.Piece == ChessPiece.BlackKing))
            {
                myKings &= frombb;
                myKings |= tobb;
            }


            var isSafe = (enemyAttackedAfterMove & myKings) == 0;
            return isSafe;
        }

        private static IList<Move> BitmaskToMoves(Board board, Bitboard bitmask, Position positionFrom, Piece piece)
        {
            var moves = new List<Move>();
            for (Position i = 0; i < 64; i++)
            {
                if ((bitmask & (1UL << i)) != 0)
                {
                    var move = new Move(positionFrom, i, piece, board.ArrayBoard[i]);
                    moves.Add(move);
                }
            }
            return moves;
        }
    }
}
