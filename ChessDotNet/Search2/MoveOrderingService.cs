using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;

namespace ChessDotNet.Search2
{
    public class MoveOrderingService
    {
        public MoveOrderingService()
        {
        }

        public void CalculateStaticScores(Board board, Move[] moves, int moveCount, int ply, Move pvMove, uint[][] killers, bool useSee, int[] seeScores, int[] staticScores)
        {
            for (var i = 0; i < moveCount; i++)
            {
                var seeScore = seeScores[i];
                var score = CalculateStaticMoveScore(board, moves[i], ply, pvMove, killers, useSee, seeScore);
                staticScores[i] = score;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CalculateStaticMoveScore(Board board, Move move, int ply, Move pvMove, uint[][] killers, bool useSee, int seeScore)
        {
            var moveKey = move.Key2;
            var isPrincipalVariation = pvMove.Key2 == moveKey;
            if (isPrincipalVariation)
            {
                return 200_000_000;
            }

            if (move.TakesPiece != ChessPiece.Empty)
            {
                if (useSee)
                {
                    //if (seeScore > 0)
                    //{
                    //    return seeScore * 100_000;
                    //    //return 116000000;
                    //}
                    //if (seeScore == 0)
                    //{
                    //    return 99 * 100_000;
                    //}
                    var mvvLvaScore = MVVLVAScoreCalculation.Scores[move.Piece][move.TakesPiece];
                    if (seeScore > 0)
                    {
                        //return 116_000_000;
                        return mvvLvaScore;
                    }

                    if (seeScore == 0)
                    {
                        return mvvLvaScore / 2;
                    }

                    return mvvLvaScore / 200;
                    //return seeScore * -1_000;
                    //return mvvLvaScore - 200_000_000;
                }
                else
                {
                    var mvvLvaScore = MVVLVAScoreCalculation.Scores[move.Piece][move.TakesPiece];
                    return mvvLvaScore;
                }
            }

            if (killers[ply][0] == moveKey)
            {
                return 9_000_000;
            }
            if (killers[ply][1] == moveKey)
            {
                return 8_000_000;
            }
            
            return 0;
        }

        public void OrderNextMove(int currentIndex, Move[] moves, int[] staticScores, int[] seeScores, int moveCount, int[][][] history)
        {
            var bestScore = int.MinValue;
            var bestScoreIndex = -1;
            for (var i = currentIndex; i < moveCount; i++)
            {
                var move = moves[i];
                var score = staticScores[i];
                if (score == 0)
                {
                    var num = move.ColorToMove;
                    score = history[num][move.From][move.To];
                    //score = 0;
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

            var tempSee = seeScores[currentIndex];
            seeScores[currentIndex] = seeScores[bestScoreIndex];
            seeScores[bestScoreIndex] = tempSee;

            var tempScore = staticScores[currentIndex];
            staticScores[currentIndex] = staticScores[bestScoreIndex];
            staticScores[bestScoreIndex] = tempScore;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CalculateDynamicScore(Move move, int[][][] history)
        {
            var historyScore = history[move.ColorToMove][move.From][move.To];
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

            var mvvLvaScore = MVVLVAScoreCalculation.Scores[move.Piece][move.TakesPiece];
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

            var historyScore = history[move.ColorToMove, move.From, move.To];
            return historyScore;
        }
    }
}