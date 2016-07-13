using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet.Data
{
    public class HistoryEntry
    {
        public HistoryEntry(Board board, Move move)
        {
            Board = board;
            Move = move;
        }

        public Board Board { get; }
        public Move Move { get; }
    }
}
