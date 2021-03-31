using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChessDotNet.Data;
using ChessDotNet.Search2;
using Newtonsoft.Json;

namespace ChessDotNet.Testing
{
    public class SavedState
    {
        public Board Board { get; set; }
        public SearchState State { get; set; }
    }

    public static class State
    {
        public static void SaveState(Board board, SearchState state)
        {
            var savedState = new SavedState();
            savedState.Board = board;
            savedState.State = state;

            var json = JsonConvert.SerializeObject(savedState);
            File.WriteAllText($"state-{DateTime.Now:yyyy-MM-dd-hh-mm-ss-fff}.json", json);
        }

        public static SavedState LoadState(string path)
        {
            var json = File.ReadAllText(path);
            var savedState = JsonConvert.DeserializeObject<SavedState>(json);
            return savedState;
        }
    }
}
