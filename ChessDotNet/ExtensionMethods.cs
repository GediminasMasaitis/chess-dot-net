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
    }
}
