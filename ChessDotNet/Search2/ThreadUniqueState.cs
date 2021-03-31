using System;
using System.Collections.Generic;
using ChessDotNet.Data;
using Newtonsoft.Json;

namespace ChessDotNet.Search2
{
    public class ThreadUniqueState
    {
        public int ThreadId { get; set; }
        public uint[][] Killers { get; set; }
        public uint[][] Countermove { get; set;}
        public int[][][] History { get; set; }
        public int[][][] Cutoff { get; set; }
        public Move[][] Moves { get; set; }
        public int[][] SeeScores { get; set; }
        public int[][] MoveStaticScores { get; set; }

        [JsonIgnore]
        public Random Rng { get; }

        public ThreadUniqueState()
        {
        }

        public ThreadUniqueState(int threadId)
        {
            ThreadId = threadId;
            Killers = new uint[SearchConstants.MaxDepth][]; // Non-captures causing beta cutoffs
            for (int i = 0; i < Killers.Length; i++)
            {
                Killers[i] = new uint[2];
            }

            Countermove = new uint[SearchConstants.MaxDepth][];
            for (int i = 0; i < Countermove.Length; i++)
            {
                Countermove[i] = new uint[64];
            }

            History = new int[2][][];
            for (int i = 0; i < History.Length; i++)
            {
                History[i] = new int[64][];
                for (int j = 0; j < History[i].Length; j++)
                {
                    History[i][j] = new int[64];
                }
            }

            Cutoff = new int[2][][];
            for (int i = 0; i < Cutoff.Length; i++)
            {
                Cutoff[i] = new int[64][];
                for (int j = 0; j < Cutoff[i].Length; j++)
                {
                    Cutoff[i][j] = new int[64];
                }
            }

            Moves = new Move[SearchConstants.MaxDepth][];
            for (int i = 0; i < Moves.Length; i++)
            {
                Moves[i] = new Move[218];
            }

            SeeScores = new int[SearchConstants.MaxDepth][];
            for (int i = 0; i < SeeScores.Length; i++)
            {
                SeeScores[i] = new int[218];
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
            for (int i = 0; i < Killers.Length; i++)
            {
                Array.Clear(Killers[i], 0, Killers[i].Length);
            }

            for (int i = 0; i < Countermove.Length; i++)
            {
                Array.Clear(Countermove[i], 0, Countermove[i].Length);
            }

            for (int i = 0; i < History.Length; i++)
            {
                for (int j = 0; j < History[i].Length; j++)
                {
                    Array.Clear(History[i][j], 0, History[i][j].Length);
                }
            }
            
            for (var i = 0; i < 2; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    for (int k = 0; k < 64; k++)
                    {
                        Cutoff[i][j][k] = 100;
                    }
                }
            }
        }

        public void OnIterativeDeepen()
        {
            for (int i = 0; i < Killers.Length; i++)
            {
                Array.Clear(Killers[i], 0, Killers[i].Length);
            }

            for (int i = 0; i < History.Length; i++)
            {
                for (int j = 0; j < History[i].Length; j++)
                {
                    Array.Clear(History[i][j], 0, History[i][j].Length);
                }
            }
        }

        public ThreadUniqueState Clone()
        {
            // TODO
            return new ThreadUniqueState(ThreadId);
        }
    }
}