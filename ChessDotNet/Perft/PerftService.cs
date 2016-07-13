using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.Hashing;
using ChessDotNet.MoveGeneration;

namespace ChessDotNet.Perft
{
    public class PerftService
    {
        public PossibleMovesService PossibleMovesService { get; set; }
        public bool MultiThreaded { get; set; }

        public PerftService(PossibleMovesService possibleMovesService)
        {
            PossibleMovesService = possibleMovesService;
            MultiThreaded = true;
        }

        public IList<string> GetPossibleMoves(Board board, int depth)
        {
            return GetPossibleMovesInner(board, depth, 1, "").ToList();
        }

        public IList<MoveAndNodes> Divide(Board board, int depth)
        {
            var moves = PossibleMovesService.GetAllPossibleMoves(board);
            if (depth == 1)
            {
                return moves.Select(x => new MoveAndNodes(x.ToPositionString(), 1)).OrderBy(x => x.Move).ToList();
            }
            var results = new List<MoveAndNodes>();
            Action<Move> act = m =>
            {
                var moved = board.DoMove(m);
                var count = GetPossibleMoveCountInner(moved, depth, 2);
                var posStr = m.ToPositionString();
                var man = new MoveAndNodes(posStr, count, m);
                lock (results)
                {
                    results.Add(man);
                }
            };
            if (MultiThreaded)
            {
                Parallel.ForEach(moves, act);
            }
            else
            {
                foreach (var move in moves)
                {
                    act.Invoke(move);
                }
            }
            var ordered = results.OrderBy(x => x.Move).ToList();
            return ordered;
        }

        public int GetPossibleMoveCount(Board board, int depth)
        {
            return GetPossibleMoveCountInner(board, depth, 1);
        }

        public int GetPossibleMoveCountInner(Board board, int depth, int currentDepth)
        {
            var currentNum = 0;
            var moves = PossibleMovesService.GetAllPossibleMoves(board);
            if (currentDepth >= depth)
            {
                currentNum = moves.Count;
            }
            else
            {
                if (currentDepth == 1)
                {
                    var sync = new object();
                    Parallel.ForEach(moves, m =>
                    {
                        var movedBoard = board.DoMove(m);
                        var possibleMoveCountInner = GetPossibleMoveCountInner(movedBoard, depth, currentDepth + 1);
                        lock (sync)
                        {
                            currentNum += possibleMoveCountInner;
                        }
                    });
                }
                else
                {
                    foreach (var move in moves)
                    {
                        var movedBoard = board.DoMove(move);
                        var possibleMoveCountInner = GetPossibleMoveCountInner(movedBoard, depth, currentDepth + 1);
                        currentNum += possibleMoveCountInner;
                    }
                }
            }
            return currentNum;
        }

        private IEnumerable<string> GetPossibleMovesInner(Board board, int depth, int currentDepth, string currentString)
        {
            var moves = PossibleMovesService.GetAllPossibleMoves(board);
            foreach (var move in moves)
            {
                var moveString = currentString + (currentString.Length == 0 ? string.Empty : " ") + move.ToPositionString();
                if (currentDepth >= depth)
                {
                    yield return moveString;
                }
                else
                {
                    var movedBoard = board.DoMove(move);
                    foreach (var otherBoards in GetPossibleMovesInner(movedBoard, depth, currentDepth + 1, moveString))
                    {
                        yield return otherBoards;
                    }
                }
            }
        }


    }
}