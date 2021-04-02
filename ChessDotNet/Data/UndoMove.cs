using System;
using System.Runtime.InteropServices;

namespace ChessDotNet.Data
{
    [Flags]
    public enum UndoMoveFlags : byte
    {
        HasPinnedPieces = 1 << 0,
        HasCheckers = 1 << 1,
    }

    [StructLayout(LayoutKind.Auto)]
    public struct UndoMove
    {
        public CastlingPermission CastlingPermission { get; set; }
        public sbyte EnPassantFileIndex { get; set; }
        public sbyte EnPassantRankIndex { get; set; }
        public ushort FiftyMoveRule { get; set; }
        public Move Move { get; set; }
        public UInt64 Key { get; set; }
        public UInt64 PawnKey { get; set; }
        public ulong PinnedPieces { get; set; }
        
        public ulong Checkers { get; set; }
        public bool HasPinnedPieces { get; set; }
        public bool HasCheckers { get; set; }
    }
}