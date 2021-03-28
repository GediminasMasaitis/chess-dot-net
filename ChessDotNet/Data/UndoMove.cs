using System;

namespace ChessDotNet.Data
{
    public struct UndoMove
    {
        public Move Move { get; set; }
        public CastlingPermission CastlingPermission { get; set; }
        public int EnPassantFileIndex { get; set; }
        public int EnPassantRankIndex { get; set; }
        public int FiftyMoveRule { get; set; }
        public UInt64 Key { get; set; }
        public UInt64 PawnKey { get; set; }
    }
}