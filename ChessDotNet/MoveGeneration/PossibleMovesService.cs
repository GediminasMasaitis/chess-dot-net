using System;
using System.Collections.Generic;
using System.Linq;
using ChessDotNet.Data;

namespace ChessDotNet.MoveGeneration
{
    public class PossibleMovesService
    {
        public AttacksService AttacksService { get; set; }
        public HyperbolaQuintessence HyperbolaQuintessence { get; set; }
        public bool MultiThreadKingSafety { get; set; }

        public PossibleMovesService(AttacksService attacksService, HyperbolaQuintessence hyperbolaQuintessence)
        {
            AttacksService = attacksService;
            HyperbolaQuintessence = hyperbolaQuintessence;
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
            var takeLeft = (board.BitBoard[ChessPiece.WhitePawn] << 7) & ~Board.Files[7] & board.BlackPieces;
            var takeRight = (board.BitBoard[ChessPiece.WhitePawn] << 9) & ~Board.Files[0] & board.BlackPieces;

            var enPassantLeft = (board.BitBoard[ChessPiece.WhitePawn] << 7) & ~Board.Files[7] & board.EnPassantFile & Board.Ranks[5] & board.BitBoard[ChessPiece.BlackPawn] << 8;
            var enPassantRight = (board.BitBoard[ChessPiece.WhitePawn] << 9) & ~Board.Files[0] & board.EnPassantFile & Board.Ranks[5] & board.BitBoard[ChessPiece.BlackPawn] << 8;

            var moveOne = (board.BitBoard[ChessPiece.WhitePawn] << 8) & board.EmptySquares;
            var moveTwo = (board.BitBoard[ChessPiece.WhitePawn] << 16) & board.EmptySquares & board.EmptySquares << 8 & Board.Ranks[3];

            var moves = new List<Move>();

            for (byte i = 0; i < 64; i++)
            {
                if ((takeLeft & (1UL << i)) != 0)
                {
                    if (i > 55)
                    {
                        var promotionMoves = GeneratePromotionMoves(i - 7, i, board.ArrayBoard[i], false, true);
                        moves.AddRange(promotionMoves);
                    }
                    else
                    {
                        var move = new Move(i - 7, i, ChessPiece.WhitePawn, board.ArrayBoard[i]);
                        moves.Add(move);
                    }
                }

                if ((takeRight & (1UL << i)) != 0)
                {
                    if (i > 55)
                    {
                        var promotionMoves = GeneratePromotionMoves(i - 9, i, board.ArrayBoard[i], false, true);
                        moves.AddRange(promotionMoves);
                    }
                    else
                    {
                        var move = new Move(i - 9, i, ChessPiece.WhitePawn, board.ArrayBoard[i]);
                        moves.Add(move);
                    }
                }

                if ((enPassantLeft & (1UL << i)) != 0)
                {
                    var move = new Move(i - 7, i, ChessPiece.WhitePawn, board.ArrayBoard[i-8], true);
                    moves.Add(move);
                }

                if ((enPassantRight & (1UL << i)) != 0)
                {
                    var move = new Move(i - 9, i, ChessPiece.WhitePawn, board.ArrayBoard[i-8], true);
                    moves.Add(move);
                }

                if ((moveOne & (1UL << i)) != 0)
                {
                    if (i > 55)
                    {
                        var promotionMoves = GeneratePromotionMoves(i - 8, i, board.ArrayBoard[i], false, true);
                        moves.AddRange(promotionMoves);
                    }
                    else
                    {
                        var move = new Move(i - 8, i, ChessPiece.WhitePawn);
                        moves.Add(move);
                    }
                }

                if ((moveTwo & (1UL << i)) != 0)
                {
                    var move = new Move(i - 16, i, ChessPiece.WhitePawn);
                    moves.Add(move);
                }
            }
            return moves;
        }

        private IList<Move> GetPotentialBlackPawnMoves(Board board)
        {
            var takeLeft = (board.BitBoard[ChessPiece.BlackPawn] >> 7) & ~Board.Files[0] & board.WhitePieces;
            var takeRight = (board.BitBoard[ChessPiece.BlackPawn] >> 9) & ~Board.Files[7] & board.WhitePieces;

            var enPassantLeft = (board.BitBoard[ChessPiece.BlackPawn] >> 7) & ~Board.Files[0] & board.EnPassantFile & Board.Ranks[2] & board.BitBoard[ChessPiece.WhitePawn] >> 8;
            var enPassantRight = (board.BitBoard[ChessPiece.BlackPawn] >> 9) & ~Board.Files[7] & board.EnPassantFile & Board.Ranks[2] & board.BitBoard[ChessPiece.WhitePawn] >> 8;

            var moveOne = (board.BitBoard[ChessPiece.BlackPawn] >> 8) & board.EmptySquares;
            var moveTwo = (board.BitBoard[ChessPiece.BlackPawn] >> 16) & board.EmptySquares & board.EmptySquares >> 8 & Board.Ranks[4];

            var moves = new List<Move>();

            for (byte i = 0; i < 64; i++)
            {
                if ((takeLeft & (1UL << i)) != 0)
                {
                    if (i < 8)
                    {
                        var promotionMoves = GeneratePromotionMoves(i + 7, i, board.ArrayBoard[i], false, false);
                        moves.AddRange(promotionMoves);
                    }
                    else
                    {
                        var move = new Move(i + 7, i, ChessPiece.BlackPawn, board.ArrayBoard[i]);
                        moves.Add(move);
                    }
                }

                if ((takeRight & (1UL << i)) != 0)
                {
                    if (i < 8)
                    {
                        var promotionMoves = GeneratePromotionMoves(i + 9, i, board.ArrayBoard[i], false, false);
                        moves.AddRange(promotionMoves);
                    }
                    else
                    {
                        var move = new Move(i + 9, i, ChessPiece.BlackPawn, board.ArrayBoard[i]);
                        moves.Add(move);
                    }
                }

                if ((enPassantLeft & (1UL << i)) != 0)
                {
                    var move = new Move(i + 7, i, ChessPiece.BlackPawn, board.ArrayBoard[i + 8], true);
                    moves.Add(move);
                }

                if ((enPassantRight & (1UL << i)) != 0)
                {
                    var move = new Move(i + 9, i, ChessPiece.BlackPawn, board.ArrayBoard[i + 8], true);
                    moves.Add(move);
                }

                if ((moveOne & (1UL << i)) != 0)
                {
                    if (i < 8)
                    {
                        var promotionMoves = GeneratePromotionMoves(i + 8, i, board.ArrayBoard[i], false, false);
                        moves.AddRange(promotionMoves);
                    }
                    else
                    {
                        var move = new Move(i + 8, i, ChessPiece.BlackPawn);
                        moves.Add(move);
                    }
                }

                if ((moveTwo & (1UL << i)) != 0)
                {
                    var move = new Move(i + 16, i, ChessPiece.BlackPawn);
                    moves.Add(move);
                }
            }
            return moves;
        }

        private IList<Move> GeneratePromotionMoves(int from, int to, int takesPiece, bool enPassant, bool forWhite)
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
            var normalMoves = GetPotentialJumpingMoves(board, kings, Board.KingSpan, Board.KingSpanPosition, chessPiece);
            var castlingMoves = GetPotentialCastlingMoves(board);
            var allMoves = normalMoves.Union(castlingMoves).ToList();
            return allMoves;
        }

        public IList<Move> GetPotentialCastlingMoves(Board board)
        {
            var castlingMoves = new List<Move>();
            var isWhite = board.WhiteToMove;
            int kingPos;
            ulong queenSideCastleMask;
            ulong kingSideCastleMask;
            ulong queenSideCastleAttackMask;
            ulong kingSideCastleAttackMask;
            int piece;
            bool castlingPermissionQueenSide;
            bool castlingPermissionKingSide;

            if (isWhite)
            {
                castlingPermissionQueenSide = board.CastlingPermissions[CastlePermission.WhiteQueenSide];
                castlingPermissionKingSide = board.CastlingPermissions[CastlePermission.WhiteKingSide];
                kingPos = board.BitBoard[ChessPiece.WhiteKing].BitScanForward();
                queenSideCastleMask = Board.WhiteQueenSideCastleMask;
                kingSideCastleMask = Board.WhiteKingSideCastleMask;
                queenSideCastleAttackMask = Board.WhiteQueenSideCastleAttackMask;
                kingSideCastleAttackMask = Board.WhiteKingSideCastleAttackMask;
                piece = ChessPiece.WhiteKing;
            }
            else
            {
                castlingPermissionQueenSide = board.CastlingPermissions[CastlePermission.BlackQueenSide];
                castlingPermissionKingSide = board.CastlingPermissions[CastlePermission.BlackKingSide];
                kingPos = board.BitBoard[ChessPiece.BlackKing].BitScanForward();
                queenSideCastleMask = Board.BlackQueenSideCastleMask;
                kingSideCastleMask = Board.BlackKingSideCastleMask;
                queenSideCastleAttackMask = Board.BlackQueenSideCastleAttackMask;
                kingSideCastleAttackMask = Board.BlackKingSideCastleAttackMask;
                piece = ChessPiece.BlackKing;
            }

            var canMaybeCastleQueenSide = castlingPermissionQueenSide && ((board.AllPieces & queenSideCastleMask) == 0);
            var canMaybeCastleKingSide = castlingPermissionKingSide && (board.AllPieces & kingSideCastleMask) == 0;

            if (canMaybeCastleQueenSide | canMaybeCastleKingSide)
            {
                var attackedByEnemy = AttacksService.GetAllAttacked(board, !board.WhiteToMove);
                if (canMaybeCastleQueenSide && ((attackedByEnemy & queenSideCastleAttackMask) == 0))
                {
                    castlingMoves.Add(new Move(kingPos, kingPos - 2, piece));
                }
                if (canMaybeCastleKingSide && ((attackedByEnemy & kingSideCastleAttackMask) == 0))
                {
                    castlingMoves.Add(new Move(kingPos, kingPos + 2, piece));
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
            return GetPotentialJumpingMoves(board, knights, Board.KnightSpan, Board.KnightSpanPosition, chessPiece);
        }

        private IList<Move> GetPotentialJumpingMoves(Board board, ulong jumpingPieces, ulong jumpMask, int jumpMaskCenter, int piece)
        {
            var ownPieces = board.WhiteToMove ? board.WhitePieces : board.BlackPieces;
            var moves = new List<Move>();
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

                jumps &= ~(i%8 < 4 ? Board.Files[6] | Board.Files[7] : Board.Files[0] | Board.Files[1]);
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
            return GetPotentialSlidingPieceMoves(board, rooks, HyperbolaQuintessence.HorizontalVerticalSlide, chessPiece);
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
            return GetPotentialSlidingPieceMoves(board, bishops, HyperbolaQuintessence.DiagonalAntidiagonalSlide, chessPiece);
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
            return GetPotentialSlidingPieceMoves(board, bishops, HyperbolaQuintessence.AllSlide, chessPiece);
        }

        private IList<Move> GetPotentialSlidingPieceMoves(Board board, ulong slidingPieces, Func<Board, int, ulong> slideResolutionFunc, int piece)
        {
            var ownPieces = board.WhiteToMove ? board.WhitePieces : board.BlackPieces;
            var moves = new List<Move>();
            while (slidingPieces != 0)
            {
                var i = slidingPieces.BitScanForward();
                var slide = slideResolutionFunc.Invoke(board, i);
                slide &= ~ownPieces;
                foreach (var move in BitmaskToMoves(board, slide, i, piece))
                {
                    moves.Add(move);
                }
                slidingPieces &= ~(1UL << i);
            }
            return moves;
        }

        private IList<Move> FilterMovesByKingSafety(Board board, IEnumerable<Move> moves)
        {
            if (MultiThreadKingSafety)
            {
                return moves.AsParallel().Where(x => IsKingSafeAfterMove(board, x)).ToList();
            }
            else
            {
                return moves.Where(x => IsKingSafeAfterMove(board, x)).ToList();
            }
        }

        public Board DoMoveIfKingSafe(Board board, Move move)
        {
            var afterMoveBitBoards = board.DoMove(move);
            var enemyAttackedAfterMove = AttacksService.GetAllAttacked(afterMoveBitBoards);
            var myKings = board.WhiteToMove ? afterMoveBitBoards.BitBoard[ChessPiece.WhiteKing] : afterMoveBitBoards.BitBoard[ChessPiece.BlackKing];
            var isSafe = (enemyAttackedAfterMove & myKings) == 0;
            return isSafe ? afterMoveBitBoards : null;
        }

        public bool IsKingSafeAfterMove(Board board, Move move)
        {
            //return true;
            var afterMove = DoMoveIfKingSafe(board, move);
            return afterMove != null;
        }

        private static IList<Move> BitmaskToMoves(Board board, ulong bitmask, int positionFrom, int piece)
        {
            var moves = new List<Move>();
            for (var i = 0; i < 64; i++)
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
