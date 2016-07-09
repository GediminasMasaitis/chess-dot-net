using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet
{
    internal static class ExtensionMethods
    {
        public static bool HasBit(this ulong bitboard, int bit)
        {
            return (bitboard & (1UL << bit)) != 0;
        }

        public static ulong Reverse(this ulong bitboard)
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
