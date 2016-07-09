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

        public ulong WhitePieces { get; set; }
        public ulong BlackPieces { get; set; }
        public ulong EmptySquares { get; set; }
        public ulong FilledSquares { get; set; }
       
        public ulong EnPassantFile { get; set; }

        public static ulong AllBoard { get; }
        public static ulong KnightSpan { get; private set; }
        public static int KnightSpanPosition { get; private set; }
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
        }
    }
}