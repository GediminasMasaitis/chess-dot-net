using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChessDotNet.Perft.Suite
{
    //class PerftSuiteExpectedResult
    //{

    //}

    class PerftSuiteEntry
    {
        public string Fen { get; }
        public int Depth { get; }

        public PerftSuiteEntry(string fen, int depth)
        {
            Fen = fen;
            Depth = depth;
        }
    }

    public class PerftSuiteRunner
    {
        private readonly PerftRunner _runner;

        public PerftSuiteRunner(PerftRunner runner)
        {
            _runner = runner;
        }

        public void RunSuite(string path)
        {
            var entries = ReadFens(path);
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                _runner.Test(entry.Fen, entry.Depth);
            }
        }

        private IList<PerftSuiteEntry> ReadFens(string path)
        {
            var fens = new List<PerftSuiteEntry>();
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var parts = line.Split(';');
                var entry = new PerftSuiteEntry(parts[0], parts.Length - 1);
                fens.Add(entry);
            }

            return fens;
        }
    }
}
