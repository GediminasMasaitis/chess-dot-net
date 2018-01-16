using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet.Perft
{
    public class PerftSuite
    {
        public PerftRunner Runner { get; }

        private class PerftSuiteEntry
        {
            public PerftSuiteEntry(string fen, int depth, ulong expectedResults)
            {
                Fen = fen;
                Depth = depth;
                ExpectedResults = expectedResults;
            }

            public string Fen { get; }
            public int Depth { get; }
            public ulong ExpectedResults { get; }
        }

        private IList<PerftSuiteEntry> Entries { get; set; }

        public PerftSuite(PerftRunner runner)
        {
            Runner = runner;
            Entries = new PerftSuiteEntry[]
            {
                //--Illegal ep move #1
                new PerftSuiteEntry("3k4/3p4/8/K1P4r/8/8/8/8 b - - 0 1", 6, 1134888),

                //--Illegal ep move #2
                new PerftSuiteEntry("8/8/4k3/8/2p5/8/B2P2K1/8 w - - 0 1", 6, 1015133),

                //--EP Capture Checks Opponent
                new PerftSuiteEntry("8/8/1k6/2b5/2pP4/8/5K2/8 b - d3 0 1", 6, 1440467),

                //--Short Castling Gives Check
                new PerftSuiteEntry("5k2/8/8/8/8/8/8/4K2R w K - 0 1", 6, 661072),

                //--Long Castling Gives Check
                new PerftSuiteEntry("3k4/8/8/8/8/8/8/R3K3 w Q - 0 1", 6, 803711),

                //--Castle Rights
                new PerftSuiteEntry("r3k2r/1b4bq/8/8/8/8/7B/R3K2R w KQkq - 0 1", 4, 1274206),

                //--Castling Prevented
                new PerftSuiteEntry("r3k2r/8/3Q4/8/8/5q2/8/R3K2R b KQkq - 0 1", 4, 1720476),

                //--Promote out of Check
                new PerftSuiteEntry("2K2r2/4P3/8/8/8/8/8/3k4 w - - 0 1", 6, 3821001),

                //--Discovered Check
                new PerftSuiteEntry("8/8/1P2K3/8/2n5/1q6/8/5k2 b - - 0 1", 5, 1004658),

                //--Promote to give check
                new PerftSuiteEntry("4k3/1P6/8/8/8/8/K7/8 w - - 0 1", 6, 217342),

                //--Under Promote to give check
                new PerftSuiteEntry("8/P1k5/K7/8/8/8/8/8 w - - 0 1", 6, 92683),

                //--Self Stalemate
                new PerftSuiteEntry("K1k5/8/P7/8/8/8/8/8 w - - 0 1", 6, 2217),

                //--Stalemate & Checkmate
                new PerftSuiteEntry("8/k1P5/8/1K6/8/8/8/8 w - - 0 1", 7, 567584),    

                //--Stalemate & Checkmate
                new PerftSuiteEntry("8/8/2k5/5q2/5n2/8/5K2/8 b - - 0 1", 4, 23527),
            };
        }

        public void Run()
        {
            foreach (var entry in Entries)
            {
                Runner.Test(entry.Fen, entry.Depth);
            }
        }
    }
}
