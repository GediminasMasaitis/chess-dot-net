﻿using System.Collections.Generic;

namespace ChessDotNet.Data
{
    public class BitBoards : BoardBase
    {
        public ulong WhitePawns { get; set; }
        public ulong WhiteNights { get; set; }
        public ulong WhiteBishops { get; set; }
        public ulong WhiteRooks { get; set; }
        public ulong WhiteQueens { get; set; }
        public ulong WhiteKings { get; set; }

        public ulong BlackPawns { get; set; }
        public ulong BlackNights { get; set; }
        public ulong BlackBishops { get; set; }
        public ulong BlackRooks { get; set; }
        public ulong BlackQueens { get; set; }
        public ulong BlackKings { get; set; }

        public ulong WhitePieces { get; private set; }
        public ulong BlackPieces { get; private set; }
        public ulong EmptySquares { get; private set; }
        public ulong AllPieces { get; private set; }
        public IReadOnlyDictionary<ChessPiece, ulong> PiecesDict { get; private set; }

        public ulong EnPassantFile { get; set; }


        public static ulong AllBoard { get; }
        public static ulong KnightSpan { get; private set; }
        public static int KnightSpanPosition { get; private set; }
        public static ulong KingSpan { get; private set; }
        public static int KingSpanPosition { get; private set; }
        public static IReadOnlyList<ulong> Files { get; }
        public static IReadOnlyList<ulong> Ranks { get; }
        public static IReadOnlyList<ulong> Diagonals { get; private set; }
        public static IReadOnlyList<ulong> Antidiagonals { get; private set; }
        public static ulong KingSide { get; set; }
        public static ulong QueenSide { get; set; }

        public static ulong WhiteQueenSideCastleMask { get; }
        public static ulong WhiteKingSideCastleMask { get; }
        public static ulong BlackQueenSideCastleMask { get; }
        public static ulong BlackKingSideCastleMask { get; }

        public static ulong WhiteQueenSideCastleAttackMask { get; }
        public static ulong WhiteKingSideCastleAttackMask { get; }
        public static ulong BlackKingSideCastleAttackMask { get; }
        public static ulong BlackQueenSideCastleAttackMask { get; }


        public BitBoards()
        {
            
        }

        static BitBoards()
        {
            KnightSpan = 43234889994UL;
            KnightSpanPosition = 18;

            KingSpan = 460039UL;
            KingSpanPosition = 9;

            var files = new List<ulong>(8);
            for (var i = 0; i < 8; i++)
            {
                var file = 0UL;
                for (var j = 0; j < 8; j++)
                {
                    file |= 1UL << i << (j*8);
                }
                files.Add(file);
            }
            Files = files;

            QueenSide = Files[0] | Files[1] | Files[2] | Files[3];
            KingSide = ~QueenSide;

            var ranks = new List<ulong>(8);
            for (var i = 0; i < 8; i++)
            {
                var rank = 0UL;
                for (var j = 0; j < 8; j++)
                {
                    rank |= 1UL << (i*8) << j;
                }
                ranks.Add(rank);
            }
            Ranks = ranks;

            Diagonals = new[]
            {
                0x1UL,
                0x102UL,
                0x10204UL,
                0x1020408UL,
                0x102040810UL,
                0x10204081020UL,
                0x1020408102040UL,
                0x102040810204080UL,
                0x204081020408000UL,
                0x408102040800000UL,
                0x810204080000000UL,
                0x1020408000000000UL,
                0x2040800000000000UL,
                0x4080000000000000UL,
                0x8000000000000000UL
            };

            Antidiagonals = new[]
            {
                0x80UL,
                0x8040UL,
                0x804020UL,
                0x80402010UL,
                0x8040201008UL,
                0x804020100804UL,
                0x80402010080402UL,
                0x8040201008040201UL,
                0x4020100804020100UL,
                0x2010080402010000UL,
                0x1008040201000000UL,
                0x804020100000000UL,
                0x402010000000000UL,
                0x201000000000000UL,
                0x100000000000000UL
            };

            var queenSideCastleMask = Files[1] | Files[2] | Files[3];
            var kingSideCastleMask = Files[5] | Files[6];
            WhiteQueenSideCastleMask = queenSideCastleMask & Ranks[0];
            WhiteKingSideCastleMask = kingSideCastleMask & Ranks[0];
            BlackQueenSideCastleMask = queenSideCastleMask & Ranks[7];
            BlackKingSideCastleMask = kingSideCastleMask & Ranks[7];

            var queenSideCastleAttackMask = Files[2] | Files[3] | Files[4];
            var kingSideCastleAttackMask = Files[4] | Files[5] | Files[6];
            WhiteQueenSideCastleAttackMask = queenSideCastleAttackMask & Ranks[0];
            WhiteKingSideCastleAttackMask = kingSideCastleAttackMask & Ranks[0];
            BlackQueenSideCastleAttackMask = queenSideCastleAttackMask & Ranks[7];
            BlackKingSideCastleAttackMask = kingSideCastleAttackMask & Ranks[7];
        }


        public void Sync(IReadOnlyDictionary<ChessPiece, ulong> dictToUse = null)
        {
            WhitePieces = WhitePawns | WhiteNights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKings;
            BlackPieces = BlackPawns | BlackNights | BlackBishops | BlackRooks | BlackQueens | BlackKings;
            AllPieces = WhitePieces | BlackPieces;
            EmptySquares = ~AllPieces;

            PiecesDict = dictToUse ?? new Dictionary<ChessPiece, ulong>
            {
                { ChessPiece.WhitePawn, WhitePawns },
                { ChessPiece.WhiteKnight, WhiteNights },
                { ChessPiece.WhiteBishop, WhiteBishops },
                { ChessPiece.WhiteRook, WhiteRooks},
                { ChessPiece.WhiteQueen, WhiteQueens },
                { ChessPiece.WhiteKing, WhiteKings },

                { ChessPiece.BlackPawn, BlackPawns },
                { ChessPiece.BlackKnight, BlackNights },
                { ChessPiece.BlackBishop, BlackBishops },
                { ChessPiece.BlackRook, BlackRooks},
                { ChessPiece.BlackQueen, BlackQueens },
                { ChessPiece.BlackKing, BlackKings }
            };
        }

        public BitBoards DoMove(Move move)
        {
            var newPiecesDict = new Dictionary<ChessPiece, ulong>(PiecesDict.Count);
            foreach (var pair in PiecesDict)
            {
                var bitBoard = pair.Value & ~(1UL << move.From) & ~(1UL << move.To);
                var pieceToAdd = move.PawnPromoteTo ?? move.Piece;
                if (pair.Key == pieceToAdd)
                {
                    bitBoard |= 1UL << move.To;
                }
                if (move.EnPassant && move.Piece == ChessPiece.WhitePawn && pair.Key == ChessPiece.BlackPawn)
                {
                    bitBoard &= ~(1UL << (move.To - 8));
                }
                if (move.EnPassant && move.Piece == ChessPiece.BlackPawn && pair.Key == ChessPiece.WhitePawn)
                {
                    bitBoard &= ~(1UL << (move.To + 8));
                }
                newPiecesDict[pair.Key] = bitBoard;
            }
            var newBoards = FromDict(newPiecesDict);

            if (move.Castle)
            {
                var kingSide = move.To % 8 > 3;
                var isWhite = move.Piece == ChessPiece.WhiteKing;
                var castlingRookPos = (kingSide ? 7 : 0) + (isWhite ? 0 : 56);
                var castlingRookNewPos = (move.From + move.To) / 2;

                if (isWhite)
                {
                    newBoards.WhiteRooks &= ~(1UL << castlingRookPos);
                    newBoards.WhiteRooks |= 1UL << castlingRookNewPos;
                }
                else
                {
                    newBoards.BlackRooks &= ~(1UL << castlingRookPos);
                    newBoards.BlackRooks |= 1UL << castlingRookNewPos;
                }
            }

            newBoards.Sync();

            if ((move.Piece == ChessPiece.WhitePawn && move.From + 16 == move.To) || (move.Piece == ChessPiece.BlackPawn && move.From - 16 == move.To))
            {
                newBoards.EnPassantFile = Files[move.From%8];
            }
            else
            {
                newBoards.EnPassantFile = 0;
            }
            newBoards.WhiteToMove = !WhiteToMove;

            if (move.Piece == ChessPiece.WhiteKing)
            {
                newBoards.WhiteCanCastleQueenSide = false;
                newBoards.WhiteCanCastleKingSide = false;
            }
            else if (move.Piece == ChessPiece.WhiteRook)
            {
                newBoards.WhiteCanCastleQueenSide = WhiteCanCastleQueenSide && move.From % 8 > 3;
                newBoards.WhiteCanCastleKingSide = WhiteCanCastleKingSide && move.From%8 < 3;
            }
            else
            {
                newBoards.WhiteCanCastleQueenSide = WhiteCanCastleQueenSide;
                newBoards.WhiteCanCastleKingSide = WhiteCanCastleKingSide;
            }

            if (move.Piece == ChessPiece.BlackKing)
            {
                newBoards.BlackCanCastleQueenSide = false;
                newBoards.BlackCanCastleKingSide = false;
            }
            else if (move.Piece == ChessPiece.BlackRook)
            {
                newBoards.BlackCanCastleQueenSide = BlackCanCastleQueenSide && move.From % 8 > 3;
                newBoards.BlackCanCastleKingSide = BlackCanCastleKingSide && move.From % 8 < 3;
            }
            else
            {
                newBoards.BlackCanCastleQueenSide = BlackCanCastleQueenSide;
                newBoards.BlackCanCastleKingSide = BlackCanCastleKingSide;
            }
            return newBoards;
        }

        private BitBoards FromDict(IReadOnlyDictionary<ChessPiece, ulong> dictionary)
        {
            var newBoards = new BitBoards
            {
                WhitePawns = dictionary[ChessPiece.WhitePawn],
                WhiteNights = dictionary[ChessPiece.WhiteKnight],
                WhiteBishops = dictionary[ChessPiece.WhiteBishop],
                WhiteRooks = dictionary[ChessPiece.WhiteRook],
                WhiteQueens = dictionary[ChessPiece.WhiteQueen],
                WhiteKings = dictionary[ChessPiece.WhiteKing],

                BlackPawns = dictionary[ChessPiece.BlackPawn],
                BlackNights = dictionary[ChessPiece.BlackKnight],
                BlackBishops = dictionary[ChessPiece.BlackBishop],
                BlackRooks = dictionary[ChessPiece.BlackRook],
                BlackQueens = dictionary[ChessPiece.BlackQueen],
                BlackKings = dictionary[ChessPiece.BlackKing],
            };
            return newBoards;
        }

        public BitBoards Clone()
        {
            var newBoards = new BitBoards
            {
                WhitePawns = WhitePawns,
                WhiteNights = WhiteNights,
                WhiteBishops = WhiteBishops,
                WhiteRooks = WhiteRooks,
                WhiteQueens = WhiteQueens,
                WhiteKings = WhiteKings,

                BlackPawns = BlackPawns,
                BlackNights = BlackNights,
                BlackBishops = BlackBishops,
                BlackRooks = BlackRooks,
                BlackQueens = BlackQueens,
                BlackKings = BlackKings
            };
            newBoards.Sync();
            return newBoards;
        }

        public int CountPieces(ulong pieceBitBoard)
        {
            var count = 0;
            while (pieceBitBoard != 0)
            {
                count++;
                pieceBitBoard &= pieceBitBoard - 1;
            }
            return count;
        }

        public PieceCounts CountPieces(bool forWhite)
        {
            return forWhite ? CountPiecesForWhite() : CountPiecesForBlack();
        }

        public PieceCounts CountPiecesForWhite()
        {
            var pawns = CountPieces(WhitePawns);
            var knights = CountPieces(WhiteNights);
            var bishops = CountPieces(WhiteBishops);
            var rooks = CountPieces(WhiteRooks);
            var queens = CountPieces(WhiteQueens);
            return new PieceCounts(pawns, knights, bishops, rooks, queens);
        }

        public PieceCounts CountPiecesForBlack()
        {
            var pawns = CountPieces(BlackPawns);
            var knights = CountPieces(BlackNights);
            var bishops = CountPieces(BlackBishops);
            var rooks = CountPieces(BlackRooks);
            var queens = CountPieces(BlackQueens);
            return new PieceCounts(pawns, knights, bishops, rooks, queens);
        }
    }

    public struct PieceCounts
    {
        public PieceCounts(int pawns, int knights, int bishops, int rooks, int queens)
        {
            Pawns = pawns;
            Knights = knights;
            Bishops = bishops;
            Rooks = rooks;
            Queens = queens;
        }

        public int Pawns { get; }
        public int Knights { get; }
        public int Bishops { get; }
        public int Rooks { get; }
        public int Queens { get; }
    }
}