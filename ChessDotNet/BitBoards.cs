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

        public static bool IsInitialized { get; private set; }
        public static ulong AllBoard { get; }
        public static IReadOnlyList<ulong> Files { get; private set; }
        public static IReadOnlyList<ulong> Ranks { get; private set; }

        public BitBoards()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

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
            IsInitialized = true;
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