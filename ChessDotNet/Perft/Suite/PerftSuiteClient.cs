using System;
using System.Collections.Generic;
using System.Text;

namespace ChessDotNet.Perft.Suite
{


    class PerftSuiteClient : IPerftClient
    {
        private string _currentFen;

        public PerftSuiteClient()
        {

        }

        public void SetBoard(string fen)
        {
            _currentFen = fen;
        }

        public int GetMoveCount(int depth)
        {
            throw new NotImplementedException();
        }

        public IList<MoveAndNodes> GetMovesAndNodes(int depth)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
