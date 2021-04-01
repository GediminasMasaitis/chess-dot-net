using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ChessDotNet.Data;

namespace ChessDotNet.Search2
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TranspositionTableEntry
    {
        public byte DepthCheckAndFlag;
        public byte Depth => (byte) (DepthCheckAndFlag >> 3);
        //public bool InCheck => ((DepthCheckAndFlag >> 2) & 1) != 0;
        public byte Flag => (byte) (DepthCheckAndFlag & 3);

        //public byte Depth;
        //public byte Flag;

        public ulong PartialKeyAndScore;
        public ulong PartialKey => PartialKeyAndScore & ~0xFFFFUL;
        public short Score => unchecked((short)(PartialKeyAndScore & 0xFFFFUL));

        //public short Score;
        //public ulong Key;

        public Move Move;
        //public ushort pad;

        //public UInt64 Key2 { get; set; }

        public TranspositionTableEntry(UInt64 key, Move move, byte depth, short score, Byte flag)
        {
            Debug.Assert(depth >= 0);
            Debug.Assert(score < short.MaxValue);
            Debug.Assert(score > short.MinValue);

            PartialKeyAndScore = (key & ~0xFFFFUL) | unchecked((ushort)score);
            //Key = key;
            //Score = score;

            DepthCheckAndFlag = (byte)((depth << 3) /*| (byte)(inCheck ? (1 << 2) : 0)*/ | flag);
            //Depth = depth;
            //Flag = flag;

            Move = move;

            //Key2 = key2;
            //pad = 0;
        }
    }
}