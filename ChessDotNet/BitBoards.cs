using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessDotNet
{
    public class BitBoards
    {
        public ulong WhitePawns { get; set; }
        public ulong WhiteKnights { get; set; }
        public ulong WhiteBishops { get; set; }
        public ulong WhiteRooks { get; set; }
        public ulong WhiteQueens { get; set; }
        public ulong WhiteKings { get; set; }

        public ulong BlackPawns { get; set; }
        public ulong BlackKnights { get; set; }
        public ulong BlackBishops { get; set; }
        public ulong BlackRooks { get; set; }
        public ulong BlackQueens { get; set; }
        public ulong BlackKings { get; set; }

        public ulong EmptySquares { get; set; }
        public ulong FilledSquares { get; set; }

        public ulong AllBoard { get; }

        public IReadOnlyList<ulong> Files { get; private set; }
        public IReadOnlyList<ulong> Ranks { get; private set; }

        public BitBoards()
        {
            Initialize();
        }

        public void Initialize()
        {
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
        }

    
        public void Sync()
        {
            FilledSquares = WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKings | BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKings;
            EmptySquares = ~FilledSquares;
        }
    }
}