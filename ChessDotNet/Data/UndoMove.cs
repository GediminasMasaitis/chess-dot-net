using System;
using System.Runtime.InteropServices;

namespace ChessDotNet.Data
{
    [StructLayout(LayoutKind.Auto)]
    public struct UndoMove
    {
        public Move Move { get; set; }
        public CastlingPermission CastlingPermission { get; set; }
        public sbyte EnPassantFileIndex { get; set; }
        public sbyte EnPassantRankIndex { get; set; }
        public ushort FiftyMoveRuleIndex { get; set; }
        public int Evaluation { get; set; }
        public UInt64 Key { get; set; }
        public UInt64 PawnKey { get; set; }
    }
}