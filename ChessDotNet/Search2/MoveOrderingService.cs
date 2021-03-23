using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;

namespace ChessDotNet.Search2
{
    public class MoveOrderingService
    {
        public void CalculateStaticScores(Move[] moves, int moveCount, int ply, Move pvMove, uint[,] killers, int[] staticScores)
        {
            for (var i = 0; i < moveCount; i++)
            {
                var score = CalculateStaticMoveScore(moves[i], ply, pvMove, killers);
                staticScores[i] = score;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CalculateStaticMoveScore(Move move, int ply, Move pvMove, uint[,] killers)
        {
            var moveKey = move.Key2;
            var isPrincipalVariation = pvMove.Key2 == moveKey;
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

            return 0;
        }

        public void OrderNextMove(int currentIndex, Move[] moves, int[] staticScores, int moveCount, int[,,] history)
        {
            var bestScore = -SearchConstants.Inf;
            var bestScoreIndex = -1;
            for (var i = currentIndex; i < moveCount; i++)
            {
                var move = moves[i];
                var score = staticScores[i];
                if (score == 0)
                {
                    var dynamicScore = CalculateDynamicScore(move, history);
                    score += dynamicScore;
                }
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestScoreIndex = i;
                }
            }

            var tempMove = moves[currentIndex];
            moves[currentIndex] = moves[bestScoreIndex];
            moves[bestScoreIndex] = tempMove;

            var tempScore = staticScores[currentIndex];
            staticScores[currentIndex] = staticScores[bestScoreIndex];
            staticScores[bestScoreIndex] = tempScore;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CalculateDynamicScore(Move move, int[,,] history)
        {
            var historyScore = history[move.WhiteToMoveNum, move.From, move.To];
            return historyScore;
        }

        private int CalculateFullScore(Move move, int ply, Move principalVariationMove, uint[,] killers, int[,,] history)
        {
            //return 0;
            var moveKey = move.Key2;
            var isPrincipalVariation = principalVariationMove.Key2 == moveKey;
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