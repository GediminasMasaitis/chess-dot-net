using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessDotNet
{
    public class BitBoards
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
        public ulong FilledSquares { get; private set; }
        public IReadOnlyDictionary<ChessPiece, ulong> PiecesDict { get; private set; }

        public ulong EnPassantFile { get; set; }

        public static ulong AllBoard { get; }
        public static ulong KnightSpan { get; private set; }
        public static int KnightSpanPosition { get; private set; }
        public static ulong KingSpan { get; private set; }
        public static int KingSpanPosition { get; private set; }
        public static IReadOnlyList<ulong> Files { get; private set; }
        public static IReadOnlyList<ulong> Ranks { get; private set; }
        public static IReadOnlyList<ulong> Diagonals { get; private set; }
        public static IReadOnlyList<ulong> Antidiagonals { get; private set; }

        public BitBoards()
        {
            
        }

        static BitBoards()
        {
            Initialize();
        }

        public static void Initialize()
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
        }

    
        public void Sync()
        {
            WhitePieces = WhitePawns | WhiteNights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKings;
            BlackPieces = BlackPawns | BlackNights | BlackBishops | BlackRooks | BlackQueens | BlackKings;
            FilledSquares = WhitePieces | BlackPieces;
            EmptySquares = ~FilledSquares;

            PiecesDict = new Dictionary<ChessPiece, ulong>
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
                if (move.Piece == pair.Key)
                {
                    bitBoard |= 1UL << move.To;
                }
                newPiecesDict.Add(pair.Key, bitBoard);
            }
            var newBoards = FromDict(newPiecesDict);
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
            newBoards.Sync();
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
    }
}