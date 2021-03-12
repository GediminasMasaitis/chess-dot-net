using System;
using System.Collections.Generic;

namespace ChessDotNet.Perft
{
    public interface IPerftClient : IDisposable
    {
        void SetBoard(string fen);

        int GetMoveCount(int depth);
        IList<MoveAndNodes> GetMovesAndNodes(int depth);
    }
}
