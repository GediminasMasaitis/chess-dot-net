using System;

namespace ChessDotNet.Data
{
    [Flags]
    public enum CastlingPermission : byte
    {
        None = 0,

        WhiteQueen = 1 << 0,
        WhiteKing = 1 << 1,

        BlackQueen = 1 << 2,
        BlackKing = 1 << 3,
        
        All = WhiteKing | WhiteQueen | BlackKing | BlackQueen
    }
}