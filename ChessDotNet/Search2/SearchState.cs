using System;
using System.Threading;

namespace ChessDotNet.Search2
{
    public class SearchState
    {
        public ThreadUniqueState[] ThreadStates { get; }
        public PrincipalVariationTable PrincipalVariationTable { get; }
        public TranspositionTable TranspositionTable { get; }
        //public AbdadaTable AbdadaTable { get; }

        //public UInt64[,] Killers => _threadUniqueState.Value.Killers;
        //public int[,,] History => _threadUniqueState.Value.History;
        //public int[,,] Cutoff => _threadUniqueState.Value.Cutoff;
        //public List<Move>[] Moves => _threadUniqueState.Value.Moves;



        //private readonly ThreadLocal<ThreadUniqueState> _threadUniqueState;

        public SearchState()
        {
            TranspositionTable = new TranspositionTable();
            PrincipalVariationTable = new PrincipalVariationTable();
            //AbdadaTable = new AbdadaTable();
            ThreadStates = new ThreadUniqueState[SearchConstants.ThreadCount];
            for (int i = 0; i < ThreadStates.Length; i++)
            {
                ThreadStates[i] = new ThreadUniqueState(i);
            }

            //_threadUniqueState = new ThreadLocal<ThreadUniqueState>(() => new ThreadUniqueState(), true);

        }

        public void OnNewSearch(SearchOptions options)
        {
            TranspositionTable.SetSize(options.Hash * 1024 * 1024);
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