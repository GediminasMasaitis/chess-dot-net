using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet
{
    public class PossibleMovesService
    {
        public bool MultiThreadKingSafety { get; set; }

        public PossibleMovesService()
        {
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
                    var move = new Move(i - 7, i, ChessPiece.WhitePawn);
                    moves.Add(move);
                }

                if ((takeRight & (1UL << i)) != 0)
                {
                    var move = new Move(i - 9, i, ChessPiece.WhitePawn);
                    moves.Add(move);
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
                    var move = new Move(i - 8, i, ChessPiece.WhitePawn);
                    moves.Add(move);
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
                    var move = new Move(i + 7, i, ChessPiece.BlackPawn);
                    moves.Add(move);
                }

                if ((takeRight & (1UL << i)) != 0)
                {
                    var move = new Move(i + 9, i, ChessPiece.BlackPawn);
                    moves.Add(move);
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
                    var move = new Move(i + 8, i, ChessPiece.BlackPawn);
                    moves.Add(move);
                }

                if ((moveTwo & (1UL << i)) != 0)
                {
                    var move = new Move(i + 16, i, ChessPiece.BlackPawn);
                    moves.Add(move);
                }
            }
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
            for (var i = 0; i < 64; i++)
            {
                if ((jumpingPieces & (1UL << i)) != 0)
                {
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
                }
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
            return GetPotentialSlidingPieceMoves(bitBoards, rooks, HorizontalVerticalSlide, forWhite, chessPiece);
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
            return GetPotentialSlidingPieceMoves(bitBoards, bishops, DiagonalAntidiagonalSlide, forWhite, chessPiece);
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
            return GetPotentialSlidingPieceMoves(bitBoards, bishops, AllSlide, forWhite, chessPiece);
        }

        private IList<Move> GetPotentialSlidingPieceMoves(BitBoards bitBoards, ulong slidingPieces, Func<BitBoards, int, ulong> slideResolutionFunc, bool forWhite, ChessPiece piece)
        {
            var ownPieces = forWhite ? bitBoards.WhitePieces : bitBoards.BlackPieces;
            var moves = new List<Move>();
            for (var i = 0; i < 64; i++)
            {
                if ((slidingPieces & (1UL << i)) != 0)
                //if ((slidingPieces & (1UL << i)) > 0)
                {
                    var slide = slideResolutionFunc.Invoke(bitBoards, i);
                    slide &= ~ownPieces;
                    foreach (var move in BitmaskToMoves(slide, i, piece))
                    {
                        moves.Add(move);
                    }
                }
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

        private bool IsKingSafeAfterMove(BitBoards bitBoards, Move move, bool forWhite)
        {
            //return true;
            var afterMoveBitBoards = bitBoards.DoMove(move);
            var enemyAttackedAfterMove = GetAllAttacked(afterMoveBitBoards, !forWhite);
            var myKings = forWhite ? afterMoveBitBoards.WhiteKings : afterMoveBitBoards.BlackKings;
            var isSafe = (enemyAttackedAfterMove & myKings) == 0;
            return isSafe;
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

        public ulong GetAllAttacked(BitBoards bitBoards, bool forWhite)
        {
            var pawnsAttack = GetAttackedByPawns(bitBoards, forWhite);
            var knightsAttack = GetAttackedByKnights(bitBoards, forWhite);

            //var bishopsAttack = GetAttackedByBishops(bitBoards, forWhite);
            //var rooksAttack = GetAttackedByRooks(bitBoards, forWhite);
            //var queensAttack = GetAttackedByQueens(bitBoards, forWhite);

            var bq = forWhite ? bitBoards.WhiteBishops | bitBoards.WhiteQueens : bitBoards.BlackBishops | bitBoards.BlackQueens;
            var bqAttack = GetAttackedBySlidingPieces(bitBoards, bq, DiagonalAntidiagonalSlide);

            var rq = forWhite ? bitBoards.WhiteRooks | bitBoards.WhiteQueens : bitBoards.BlackRooks | bitBoards.BlackQueens;
            var rqAttack = GetAttackedBySlidingPieces(bitBoards, rq, HorizontalVerticalSlide);

            var kingsAttack = GetAttackedByKings(bitBoards, forWhite);

            var allAttacked = pawnsAttack | knightsAttack | bqAttack | rqAttack | kingsAttack;
            return allAttacked;
        }

        public ulong GetAttackedByBishops(BitBoards bitBoards, bool forWhite)
        {
            var bishops = forWhite ? bitBoards.WhiteBishops : bitBoards.BlackBishops;
            return GetAttackedBySlidingPieces(bitBoards, bishops, DiagonalAntidiagonalSlide);
        }

        public ulong GetAttackedByRooks(BitBoards bitBoards, bool forWhite)
        {
            var rooks = forWhite ? bitBoards.WhiteRooks : bitBoards.BlackRooks;
            return GetAttackedBySlidingPieces(bitBoards, rooks, HorizontalVerticalSlide);
        }

        public ulong GetAttackedByQueens(BitBoards bitBoards, bool forWhite)
        {
            var queens = forWhite ? bitBoards.WhiteQueens : bitBoards.BlackQueens;
            return GetAttackedBySlidingPieces(bitBoards, queens, AllSlide);
        }

        private ulong GetAttackedBySlidingPieces(BitBoards bitBoards, ulong slidingPieces, Func<BitBoards, int, ulong> slideResolutionFunc)
        {
            var allSlide = 0UL;
            while(slidingPieces != 0)
            {
                var i = slidingPieces.BitScanForward();
                var slide = slideResolutionFunc.Invoke(bitBoards, i);
                allSlide |= slide;
                slidingPieces &= ~(1UL << i);
            }
            return allSlide;
        }

        [Obsolete]
        private ulong GetAttackedBySlidingPiecesOld(BitBoards bitBoards, ulong slidingPieces, Func<BitBoards, int, ulong> slideResolutionFunc)
        {
            var allSlide = 0UL;
            for (var i = 0; i < 64; i++)
            {
                if ((slidingPieces & (1UL << i)) > 0)
                {
                    var slide = slideResolutionFunc.Invoke(bitBoards, i);
                    allSlide |= slide;
                }
            }
            return allSlide;
        }

        public ulong GetAttackedByKings(BitBoards bitBoards, bool forWhite)
        {
            var kings = forWhite ? bitBoards.WhiteKings : bitBoards.BlackKings;
            return GetAttackedByJumpingPieces(bitBoards, kings, BitBoards.KingSpan, BitBoards.KingSpanPosition);
        }

        public ulong GetAttackedByKnights(BitBoards bitBoards, bool forWhite)
        {
            var knights = forWhite ? bitBoards.WhiteNights : bitBoards.BlackNights;
            return GetAttackedByJumpingPieces(bitBoards, knights, BitBoards.KnightSpan, BitBoards.KnightSpanPosition);
        }

        public ulong GetAttackedByPawns(BitBoards bitBoards, bool forWhite)
        {
            ulong pawnsLeft;
            ulong pawnsRight;
            if (forWhite)
            {
                pawnsLeft = (bitBoards.WhitePawns << 7) & ~BitBoards.Files[7];
                pawnsRight = (bitBoards.WhitePawns << 9) & ~BitBoards.Files[0];
            }
            else
            {
                pawnsLeft = (bitBoards.BlackPawns >> 7) & ~BitBoards.Files[0];
                pawnsRight = (bitBoards.BlackPawns >> 9) & ~BitBoards.Files[7];
            }
            return pawnsLeft | pawnsRight;
        }

        private ulong GetAttackedByJumpingPieces(BitBoards bitBoards, ulong jumpingPieces, ulong jumpMask, int jumpMaskCenter)
        {
            ulong allJumps = 0;
            while(jumpingPieces != 0)
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

                jumps &= ~(i % 8 < 4 ? BitBoards.Files[6] | BitBoards.Files[7] : BitBoards.Files[0] | BitBoards.Files[1]);
                allJumps |= jumps;
                jumpingPieces &= ~(1UL << i);
            }
            return allJumps;
        }

        [Obsolete]
        private ulong GetAttackedByJumpingPiecesOld(BitBoards bitBoards, ulong jumpingPieces, ulong jumpMask, int jumpMaskCenter)
        {
            ulong allJumps = 0;
            for (var i = 0; i < 64; i++)
            {
                if ((jumpingPieces & (1UL << i)) != 0)
                {
                    ulong jumps;
                    if (i > jumpMaskCenter)
                    {
                        jumps = jumpMask << (i - jumpMaskCenter);
                    }
                    else
                    {
                        jumps = jumpMask >> (jumpMaskCenter - i);
                    }

                    jumps &= ~(i % 8 < 4 ? BitBoards.Files[6] | BitBoards.Files[7] : BitBoards.Files[0] | BitBoards.Files[1]);
                    allJumps |= jumps;
                }
            }
            return allJumps;
        }

        private ulong AllSlide(BitBoards bitboards, int position)
        {
            var hv = HorizontalVerticalSlide(bitboards, position);
            var dad = DiagonalAntidiagonalSlide(bitboards, position);
            return hv | dad;
        }

        private ulong HorizontalVerticalSlide(BitBoards bitboards, int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(bitboards, pieceBitboard, BitBoards.Ranks[position/8]);
            var vertical = MaskedSlide(bitboards, pieceBitboard, BitBoards.Files[position%8]);
            return horizontal | vertical;
        }

        private ulong DiagonalAntidiagonalSlide(BitBoards bitboards, int position)
        {
            var pieceBitboard = 1UL << position;
            var horizontal = MaskedSlide(bitboards, pieceBitboard, BitBoards.Diagonals[position/8 + position%8]);
            var vertical = MaskedSlide(bitboards, pieceBitboard, BitBoards.Antidiagonals[position/8 + 7 - position%8]);
            return horizontal | vertical;
        }

        private ulong MaskedSlide(BitBoards bitboards, ulong pieceBitboard, ulong mask)
        {
            var left = ((bitboards.FilledSquares & mask) - 2 * pieceBitboard);
            var right = ((bitboards.FilledSquares & mask).Reverse() - 2 * pieceBitboard.Reverse()).Reverse();
            var both = left ^ right;
            var slide = both & mask;
            return slide;
        }
    }
}
