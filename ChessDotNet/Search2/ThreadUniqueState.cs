using System;
using System.Collections.Generic;
using ChessDotNet.Data;

namespace ChessDotNet.Search2
{
    public class ThreadUniqueState
    {
        public uint[,] Killers { get; }
        public int[,,] History { get; }
        public int[,,] Cutoff { get; }
        public Move[][] Moves { get; }
        public int[][] MoveStaticScores { get; }
        public Random Rng { get; }

        public ThreadUniqueState(int threadId)
        {
            Killers = new uint[SearchConstants.MaxDepth, 2]; // Non-captures causing beta cutoffs
            History = new int[2, 64, 64];
            Cutoff = new int[2, 64, 64];
            Moves = new Move[SearchConstants.MaxDepth][];
            for (int i = 0; i < Moves.Length; i++)
            {
                Moves[i] = new Move[218];
            }
            MoveStaticScores = new int[SearchConstants.MaxDepth][];
            for (int i = 0; i < MoveStaticScores.Length; i++)
            {
                MoveStaticScores[i] = new int[218];
            }
            Rng = new Random(threadId);
        }

        public void OnNewSearch()
        {
            Array.Clear(Killers, 0, Killers.Length);
            Array.Clear(History, 0, History.Length);
            for (var i = 0; i < 2; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    for (int k = 0; k < 64; k++)
                    {
                        Cutoff[i, j, k] = 100;
                    }
                }
            }
        }

        public void OnIterativeDeepen()
        {
            //Array.Clear(Killers, 0, Killers.Length);
            //Array.Clear(History, 0, History.Length);
        }
    }
}