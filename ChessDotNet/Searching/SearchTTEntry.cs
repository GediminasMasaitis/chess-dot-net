using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;

using Bitboard = System.UInt64;
using ZobristKey = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;
using TTFlag = System.Byte;

namespace ChessDotNet.Searching
{
    public class SearchTTEntry
    {
        public SearchTTEntry(ZobristKey key, Move move, int score, byte depth, TTFlag flag)
        {
            Key = key;
            Move = move;
            Score = score;
            Depth = depth;
            Flag = flag;
        }

        public ulong Key { get; }
        public Move Move { get; }
        public int Score { get; }
        public byte Depth { get; }
        public TTFlag Flag { get; }
    }
}
