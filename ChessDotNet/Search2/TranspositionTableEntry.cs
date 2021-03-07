using System;
using ChessDotNet.Data;

namespace ChessDotNet.Search2
{
    public struct TranspositionTableEntry
    {
        public UInt64 Key { get; }
        public Move Move { get; }
        public int Depth { get; }
        public int Score { get; }
        public Byte Flag { get; }

        public TranspositionTableEntry(UInt64 key, Move move, int depth, int score, Byte flag)
        {
            Key = key;
            Move = move;
            Depth = depth;
            Score = score;
            Flag = flag;
        }
    }
}