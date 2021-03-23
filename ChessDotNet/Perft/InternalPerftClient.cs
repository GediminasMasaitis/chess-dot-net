using System;
using System.Collections.Generic;
using System.Linq;
using ChessDotNet.Data;
using ChessDotNet.MoveGeneration;
using ChessDotNet.Search2;

namespace ChessDotNet.Perft
{
    public class InternalPerftClient : IPerftClient
    {
        private readonly PossibleMovesService _possibleMovesService;
        private readonly BoardFactory _boardFactory;

        private Board _currentBoard;
        private Move[][] _moves;

        public InternalPerftClient(PossibleMovesService possibleMovesService, BoardFactory boardFactory)
        {
            _possibleMovesService = possibleMovesService;
            _boardFactory = boardFactory;
            _moves = new Move[SearchConstants.MaxDepth][];
            for (var i = 0; i < SearchConstants.MaxDepth; i++)
            {
                _moves[i] = new Move[218];
            }

            SetBoard("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        }

        public void SetBoard(string fen)
        {
            _currentBoard = _boardFactory.ParseFEN(fen);
        }

        public int GetMoveCount(int depth)
        {
            throw new NotImplementedException();
        }

        public IList<MoveAndNodes> GetMovesAndNodes(int depth)
        {
            var results = GetMovesAndNodeRoot(depth).ToList();
            results = results.OrderBy(x => x.Move).ToList();
            return results;
        }

        private IEnumerable<MoveAndNodes> GetMovesAndNodeRoot(int depth)
        {
            if (depth == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth, null);
            }

            var possibleMoves = _moves[depth];
            var moveCount = 0;
            _possibleMovesService.GetAllPossibleMoves(_currentBoard, possibleMoves, ref moveCount);
            for (var i = 0; i < moveCount; i++)
            {
                var move = possibleMoves[i];
                //_currentBoard.TestMove(move);
                _currentBoard.DoMove2(move);
                var nodes = GetNodesInner(_currentBoard, depth - 1);
                //var nodes = 1;
                _currentBoard.UndoMove();
                var moveStr = move.ToPositionString();
                var moveAndNodes = new MoveAndNodes(moveStr, nodes, move);
                yield return moveAndNodes;
            }
        }

        private int GetNodesInner(Board board, int depth)
        {
            if (depth == 0)
            {
                return 1;
            }

            var possibleMoves = _moves[depth];
            var moveCount = 0;
            _possibleMovesService.GetAllPossibleMoves(board, possibleMoves, ref moveCount);
            if (depth == 1)
            {
                return moveCount;
            }

            var nodes = 0;
            for (var i = 0; i < moveCount; i++)
            {
                var move = possibleMoves[i];
                //board.TestMove(move);
                board.DoMove2(move);
                var childNodes = GetNodesInner(board, depth - 1);
                board.UndoMove();
                nodes += childNodes;
            }

            return nodes;
        }

        public void Dispose()
        {
        }
    }
}