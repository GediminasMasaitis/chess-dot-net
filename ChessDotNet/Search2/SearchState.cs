using System;
using System.Collections.Generic;
using System.Threading;
using ChessDotNet.Data;

namespace ChessDotNet.Search2
{
    public class ThreadUniqueState
    {
        public UInt64[,] Killers { get; }
        public int[,,] History { get; }
        public int[,,] Cutoff { get; }
        public List<Move>[] Moves { get; }
        public Random Rng { get; }

        public ThreadUniqueState(int threadId)
        {
            Killers = new UInt64[SearchConstants.MaxDepth, 2]; // Non-captures causing beta cutoffs
            History = new int[2, 64, 64];
            Cutoff = new int[2, 64, 64];
            Moves = new List<Move>[SearchConstants.MaxDepth];
            for (int i = 0; i < Moves.Length; i++)
            {
                Moves[i] = new List<Move>();
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

    public class AbdadaTable
    {
        private readonly ulong[,] _table;

        private const int TableSize = 32768;
        private const int TableWays = 4;
        private const int DeferDepth = 3;

        public AbdadaTable()
        {
            _table = new ulong[TableSize, TableWays];
        }

        public bool DeferMove(ulong move_hash, int depth)
        {
            if (depth < DeferDepth) // note 1
            {
                return false;
            }

            var index = move_hash & (TableSize - 1);

            for (var i = 0; i < TableWays; i++)  // note 2
            {
                if (_table[index, i] == move_hash)
                {
                    return true;
                }
            }
            return false;
        }

        public void StartingSearch(ulong move_hash, int depth)
        {
            if (depth < DeferDepth)
            {
                return;
            }

            var index = move_hash & (TableSize - 1);
            for (var i = 0; i < TableWays; i++)
            {
                if (_table[index, i] == 0)
                {
                    _table[index, i] = move_hash;
                    return;
                }

                if (_table[index, i] == move_hash) // note 3.1
                {
                    return;
                }
            }
            _table[index, 0] = move_hash;
        }

        public void FinishedSearch(ulong move_hash, int depth)
        {
            if (depth < DeferDepth)
            {
                return;
            }

            var index = move_hash & (TableSize - 1);
            for (var i = 0; i < TableWays; i++)
            {
                if (_table[index, i] == move_hash) // note 3.2
                {
                    _table[index, i] = 0;
                }
            }

        }
    }

    public class SearchState
    {
        public ThreadUniqueState[] ThreadStates { get; }
        public PrincipalVariationTable PrincipalVariationTable { get; }
        public TranspositionTable TranspositionTable { get; }
        public AbdadaTable AbdadaTable { get; }

        //public UInt64[,] Killers => _threadUniqueState.Value.Killers;
        //public int[,,] History => _threadUniqueState.Value.History;
        //public int[,,] Cutoff => _threadUniqueState.Value.Cutoff;
        //public List<Move>[] Moves => _threadUniqueState.Value.Moves;



        //private readonly ThreadLocal<ThreadUniqueState> _threadUniqueState;

        public SearchState()
        {
            TranspositionTable = new TranspositionTable(1024 * 1024 * 32);
            PrincipalVariationTable = new PrincipalVariationTable();
            AbdadaTable = new AbdadaTable();
            ThreadStates = new ThreadUniqueState[SearchConstants.ThreadCount];
            for (int i = 0; i < ThreadStates.Length; i++)
            {
                ThreadStates[i] = new ThreadUniqueState(i);
            }

            //_threadUniqueState = new ThreadLocal<ThreadUniqueState>(() => new ThreadUniqueState(), true);

        }

        public void OnNewSearch()
        {
            foreach (var threadState in ThreadStates)
            {
                threadState.OnNewSearch();
            }
            PrincipalVariationTable.Clear();
        }

        public void OnIterativeDeepen()
        {
            foreach (var threadState in ThreadStates)
            {
                threadState.OnIterativeDeepen();
            }

            //Table.Clear();
        }

        public void Synchronize()
        {
            var mainState = ThreadStates[0];
            for (var i = 1; i < ThreadStates.Length; i++)
            {
                var helperState = ThreadStates[i];
                Array.Copy(mainState.Killers, helperState.Killers, mainState.Killers.Length);
                Array.Copy(mainState.History, helperState.History, mainState.History.Length);
                Array.Copy(mainState.Cutoff, helperState.Cutoff, mainState.Cutoff.Length);
            }
        }
    }
}