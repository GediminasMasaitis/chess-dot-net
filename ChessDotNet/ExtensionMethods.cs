﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;

namespace ChessDotNet
{
    public static class ExtensionMethods
    {
        private static readonly byte[] BitScanTable = {
            0, 47,  1, 56, 48, 27,  2, 60,
            57, 49, 41, 37, 28, 16,  3, 61,
            54, 58, 35, 52, 50, 42, 21, 44,
            38, 32, 29, 23, 17, 11,  4, 62,
            46, 55, 26, 59, 40, 36, 15, 53,
            34, 51, 20, 43, 31, 22, 10, 45,
            25, 39, 14, 33, 19, 30,  9, 24,
            13, 18,  8, 12,  7,  6,  5, 63
        };

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Position BitScanForward(this ulong bb)
        {
            const ulong debruijn64 = 0x03f79d71b4cb0a89UL;
            if (bb == 0)
            {
                return 64;
            }
            return BitScanTable[((bb ^ (bb - 1)) * debruijn64) >> 58];
        }

        public static IEnumerable<int> GetOnes(this ulong bb)
        {
            while (bb != 0)
            {
                var firstBit = BitScanForward(bb);
                yield return firstBit;
                bb &= ~(1UL << firstBit);
            }
        }

        [Obsolete]
        public static ulong ReverseOld(this ulong bitboard)
        {
            var num = 0UL;
            for (var i = 0; i < 64; i++)
            {
                num <<= 1;
                num |= (bitboard & 1);
                bitboard >>= 1;
            }
            return num;
        }
    }
}
