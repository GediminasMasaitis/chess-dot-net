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

        public IList<Move> GetAllPossibleMoves(BitBoards bitBoards, bool forWhite)
        {
            var potentialMoves = GetAllPotentialMoves(bitBoards, forWhite);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves, forWhite);
            return validMoves;
        }

        public IList<Move> GetAllPotentialMoves(BitBoards bitBoards, bool forWhite)
        {
            var pawnMoves = GetPotentialPawnMoves(bitBoards, forWhite);
            var knightMoves = GetPotentialKnightMoves(bitBoards, forWhite);
            var bishopMoves = GetPotentialBishopMoves(bitBoards, forWhite);
            var rookMoves = GetPotentialRookMoves(bitBoards, forWhite);
            var queenMoves = GetPotentialQueenMoves(bitBoards, forWhite);
            var kingMoves = GetPotentialKingMoves(bitBoards, forWhite);

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

        public IList<Move> GetPossiblePawnMoves(BitBoards bitBoards, bool forWhite)
        {
            var potentialMoves = GetPotentialPawnMoves(bitBoards, forWhite);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves, forWhite);
            return validMoves;
        }

        public IList<Move> GetPotentialPawnMoves(BitBoards bitBoards, bool forWhite)
        {
            return forWhite ? GetPotentialWhitePawnMoves(bitBoards) : GetPotentialBlackPawnMoves(bitBoards);
        }

        private IList<Move> GetPotentialWhitePawnMoves(BitBoards bitBoards)
        {
            var takeLeft = (bitBoards.WhitePawns << 7) & ~BitBoards.Files[7] & bitBoards.BlackPieces;
            var takeRight = (bitBoards.WhitePawns << 9) & ~BitBoards.Files[0] & bitBoards.BlackPieces;

            var enPassantLeft = (bitBoards.WhitePawns << 7) & ~BitBoards.Files[7] & bitBoards.EnPassantFile & bitBoards.BlackPawns << 8;
            var enPassantRight = (bitBoards.WhitePawns << 9) & ~BitBoards.Files[0] & bitBoards.EnPassantFile & bitBoards.BlackPawns << 8;
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

            var enPassantLeft = (bitBoards.BlackPawns >> 7) & ~BitBoards.Files[0] & bitBoards.EnPassantFile & bitBoards.WhitePawns >> 8;
            var enPassantRight = (bitBoards.BlackPawns >> 9) & ~BitBoards.Files[7] & bitBoards.EnPassantFile & bitBoards.WhitePawns >> 8;

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

        public IList<Move> GetPossibleKingMoves(BitBoards bitBoards, bool forWhite)
        {
            var potentialMoves = GetPotentialKingMoves(bitBoards, forWhite);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves, forWhite);
            return validMoves;
        }

        public IList<Move> GetPotentialKingMoves(BitBoards bitBoards, bool forWhite)
        {
            var kings = forWhite ? bitBoards.WhiteKings : bitBoards.BlackKings;
            var chessPiece = forWhite ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
            return GetPotentialJumpingMoves(bitBoards, kings, BitBoards.KingSpan, BitBoards.KingSpanPosition, forWhite, chessPiece);
        }

        public IList<Move> GetPossibleKnightMoves(BitBoards bitBoards, bool forWhite)
        {
            var potentialMoves = GetPotentialKnightMoves(bitBoards, forWhite);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves, forWhite);
            return validMoves;
        }

        public IList<Move> GetPotentialKnightMoves(BitBoards bitBoards, bool forWhite)
        {
            var knights = forWhite ? bitBoards.WhiteNights : bitBoards.BlackNights;
            var chessPiece = forWhite ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight;
            return GetPotentialJumpingMoves(bitBoards, knights, BitBoards.KnightSpan, BitBoards.KnightSpanPosition, forWhite, chessPiece);
        }

        private IList<Move> GetPotentialJumpingMoves(BitBoards bitBoards, ulong jumpingPieces, ulong jumpMask, int jumpMaskCenter, bool forWhite, ChessPiece piece)
        {
            var ownPieces = forWhite ? bitBoards.WhitePieces : bitBoards.BlackPieces;
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

        public IList<Move> GetPossibleRookMoves(BitBoards bitBoards, bool forWhite)
        {
            var potentialMoves = GetPotentialRookMoves(bitBoards, forWhite);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves, forWhite);
            return validMoves;
        }

        public IList<Move> GetPotentialRookMoves(BitBoards bitBoards, bool forWhite)
        {
            var rooks = forWhite ? bitBoards.WhiteRooks : bitBoards.BlackRooks;
            var chessPiece = forWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook;
            return GetPotentialSlidingPieceMoves(bitBoards, rooks, HyperbolaQuintessence.HorizontalVerticalSlide, forWhite, chessPiece);
        }

        public IList<Move> GetPossibleBishopMoves(BitBoards bitBoards, bool forWhite)
        {
            var potentialMoves = GetPotentialBishopMoves(bitBoards, forWhite);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves, forWhite);
            return validMoves;
        }

        public IList<Move> GetPotentialBishopMoves(BitBoards bitBoards, bool forWhite)
        {
            var bishops = forWhite ? bitBoards.WhiteBishops : bitBoards.BlackBishops;
            var chessPiece = forWhite ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop;
            return GetPotentialSlidingPieceMoves(bitBoards, bishops, HyperbolaQuintessence.DiagonalAntidiagonalSlide, forWhite, chessPiece);
        }

        public IList<Move> GetPossibleQueenMoves(BitBoards bitBoards, bool forWhite)
        {
            var potentialMoves = GetPotentialQueenMoves(bitBoards, forWhite);
            var validMoves = FilterMovesByKingSafety(bitBoards, potentialMoves, forWhite);
            return validMoves;
        }

        public IList<Move> GetPotentialQueenMoves(BitBoards bitBoards, bool forWhite)
        {
            var bishops = forWhite ? bitBoards.WhiteQueens : bitBoards.BlackQueens;
            var chessPiece = forWhite ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
            return GetPotentialSlidingPieceMoves(bitBoards, bishops, HyperbolaQuintessence.AllSlide, forWhite, chessPiece);
        }

        private IList<Move> GetPotentialSlidingPieceMoves(BitBoards bitBoards, ulong slidingPieces, Func<BitBoards, int, ulong> slideResolutionFunc, bool forWhite, ChessPiece piece)
        {
            var ownPieces = forWhite ? bitBoards.WhitePieces : bitBoards.BlackPieces;
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

        private IList<Move> FilterMovesByKingSafety(BitBoards bitBoards, IEnumerable<Move> moves, bool forWhite)
        {
            if (MultiThreadKingSafety)
            {
                return moves.AsParallel().Where(x => IsKingSafeAfterMove(bitBoards, x, forWhite)).ToList();
            }
            else
            {
                return moves.Where(x => IsKingSafeAfterMove(bitBoards, x, forWhite)).ToList();
            }
        }

        public BitBoards DoMoveIfKingSafe(BitBoards bitBoards, Move move, bool forWhite)
        {
            var afterMoveBitBoards = bitBoards.DoMove(move);
            var enemyAttackedAfterMove = AttacksService.GetAllAttacked(afterMoveBitBoards, !forWhite);
            var myKings = forWhite ? afterMoveBitBoards.WhiteKings : afterMoveBitBoards.BlackKings;
            var isSafe = (enemyAttackedAfterMove & myKings) == 0;
            return isSafe ? afterMoveBitBoards : null;
        }

        public bool IsKingSafeAfterMove(BitBoards bitBoards, Move move, bool forWhite)
        {
            var afterMove = DoMoveIfKingSafe(bitBoards, move, forWhite);
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
