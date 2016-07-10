using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChessDotNet.Perft
{
    public class Perft
    {
        public PossibleMovesService PossibleMovesService { get; set; }


        public Perft(PossibleMovesService possibleMovesService)
        {
            PossibleMovesService = possibleMovesService;
        }

        public IList<string> GetPossibleMoves(BitBoards bitBoards, bool whiteToMove, int depth)
        {
            return GetPossibleMovesInner(bitBoards, whiteToMove, depth, 1, "").ToList();
        }

        public IList<MoveAndNodes> FindMoveAndNodesFromEngineResults(IEnumerable<string> engineResults)
        {
            var grouped = engineResults.GroupBy(x => x.Split(' ')[0]);
            var man = grouped.Select(x => new MoveAndNodes(x.Key, x.Count()));
            return man.OrderBy(x => x.Move).ToList();
        }

        public int GetPossibleMoveCount(BitBoards bitBoards, bool whiteToMove, int depth)
        {
            return GetPossibleMoveCountInner(bitBoards, whiteToMove, depth, 1);
        }

        public int GetPossibleMoveCountInner(BitBoards bitBoards, bool whiteToMove, int depth, int currentDepth)
        {
            var currentNum = 0;
            var moves = PossibleMovesService.GetAllPossibleMoves(bitBoards, whiteToMove);
            foreach (var move in moves)
            {
                if (currentDepth >= depth)
                {
                    currentNum++;
                }
                else
                {
                    var movedBoard = bitBoards.DoMove(move);
                    currentNum += GetPossibleMoveCountInner(movedBoard, !whiteToMove, depth, currentDepth + 1);
                }
            }
            return currentNum;
        }

        private IEnumerable<string> GetPossibleMovesInner(BitBoards bitBoards, bool whiteToMove, int depth, int currentDepth, string currentString)
        {
            var moves = PossibleMovesService.GetAllPossibleMoves(bitBoards, whiteToMove);
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
                    foreach (var otherBoards in GetPossibleMovesInner(movedBoard, !whiteToMove, depth, currentDepth + 1, moveString))
                    {
                        yield return otherBoards;
                    }
                }
            }
        }


    }
}