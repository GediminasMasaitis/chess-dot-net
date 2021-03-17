using System;
using System.Collections.Generic;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;

namespace ChessDotNet.Search2
{
    public class MoveOrderingService
    {
        public void OrderNextMove(int currentIndex, IList<Move> moves, int ply, Move? pvMove, UInt64[,] killers, int[,,] history)
        {
            var bestScore = -SearchConstants.Inf;
            var bestScoreIndex = -1;
            for (var i = currentIndex; i < moves.Count; i++)
            {
                var score = CalculateMoveScore(moves[i], ply, pvMove, killers, history);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestScoreIndex = i;
                }
            }

            var temp = moves[currentIndex];
            moves[currentIndex] = moves[bestScoreIndex];
            moves[bestScoreIndex] = temp;
        }

        private int CalculateMoveScore(Move move, int ply, Move? principalVariationMove, UInt64[,] killers, int[,,] history)
        {
            //return 0;
            UInt64 moveKey = move.Key2;
            var isPrincipalVariation = principalVariationMove.HasValue && principalVariationMove.Value.Key2 == moveKey;
            if (isPrincipalVariation)
            {
                return 200000000;
            }

            var mvvLvaScore = MVVLVAScoreCalculation.Scores[move.Piece, move.TakesPiece];
            if (mvvLvaScore > 0)
            {
                return mvvLvaScore;
            }

            if (killers[ply, 0] == moveKey)
            {
                return 90000000;
            }
            if (killers[ply, 1] == moveKey)
            {
                return 80000000;
            }

            var historyScore = history[move.WhiteToMoveNum, move.From, move.To];
            return historyScore;
        }
    }
}