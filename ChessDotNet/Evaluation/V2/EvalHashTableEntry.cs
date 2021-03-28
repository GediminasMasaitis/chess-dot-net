using System.Runtime.InteropServices;

namespace ChessDotNet.Evaluation.V2
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct EvalHashTableEntry
    {
        public ulong Key { get; }
        public int Score { get; }

        public EvalHashTableEntry(ulong key, int score)
        {
            Key = key;
            Score = score;
        }
    }
}