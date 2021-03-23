using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ChessDotNet.Data;

namespace ChessDotNet.Search2
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TranspositionTableEntry
    {
        private uint _extra;

        public UInt64 Key { get; }
        public Move Move { get; }
        public byte Depth => (byte) (_extra & 0xFF);

        public int Score => unchecked((short)((_extra >> 8) & 0xFFFF));
        public Byte Flag => (byte) (_extra >> 24);

        public TranspositionTableEntry(UInt64 key, Move move, int depth, int score, Byte flag)
        {
            Debug.Assert(depth >= 0);
            Debug.Assert(score < short.MaxValue);
            Debug.Assert(score > short.MinValue);

            Key = key;
            Move = move;
            _extra = (byte) depth;
            _extra |= (uint)(unchecked((ushort)(short)score) << 8);
            _extra |= (uint)(flag << 24);
        }
    }
}