using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Int32;
using Piece = System.Int32;

namespace ChessDotNet.Data
{
    public static class BitboardConstants
    {
        public static Bitboard AllBoard { get; private set; }
        public static Bitboard KnightSpan { get; private set; }
        public static Position KnightSpanPosition { get; private set; }
        public static Bitboard KingSpan { get; private set; }
        public static Position KingSpanPosition { get; private set; }
        public static IReadOnlyList<Bitboard> Files { get; private set; }
        public static IReadOnlyList<Bitboard> Ranks { get; private set; }
        public static IReadOnlyList<Bitboard> Diagonals { get; private set; }
        public static IReadOnlyList<Bitboard> Antidiagonals { get; private set; }
        public static Bitboard KingSide { get; private set; }
        public static Bitboard QueenSide { get; private set; }

        public static Bitboard WhiteQueenSideCastleMask { get; private set; }
        public static Bitboard WhiteKingSideCastleMask { get; private set; }
        public static Bitboard BlackQueenSideCastleMask { get; private set; }
        public static Bitboard BlackKingSideCastleMask { get; private set; }

        public static Bitboard WhiteQueenSideCastleAttackMask { get; private set; }
        public static Bitboard WhiteKingSideCastleAttackMask { get; private set; }
        public static Bitboard BlackKingSideCastleAttackMask { get; private set; }
        public static Bitboard BlackQueenSideCastleAttackMask { get; private set; }


        public static void Init()
        {
            AllBoard = ~0UL;

            KnightSpan = 43234889994UL;
            KnightSpanPosition = 18;

            KingSpan = 460039UL;
            KingSpanPosition = 9;

            var files = new List<Bitboard>(8);
            for(var i = 0; i < 8; i++)
            {
                var file = 0UL;
                for(var j = 0; j < 8; j++)
                {
                    file |= 1UL << i << (j * 8);
                }
                files.Add(file);
            }
            Files = files;

            QueenSide = Files[0] | Files[1] | Files[2] | Files[3];
            KingSide = ~QueenSide;

            var ranks = new List<Bitboard>(8);
            for(var i = 0; i < 8; i++)
            {
                var rank = 0UL;
                for(var j = 0; j < 8; j++)
                {
                    rank |= 1UL << (i * 8) << j;
                }
                ranks.Add(rank);
            }
            Ranks = ranks;

            Diagonals = new Bitboard[]
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

            Antidiagonals = new Bitboard[]
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
    }
}
