using System;
using System.Linq;
using System.Threading;

namespace ChessDotNet.Search2
{
    public class SearchState
    {
        public ThreadUniqueState[] ThreadStates { get; set; }
        public PrincipalVariationTable PrincipalVariationTable { get; set; }
        public TranspositionTable TranspositionTable { get; set; }
        public byte OriginalColor { get; set; }

        //public AbdadaTable AbdadaTable { get; }

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
        }

        public void OnNewGame()
        {
            TranspositionTable.Clear();
            foreach (var threadState in ThreadStates)
            {
                threadState.OnNewGame();
            }
        }

        public void OnNewSearch()
        {
            TranspositionTable.SetSize(EngineOptions.Hash * 1024 * 1024);
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

        public SearchState Clone()
        {
            var newState = new SearchState();
            newState.TranspositionTable = TranspositionTable.Clone();
            newState.PrincipalVariationTable.Clone();
            newState.ThreadStates = ThreadStates.Select(threadState => threadState.Clone()).ToArray();
            return newState;
        }
    }
}