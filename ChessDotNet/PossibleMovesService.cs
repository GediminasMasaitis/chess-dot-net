using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet
{
    public class PossibleMovesService
    {
        private BitBoards BitBoards { get; }
        public ulong AttackedByWhite { get; private set; }
        public ulong AttackedByBlack { get; private set; }

        public PossibleMovesService(BitBoards bitBoards)
        {
            BitBoards = bitBoards;
        }

        public IEnumerable<Move> GetAllPossibleMoves(BitBoards bitBoards, bool isWhite)
        {
            var pawnMoves = GetPossiblePawnMoves(bitBoards, isWhite);
            var knightMoves = GetPossibleKnightMoves(bitBoards, isWhite);
            var bishopMoves = GetPossibleBishopMoves(bitBoards, isWhite);
            var rookMoves = GetPossibleRookMoves(bitBoards, isWhite);
            var queenMoves = GetPossibleQueenMoves(bitBoards, isWhite);
            var kingMoves = GetPossibleKingMoves(bitBoards, isWhite);

            //var allMoves = new List<Move>();
            //allMoves.AddRange(pawnMoves);
            //allMoves.AddRange(knightMoves);
            //allMoves.AddRange(bishopMoves);
            //allMoves.AddRange(rookMoves);
            //allMoves.AddRange(queenMoves);
            //allMoves.AddRange(kingMoves);

            var allMoves = pawnMoves.Concat(knightMoves).Concat(bishopMoves).Concat(rookMoves).Concat(queenMoves).Concat(kingMoves);
            return allMoves;
        }

        public IEnumerable<Move> GetPossiblePawnMoves(BitBoards bitBoards, bool isWhite)
        {
            return isWhite ? GetPossibleWhitePawnMoves(bitBoards) : GetPossibleBlackPawnMoves(bitBoards);
        }

        private IEnumerable<Move> GetPossibleWhitePawnMoves(BitBoards bitBoards)
        {
            var takeLeft = (bitBoards.WhitePawns << 7) & ~BitBoards.Files[7] & bitBoards.BlackPieces;
            var takeRight = (bitBoards.WhitePawns << 9) & ~BitBoards.Files[0] & bitBoards.BlackPieces;

            var enPassantLeft = (bitBoards.WhitePawns << 7) & ~BitBoards.Files[7] & bitBoards.EnPassantFile & bitBoards.BlackPawns << 8;
            var enPassantRight = (bitBoards.WhitePawns << 9) & ~BitBoards.Files[0] & bitBoards.EnPassantFile & bitBoards.BlackPawns << 8;
            var moveOne = (bitBoards.WhitePawns << 8) & bitBoards.EmptySquares;
            var moveTwo = (bitBoards.WhitePawns << 16) & bitBoards.EmptySquares & bitBoards.EmptySquares << 8 & BitBoards.Ranks[3];

            for (byte i = 0; i < 64; i++)
            {
                if ((takeLeft & (1UL << i)) != 0)
                {
                    var move = new Move(i - 7, i, ChessPiece.WhitePawn);
                    if (IsKingSafeAfterMove(bitBoards, move, true))
                    {
                        yield return move;
                    }
                }

                if ((takeRight & (1UL << i)) != 0)
                {
                    var move = new Move(i - 9, i, ChessPiece.WhitePawn);
                    if (IsKingSafeAfterMove(bitBoards, move, true))
                    {
                        yield return move;
                    }
                }

                if ((enPassantLeft & (1UL << i)) != 0)
                {
                    var move = new Move(i - 7, i, ChessPiece.WhitePawn, true);
                    if (IsKingSafeAfterMove(bitBoards, move, true))
                    {
                        yield return move;
                    }
                }

                if ((enPassantRight & (1UL << i)) != 0)
                {
                    var move = new Move(i - 9, i, ChessPiece.WhitePawn, true);
                    if (IsKingSafeAfterMove(bitBoards, move, true))
                    {
                        yield return move;
                    }
                }

                if ((moveOne & (1UL << i)) != 0)
                {
                    var move = new Move(i - 8, i, ChessPiece.WhitePawn);
                    if (IsKingSafeAfterMove(bitBoards, move, true))
                    {
                        yield return move;
                    }
                }

                if ((moveTwo & (1UL << i)) != 0)
                {
                    var move = new Move(i - 16, i, ChessPiece.WhitePawn);
                    if (IsKingSafeAfterMove(bitBoards, move, true))
                    {
                        yield return move;
                    }
                }
            }
        }

        private IEnumerable<Move> GetPossibleBlackPawnMoves(BitBoards bitBoards)
        {
            var takeLeft = (bitBoards.BlackPawns >> 7) & ~BitBoards.Files[0] & bitBoards.WhitePieces;
            var takeRight = (bitBoards.BlackPawns >> 9) & ~BitBoards.Files[7] & bitBoards.WhitePieces;

            var enPassantLeft = (bitBoards.BlackPawns >> 7) & ~BitBoards.Files[0] & bitBoards.EnPassantFile & bitBoards.WhitePawns >> 8;
            var enPassantRight = (bitBoards.BlackPawns >> 9) & ~BitBoards.Files[7] & bitBoards.EnPassantFile & bitBoards.WhitePawns >> 8;

            var moveOne = (bitBoards.BlackPawns >> 8) & bitBoards.EmptySquares;
            var moveTwo = (bitBoards.BlackPawns >> 16) & bitBoards.EmptySquares & bitBoards.EmptySquares >> 8 & BitBoards.Ranks[4];

            for (byte i = 0; i < 64; i++)
            {
                if ((takeLeft & (1UL << i)) != 0)
                {
                    var move = new Move(i + 7, i, ChessPiece.BlackPawn);
                    if (IsKingSafeAfterMove(bitBoards, move, false))
                    {
                        yield return move;
                    }
                }

                if ((takeRight & (1UL << i)) != 0)
                {
                    var move = new Move(i + 9, i, ChessPiece.BlackPawn);
                    if (IsKingSafeAfterMove(bitBoards, move, false))
                    {
                        yield return move;
                    }
                }

                if ((enPassantLeft & (1UL << i)) != 0)
                {
                    var move = new Move(i + 7, i, ChessPiece.BlackPawn, true);
                    if (IsKingSafeAfterMove(bitBoards, move, false))
                    {
                        yield return move;
                    }
                }

                if ((enPassantRight & (1UL << i)) != 0)
                {
                    var move = new Move(i + 9, i, ChessPiece.BlackPawn, true);
                    if (IsKingSafeAfterMove(bitBoards, move, false))
                    {
                        yield return move;
                    }
                }

                if ((moveOne & (1UL << i)) != 0)
                {
                    var move = new Move(i + 8, i, ChessPiece.BlackPawn);
                    if (IsKingSafeAfterMove(bitBoards, move, false))
                    {
                        yield return move;
                    }
                }

                if ((moveTwo & (1UL << i)) != 0)
                {
                    var move = new Move(i + 16, i, ChessPiece.BlackPawn);
                    if (IsKingSafeAfterMove(bitBoards, move, false))
                    {
                        yield return move;
                    }
                }
            }
        }

        public IEnumerable<Move> GetPossibleKingMoves(BitBoards bitBoards, bool forWhite)
        {
            var kings = forWhite ? bitBoards.WhiteKings : bitBoards.BlackKings;
            var chessPiece = forWhite ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
            return GetPossibleJumpingMoves(bitBoards, kings, BitBoards.KingSpan, BitBoards.KingSpanPosition, forWhite, chessPiece);
        }

        public IEnumerable<Move> GetPossibleKnightMoves(BitBoards bitBoards, bool forWhite)
        {
            var knights = forWhite ? bitBoards.WhiteNights : bitBoards.BlackNights;
            var chessPiece = forWhite ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight;
            return GetPossibleJumpingMoves(bitBoards, knights, BitBoards.KnightSpan, BitBoards.KnightSpanPosition, forWhite, chessPiece);
        }

        private IEnumerable<Move> GetPossibleJumpingMoves(BitBoards bitBoards, ulong jumpingPieces, ulong jumpMask, int jumpMaskCenter, bool forWhite, ChessPiece piece)
        {
            var ownPieces = forWhite ? bitBoards.WhitePieces : bitBoards.BlackPieces;
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
                        if (IsKingSafeAfterMove(bitBoards, move, forWhite))
                        {
                            yield return move;
                        }
                        //yield return move;
                    }
                }
            }
        }

        public IEnumerable<Move> GetPossibleRookMoves(BitBoards bitBoards, bool forWhite)
        {
            var rooks = forWhite ? bitBoards.WhiteRooks : bitBoards.BlackRooks;
            var chessPiece = forWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook;
            return GetPossibleSlidingPieceMoves(bitBoards, rooks, HorizontalVerticalSlide, forWhite, chessPiece);
        }

        public IEnumerable<Move> GetPossibleBishopMoves(BitBoards bitBoards, bool forWhite)
        {
            var bishops = forWhite ? bitBoards.WhiteBishops : bitBoards.BlackBishops;
            var chessPiece = forWhite ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop;
            return GetPossibleSlidingPieceMoves(bitBoards, bishops, DiagonalAntidiagonalSlide, forWhite, chessPiece);
        }

        public IEnumerable<Move> GetPossibleQueenMoves(BitBoards bitBoards, bool forWhite)
        {
            var bishops = forWhite ? bitBoards.WhiteQueens : bitBoards.BlackQueens;
            var chessPiece = forWhite ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
            return GetPossibleSlidingPieceMoves(bitBoards, bishops, AllSlide, forWhite, chessPiece);
        }

        private IEnumerable<Move> GetPossibleSlidingPieceMoves(BitBoards bitBoards, ulong slidingPieces, Func<BitBoards, int, ulong> slideResolutionFunc, bool forWhite, ChessPiece piece)
        {
            var ownPieces = forWhite ? bitBoards.WhitePieces : bitBoards.BlackPieces;
            for (var i = 0; i < 64; i++)
            {
                if ((slidingPieces & (1UL << i)) != 0)
                //if ((slidingPieces & (1UL << i)) > 0)
                {
                    var slide = slideResolutionFunc.Invoke(bitBoards, i);
                    slide &= ~ownPieces;
                    foreach (var move in BitmaskToMoves(slide, i, piece))
                    {
                        if (IsKingSafeAfterMove(bitBoards, move, forWhite))
                        {
                            yield return move;
                        }
                    }
                }
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

        private static IEnumerable<Move> BitmaskToMoves(ulong bitmask, int positionFrom, ChessPiece piece)
        {
            for (var j = 0; j < 64; j++)
            {
                if ((bitmask & (1UL << j)) != 0)
                {
                    var move = new Move(positionFrom, j, piece);
                    yield return move;
                }
            }
        }

        public ulong GetAllAttacked(BitBoards bitBoards, bool forWhite)
        {
            var pawnsAttack = GetAttackedByPawns(bitBoards, forWhite);
            var knightsAttack = GetAttackedByKnights(bitBoards, forWhite);
            var bishopsAttack = GetAttackedByBishops(bitBoards, forWhite);
            var rooksAttack = GetAttackedByRooks(bitBoards, forWhite);
            var queensAttack = GetAttackedByQueens(bitBoards, forWhite);
            var kingsAttack = GetAttackedByKings(bitBoards, forWhite);

            var allAttacked = pawnsAttack | knightsAttack | bishopsAttack | rooksAttack | queensAttack | kingsAttack;
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
            for (var i = 0; i < 64; i++)
            {
                //if (slidingPieces.HasBit(i))
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
            var slide = (((bitboards.FilledSquares & mask) - 2 * pieceBitboard) ^ ((bitboards.FilledSquares & mask).Reverse() - 2 * pieceBitboard.Reverse()).Reverse()) & mask;
            return slide;
        }
    }
}
