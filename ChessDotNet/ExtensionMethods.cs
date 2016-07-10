using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet
{
    public static class ExtensionMethods
    {
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static bool HasBit(this ulong bitboard, int bit)
        //{
        //    return (bitboard & (1UL << bit)) != 0;
        //}

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

        public static ulong Reverse(this ulong bitboard)
        {
            const ulong h1 = 0x5555555555555555;
            const ulong h2 = 0x3333333333333333;
            const ulong h4 = 0x0F0F0F0F0F0F0F0F;
            const ulong v1 = 0x00FF00FF00FF00FF;
            const ulong v2 = 0x0000FFFF0000FFFF;
            bitboard = ((bitboard >> 1) & h1) | ((bitboard & h1) << 1);
            bitboard = ((bitboard >> 2) & h2) | ((bitboard & h2) << 2);
            bitboard = ((bitboard >> 4) & h4) | ((bitboard & h4) << 4);
            bitboard = ((bitboard >> 8) & v1) | ((bitboard & v1) << 8);
            bitboard = ((bitboard >> 16) & v2) | ((bitboard & v2) << 16);
            bitboard = (bitboard >> 32) | (bitboard << 32);
            return bitboard;
        }
    }
}
