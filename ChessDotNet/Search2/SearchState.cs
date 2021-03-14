using System;
using System.Collections.Generic;
using ChessDotNet.Data;

namespace ChessDotNet.Search2
{
    public class SearchState
    {
        public PrincipalVariationTable PrincipalVariationTable { get; }
        public TranspositionTable TranspositionTable { get; }
        public UInt64[,] Killers { get; }
        public int[,] History { get; }
        public List<Move>[] Moves { get; }


        public SearchState()
        {
            TranspositionTable = new TranspositionTable(1024 * 1024 * 32);
            PrincipalVariationTable = new PrincipalVariationTable();
            Killers = new UInt64[SearchConstants.MaxDepth, 2]; // Non-captures causing beta cutoffs
            History = new int[64, 64];
            Moves = new List<Move>[SearchConstants.MaxDepth];
            for (int i = 0; i < Moves.Length; i++)
            {
                Moves[i] = new List<Move>();
            }
        }

        public void OnNewSearch()
        {
            Array.Clear(Killers, 0, Killers.Length);
            Array.Clear(History, 0, History.Length);
            PrincipalVariationTable.Clear();
            //Table.Clear();
        }

        public void OnIterativeDeepen()
        {
            //Array.Clear(Killers, 0, Killers.Length);
            //Array.Clear(History, 0, History.Length);

            //Table.Clear();
        }
    }
}