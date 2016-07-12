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

        public IList<Move> GetAllPossibleMoves(BitBoards bitBoards)
        {
            var potentialMoves = GetAllPotentialMoves(bitBoards);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetAllPotentialMoves(BitBoards bitBoards)
        {
            var pawnMoves = GetPotentialPawnMoves(bitBoards);
            var knightMoves = GetPotentialKnightMoves(bitBoards);
            var bishopMoves = GetPotentialBishopMoves(bitBoards);
            var rookMoves = GetPotentialRookMoves(bitBoards);
            var queenMoves = GetPotentialQueenMoves(bitBoards);
            var kingMoves = GetPotentialKingMoves(bitBoards);

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

        public IList<Move> GetPossiblePawnMoves(BitBoards bitBoards)
        {
            var potentialMoves = GetPotentialPawnMoves(bitBoards);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialPawnMoves(BitBoards bitBoards)
        {
            return bitBoards.WhiteToMove ? GetPotentialWhitePawnMoves(bitBoards) : GetPotentialBlackPawnMoves(bitBoards);
        }

        private IList<Move> GetPotentialWhitePawnMoves(BitBoards bitBoards)
        {
            var takeLeft = (bitBoards.WhitePawns << 7) & ~BitBoards.Files[7] & bitBoards.BlackPieces;
            var takeRight = (bitBoards.WhitePawns << 9) & ~BitBoards.Files[0] & bitBoards.BlackPieces;

            var enPassantLeft = (bitBoards.WhitePawns << 7) & ~BitBoards.Files[7] & bitBoards.EnPassantFile & BitBoards.Ranks[5] & bitBoards.BlackPawns << 8;
            var enPassantRight = (bitBoards.WhitePawns << 9) & ~BitBoards.Files[0] & bitBoards.EnPassantFile & BitBoards.Ranks[5] & bitBoards.BlackPawns << 8;

            var moveOne = (bitBoards.WhitePawns << 8) & bitBoards.EmptySquares;
            var moveTwo = (bitBoards.WhitePawns << 16) & bitBoards.EmptySquares & bitBoards.EmptySquares << 8 & BitBoards.Ranks[3];

            var moves = new List<Move>();

            for (byte i = 0; i < 64; i++)
            {
                if ((takeLeft & (1UL << i)) != 0)
                {
                    if (i > 55)
                    {
                        var promotionMoves = GeneratePromotionMoves(i - 7, i, false, true);
                        moves.AddRange(promotionMoves);
                    }
                    else
                    {
                        var move = new Move(i - 7, i, ChessPiece.WhitePawn);
                        moves.Add(move);
                    }
                }

                if ((takeRight & (1UL << i)) != 0)
                {
                    if (i > 55)
                    {
                        var promotionMoves = GeneratePromotionMoves(i - 9, i, false, true);
                        moves.AddRange(promotionMoves);
                    }
                    else
                    {
                        var move = new Move(i - 9, i, ChessPiece.WhitePawn);
                        moves.Add(move);
                    }
                }

                if ((enPassantLeft & (1UL << i)) != 0)
                {
                    var move = new Move(i - 7, i, ChessPiece.WhitePawn, true);
                    moves.Add(move);
                }

                if ((enPassantRight & (1UL << i)) != 0)
                {
                    var move = new Move(i - 9, i, ChessPiece.WhitePawn, true);
                    moves.Add(move);
                }

                if ((moveOne & (1UL << i)) != 0)
                {
                    if (i > 55)
                    {
                        var promotionMoves = GeneratePromotionMoves(i - 8, i, false, true);
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

        private IList<Move> GetPotentialBlackPawnMoves(BitBoards bitBoards)
        {
            var takeLeft = (bitBoards.BlackPawns >> 7) & ~BitBoards.Files[0] & bitBoards.WhitePieces;
            var takeRight = (bitBoards.BlackPawns >> 9) & ~BitBoards.Files[7] & bitBoards.WhitePieces;

            var enPassantLeft = (bitBoards.BlackPawns >> 7) & ~BitBoards.Files[0] & bitBoards.EnPassantFile & BitBoards.Ranks[2] & bitBoards.WhitePawns >> 8;
            var enPassantRight = (bitBoards.BlackPawns >> 9) & ~BitBoards.Files[7] & bitBoards.EnPassantFile & BitBoards.Ranks[2] & bitBoards.WhitePawns >> 8;

            var moveOne = (bitBoards.BlackPawns >> 8) & bitBoards.EmptySquares;
            var moveTwo = (bitBoards.BlackPawns >> 16) & bitBoards.EmptySquares & bitBoards.EmptySquares >> 8 & BitBoards.Ranks[4];

            var moves = new List<Move>();

            for (byte i = 0; i < 64; i++)
            {
                if ((takeLeft & (1UL << i)) != 0)
                {
                    if (i < 8)
                    {
                        var promotionMoves = GeneratePromotionMoves(i + 7, i, false, false);
                        moves.AddRange(promotionMoves);
                    }
                    else
                    {
                        var move = new Move(i + 7, i, ChessPiece.BlackPawn);
                        moves.Add(move);
                    }
                }

                if ((takeRight & (1UL << i)) != 0)
                {
                    if (i < 8)
                    {
                        var promotionMoves = GeneratePromotionMoves(i + 9, i, false, false);
                        moves.AddRange(promotionMoves);
                    }
                    else
                    {
                        var move = new Move(i + 9, i, ChessPiece.BlackPawn);
                        moves.Add(move);
                    }
                }

                if ((enPassantLeft & (1UL << i)) != 0)
                {
                    var move = new Move(i + 7, i, ChessPiece.BlackPawn, true);
                    moves.Add(move);
                }

                if ((enPassantRight & (1UL << i)) != 0)
                {
                    var move = new Move(i + 9, i, ChessPiece.BlackPawn, true);
                    moves.Add(move);
                }

                if ((moveOne & (1UL << i)) != 0)
                {
                    if (i < 8)
                    {
                        var promotionMoves = GeneratePromotionMoves(i + 8, i, false, false);
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

        private IList<Move> GeneratePromotionMoves(int from, int to, bool enPassant, bool forWhite)
        {
            var moves = new List<Move>
            {
                new Move(from, to, forWhite ? ChessPiece.WhitePawn : ChessPiece.BlackPawn, enPassant, forWhite ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight),
                new Move(from, to, forWhite ? ChessPiece.WhitePawn : ChessPiece.BlackPawn, enPassant, forWhite ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop),
                new Move(from, to, forWhite ? ChessPiece.WhitePawn : ChessPiece.BlackPawn, enPassant, forWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook),
                new Move(from, to, forWhite ? ChessPiece.WhitePawn : ChessPiece.BlackPawn, enPassant, forWhite ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen),
            };
            return moves;
        }

        public IList<Move> GetPossibleKingMoves(BitBoards bitBoards)
        {
            var potentialMoves = GetPotentialKingMoves(bitBoards);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialKingMoves(BitBoards bitBoards)
        {
            var kings = bitBoards.WhiteToMove ? bitBoards.WhiteKings : bitBoards.BlackKings;
            var chessPiece = bitBoards.WhiteToMove ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
            var normalMoves = GetPotentialJumpingMoves(bitBoards, kings, BitBoards.KingSpan, BitBoards.KingSpanPosition, chessPiece);
            var castlingMoves = GetPotentialCastlingMoves(bitBoards);
            var allMoves = normalMoves.Union(castlingMoves).ToList();
            return allMoves;
        }

        public IList<Move> GetPotentialCastlingMoves(BitBoards bitBoards)
        {
            var castlingMoves = new List<Move>();
            var isWhite = bitBoards.WhiteToMove;
            int kingPos;
            ulong queenSideCastleMask;
            ulong kingSideCastleMask;
            ulong queenSideCastleAttackMask;
            ulong kingSideCastleAttackMask;
            ChessPiece piece;
            bool castlingPermissionQueenSide;
            bool castlingPermissionKingSide;

            if (isWhite)
            {
                castlingPermissionQueenSide = bitBoards.WhiteCanCastleQueenSide;
                castlingPermissionKingSide = bitBoards.WhiteCanCastleKingSide;
                kingPos = bitBoards.WhiteKings.BitScanForward();
                queenSideCastleMask = BitBoards.WhiteQueenSideCastleMask;
                kingSideCastleMask = BitBoards.WhiteKingSideCastleMask;
                queenSideCastleAttackMask = BitBoards.WhiteQueenSideCastleAttackMask;
                kingSideCastleAttackMask = BitBoards.WhiteKingSideCastleAttackMask;
                piece = ChessPiece.WhiteKing;
            }
            else
            {
                castlingPermissionQueenSide = bitBoards.BlackCanCastleQueenSide;
                castlingPermissionKingSide = bitBoards.BlackCanCastleKingSide;
                kingPos = bitBoards.BlackKings.BitScanForward();
                queenSideCastleMask = BitBoards.BlackQueenSideCastleMask;
                kingSideCastleMask = BitBoards.BlackKingSideCastleMask;
                queenSideCastleAttackMask = BitBoards.BlackQueenSideCastleAttackMask;
                kingSideCastleAttackMask = BitBoards.BlackKingSideCastleAttackMask;
                piece = ChessPiece.BlackKing;
            }

            var canMaybeCastleQueenSide = castlingPermissionQueenSide && ((bitBoards.AllPieces & queenSideCastleMask) == 0);
            var canMaybeCastleKingSide = castlingPermissionKingSide && (bitBoards.AllPieces & kingSideCastleMask) == 0;

            if (canMaybeCastleQueenSide | canMaybeCastleKingSide)
            {
                var attackedByEnemy = AttacksService.GetAllAttacked(bitBoards, !bitBoards.WhiteToMove);
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

        public IList<Move> GetPossibleKnightMoves(BitBoards bitBoards)
        {
            var potentialMoves = GetPotentialKnightMoves(bitBoards);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialKnightMoves(BitBoards bitBoards)
        {
            var knights = bitBoards.WhiteToMove ? bitBoards.WhiteNights : bitBoards.BlackNights;
            var chessPiece = bitBoards.WhiteToMove ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight;
            return GetPotentialJumpingMoves(bitBoards, knights, BitBoards.KnightSpan, BitBoards.KnightSpanPosition, chessPiece);
        }

        private IList<Move> GetPotentialJumpingMoves(BitBoards bitBoards, ulong jumpingPieces, ulong jumpMask, int jumpMaskCenter, ChessPiece piece)
        {
            var ownPieces = bitBoards.WhiteToMove ? bitBoards.WhitePieces : bitBoards.BlackPieces;
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

                jumps &= ~(i%8 < 4 ? BitBoards.Files[6] | BitBoards.Files[7] : BitBoards.Files[0] | BitBoards.Files[1]);
                jumps &= ~ownPieces;

                foreach (var move in BitmaskToMoves(jumps, i, piece))
                {
                    moves.Add(move);
                }

                jumpingPieces &= ~(1UL << i);
            }
            return moves;
        }

        public IList<Move> GetPossibleRookMoves(BitBoards bitBoards)
        {
            var potentialMoves = GetPotentialRookMoves(bitBoards);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialRookMoves(BitBoards bitBoards)
        {
            var rooks = bitBoards.WhiteToMove ? bitBoards.WhiteRooks : bitBoards.BlackRooks;
            var chessPiece = bitBoards.WhiteToMove ? ChessPiece.WhiteRook : ChessPiece.BlackRook;
            return GetPotentialSlidingPieceMoves(bitBoards, rooks, HyperbolaQuintessence.HorizontalVerticalSlide, chessPiece);
        }

        public IList<Move> GetPossibleBishopMoves(BitBoards bitBoards)
        {
            var potentialMoves = GetPotentialBishopMoves(bitBoards);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialBishopMoves(BitBoards bitBoards)
        {
            var bishops = bitBoards.WhiteToMove ? bitBoards.WhiteBishops : bitBoards.BlackBishops;
            var chessPiece = bitBoards.WhiteToMove ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop;
            return GetPotentialSlidingPieceMoves(bitBoards, bishops, HyperbolaQuintessence.DiagonalAntidiagonalSlide, chessPiece);
        }

        public IList<Move> GetPossibleQueenMoves(BitBoards bitBoards)
        {
            var potentialMoves = GetPotentialQueenMoves(bitBoards);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves);
            return validMoves;
        }

        public IList<Move> GetPotentialQueenMoves(BitBoards bitBoards)
        {
            var bishops = bitBoards.WhiteToMove ? bitBoards.WhiteQueens : bitBoards.BlackQueens;
            var chessPiece = bitBoards.WhiteToMove ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
            return GetPotentialSlidingPieceMoves(bitBoards, bishops, HyperbolaQuintessence.AllSlide, chessPiece);
        }

        private IList<Move> GetPotentialSlidingPieceMoves(BitBoards bitBoards, ulong slidingPieces, Func<BitBoards, int, ulong> slideResolutionFunc, ChessPiece piece)
        {
            var ownPieces = bitBoards.WhiteToMove ? bitBoards.WhitePieces : bitBoards.BlackPieces;
            var moves = new List<Move>();
            while (slidingPieces != 0)
            {
                var i = slidingPieces.BitScanForward();
                var slide = slideResolutionFunc.Invoke(bitBoards, i);
                slide &= ~ownPieces;
                foreach (var move in BitmaskToMoves(slide, i, piece))
                {
                    moves.Add(move);
                }
                slidingPieces &= ~(1UL << i);
            }
            return moves;
        }

        private IList<Move> FilterMovesByKingSafety(BitBoards bitBoards, IEnumerable<Move> moves)
        {
            if (MultiThreadKingSafety)
            {
                return moves.AsParallel().Where(x => IsKingSafeAfterMove(bitBoards, x)).ToList();
            }
            else
            {
                return moves.Where(x => IsKingSafeAfterMove(bitBoards, x)).ToList();
            }
        }

        public BitBoards DoMoveIfKingSafe(BitBoards bitBoards, Move move)
        {
            var afterMoveBitBoards = bitBoards.DoMove(move);
            var enemyAttackedAfterMove = AttacksService.GetAllAttacked(afterMoveBitBoards);
            var myKings = bitBoards.WhiteToMove ? afterMoveBitBoards.WhiteKings : afterMoveBitBoards.BlackKings;
            var isSafe = (enemyAttackedAfterMove & myKings) == 0;
            return isSafe ? afterMoveBitBoards : null;
        }

        public bool IsKingSafeAfterMove(BitBoards bitBoards, Move move)
        {
            var afterMove = DoMoveIfKingSafe(bitBoards, move);
            return afterMove != null;
        }

        private static IList<Move> BitmaskToMoves(ulong bitmask, int positionFrom, ChessPiece piece)
        {
            var moves = new List<Move>();
            for (var j = 0; j < 64; j++)
            {
                if ((bitmask & (1UL << j)) != 0)
                {
                    var move = new Move(positionFrom, j, piece);
                    moves.Add(move);
                }
            }
            return moves;
        }
    }
}
