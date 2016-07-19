using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;

namespace ChessDotNet.Searching
{
    public class SearchTTEntry
    {
        public SearchTTEntry(ulong key, Move move, int score, int depth, int flag)
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
        public int Depth { get; }
        public int Flag { get; }
    }
}
