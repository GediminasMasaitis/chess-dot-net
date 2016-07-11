using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.MoveGeneration;

namespace ChessDotNet.Perft
{
    public class PerftService
    {
        public PossibleMovesService PossibleMovesService { get; set; }

        public PerftService(PossibleMovesService possibleMovesService)
        {
            PossibleMovesService = possibleMovesService;
        }

        public IList<string> GetPossibleMoves(BitBoards bitBoards, int depth)
        {
            return GetPossibleMovesInner(bitBoards, depth, 1, "").ToList();
        }

        public IList<MoveAndNodes> Divide(BitBoards bitBoards, int depth)
        {
            var moves = PossibleMovesService.GetAllPossibleMoves(bitBoards);
            if (depth == 1)
            {
                return moves.Select(x => new MoveAndNodes(x.ToPositionString(), 1)).OrderBy(x => x.Move).ToList();
            }
            var results = new List<MoveAndNodes>();
            Parallel.ForEach(moves, m =>
            {
                var moved = bitBoards.DoMove(m);
                var count = GetPossibleMoveCountInner(moved, depth, 2);
                var posStr = m.ToPositionString();
                var man = new MoveAndNodes(posStr, count, m);
                lock (results)
                {
                    results.Add(man);
                }
            });
            var ordered = results.OrderBy(x => x.Move).ToList();
            return ordered;
        }

        public int GetPossibleMoveCount(BitBoards bitBoards, int depth)
        {
            return GetPossibleMoveCountInner(bitBoards, depth, 1);
        }

        public int GetPossibleMoveCountInner(BitBoards bitBoards, int depth, int currentDepth)
        {
            var currentNum = 0;
            var moves = PossibleMovesService.GetAllPossibleMoves(bitBoards);
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
                        var movedBoard = bitBoards.DoMove(m);
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
                        var movedBoard = bitBoards.DoMove(move);
                        var possibleMoveCountInner = GetPossibleMoveCountInner(movedBoard, depth, currentDepth + 1);
                        currentNum += possibleMoveCountInner;
                    }
                }
            }
            return currentNum;
        }

        private IEnumerable<string> GetPossibleMovesInner(BitBoards bitBoards, int depth, int currentDepth, string currentString)
        {
            var moves = PossibleMovesService.GetAllPossibleMoves(bitBoards);
            foreach (var move in moves)
            {
                var moveString = currentString + (currentString.Length == 0 ? string.Empty : " ") + move.ToPositionString();
                if (currentDepth >= depth)
                {
                    yield return moveString;
                }
                else
                {
                    var movedBoard = bitBoards.DoMove(move);
                    foreach (var otherBoards in GetPossibleMovesInner(movedBoard, depth, currentDepth + 1, moveString))
                    {
                        yield return otherBoards;
                    }
                }
            }
        }


    }
}