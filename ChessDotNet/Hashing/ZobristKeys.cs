using System;
using System.Linq;
using System.Security.Cryptography;
using ChessDotNet.Data;

namespace ChessDotNet.Hashing
{
    public static class ZobristKeys
    {
        public static ulong[][] ZPieces { get; }
        public static ulong[] ZEnPassant { get; }
        public static ulong[] ZCastle { get; }
        public static ulong ZWhiteToMove { get; }

        static ZobristKeys()
        {
            ZPieces = new ulong[64][];
            for (int i = 0; i < ZPieces.Length; i++)
            {
                ZPieces[i] = new ulong[ChessPiece.Count];
            }
            
            var rng = new Random(0);
            for (var i = 0; i < 64; i++)
            {
                for (var j = 0; j < ChessPiece.Count; j++)
                {
                    ZPieces[i][j] = NextKey(rng);
                }
            }
            
            ZEnPassant = new ulong[8];
            for (var i = 0; i < 8; i++)
            {
                ZEnPassant[i] = NextKey(rng);
            }

            var castleLength = (byte)CastlingPermission.All + 1;
            ZCastle = new ulong[castleLength];
            ZCastle[(int)CastlingPermission.WhiteQueen] = NextKey(rng);
            ZCastle[(int)CastlingPermission.WhiteKing] = NextKey(rng);
            ZCastle[(int)CastlingPermission.BlackQueen] = NextKey(rng);
            ZCastle[(int)CastlingPermission.BlackKing] = NextKey(rng);
            for (int i = 1; i < castleLength; i++)
            {
                if
                (
                    i == (int) CastlingPermission.WhiteQueen
                    || i == (int) CastlingPermission.WhiteKing
                    || i == (int) CastlingPermission.BlackQueen
                    || i == (int) CastlingPermission.BlackKing
                )
                {
                    continue;
                }

                ulong key = 0UL;
                for (int j = 0; j < 4; j++)
                {
                    var existingCastleIndex = 1 << j;
                    var bitSet = (i & existingCastleIndex) != 0;
                    if (bitSet)
                    {
                        key ^= ZCastle[existingCastleIndex];
                    }
                }
                ZCastle[i] = key;
            }
            
            ZWhiteToMove = NextKey(rng);
        }

        private static ulong NextKey(Random rng)
        {
            var bytes = new byte[8];
            rng.NextBytes(bytes);
            var num = BitConverter.ToUInt64(bytes, 0);
            return num;
        }

        public static ulong CalculateKey(Board board)
        {
            var key = 0UL;
            for (var i = 0; i < 64; i++)
            {
                var piece = board.ArrayBoard[i];
                if (piece != ChessPiece.Empty)
                {
                    key ^= ZPieces[i][piece];
                }
            }

            if (board.EnPassantFileIndex >= 0)
            {
                key ^= ZEnPassant[board.EnPassantFileIndex];
            }
            
            if ((board.CastlingPermissions & CastlingPermission.WhiteQueen) != CastlingPermission.None)
            {
                key ^= ZCastle[(byte)CastlingPermission.WhiteQueen];
            }
            if ((board.CastlingPermissions & CastlingPermission.WhiteKing) != CastlingPermission.None)
            {
                key ^= ZCastle[(byte)CastlingPermission.WhiteKing];
            }
            if ((board.CastlingPermissions & CastlingPermission.BlackQueen) != CastlingPermission.None)
            {
                key ^= ZCastle[(byte)CastlingPermission.BlackQueen];
            }
            if ((board.CastlingPermissions & CastlingPermission.BlackKing) != CastlingPermission.None)
            {
                key ^= ZCastle[(byte)CastlingPermission.BlackKing];
            }

            if (board.WhiteToMove)
            {
                key ^= ZWhiteToMove;
            }

            return key;
        }

        public static ulong CalculatePawnKey(Board board)
        {
            var key = 0UL;
            for (var i = 0; i < 64; i++)
            {
                var piece = board.ArrayBoard[i];
                if((piece & ~ChessPiece.Color) == ChessPiece.Pawn)
                {
                    key ^= ZPieces[i][piece];
                }
            }
            return key;
        }

    }

    public static class ZobristKeys2
    {
        public static ulong[][] ZPieces { get; }
        public static ulong[] ZEnPassant { get; }
        public static ulong[] ZCastle { get; }
        public static ulong ZWhiteToMove { get; }

        static ZobristKeys2()
        {
            ZPieces = new ulong[64][];
            for (int i = 0; i < ZPieces.Length; i++)
            {
                ZPieces[i] = new ulong[ChessPiece.Count];
            }

            var rng = new Random(1);
            for (var i = 0; i < 64; i++)
            {
                for (var j = 0; j < ChessPiece.Count; j++)
                {
                    ZPieces[i][j] = NextKey(rng);
                }
            }

            ZEnPassant = new ulong[8];
            for (var i = 0; i < 8; i++)
            {
                ZEnPassant[i] = NextKey(rng);
            }

            var castleLength = (byte)CastlingPermission.All + 1;
            ZCastle = new ulong[castleLength];
            ZCastle[(int)CastlingPermission.WhiteQueen] = NextKey(rng);
            ZCastle[(int)CastlingPermission.WhiteKing] = NextKey(rng);
            ZCastle[(int)CastlingPermission.BlackQueen] = NextKey(rng);
            ZCastle[(int)CastlingPermission.BlackKing] = NextKey(rng);
            for (int i = 1; i < castleLength; i++)
            {
                if
                (
                    i == (int)CastlingPermission.WhiteQueen
                    || i == (int)CastlingPermission.WhiteKing
                    || i == (int)CastlingPermission.BlackQueen
                    || i == (int)CastlingPermission.BlackKing
                )
                {
                    continue;
                }

                ulong key = 0UL;
                for (int j = 0; j < 4; j++)
                {
                    var existingCastleIndex = 1 << j;
                    var bitSet = (i & existingCastleIndex) != 0;
                    if (bitSet)
                    {
                        key ^= ZCastle[existingCastleIndex];
                    }
                }
                ZCastle[i] = key;
            }
            //for (var i = 0; i < castleLength; i++)
            //{
            //    ZCastle[i] = NextKey(rng);
            //}

            ZWhiteToMove = NextKey(rng);
        }

        private static ulong NextKey(Random rng)
        {
            var bytes = new byte[8];
            rng.NextBytes(bytes);
            var num = BitConverter.ToUInt64(bytes, 0);
            return num;
        }

        public static ulong CalculateKey(Board board)
        {
            var key = 0UL;
            for (var i = 0; i < 64; i++)
            {
                var piece = board.ArrayBoard[i];
                if (piece != ChessPiece.Empty)
                {
                    key ^= ZPieces[i][piece];
                }
            }

            if (board.EnPassantFileIndex >= 0)
            {
                key ^= ZEnPassant[board.EnPassantFileIndex];
            }

            if ((board.CastlingPermissions & CastlingPermission.WhiteQueen) != CastlingPermission.None)
            {
                key ^= ZCastle[(byte)CastlingPermission.WhiteQueen];
            }
            if ((board.CastlingPermissions & CastlingPermission.WhiteKing) != CastlingPermission.None)
            {
                key ^= ZCastle[(byte)CastlingPermission.WhiteKing];
            }
            if ((board.CastlingPermissions & CastlingPermission.BlackQueen) != CastlingPermission.None)
            {
                key ^= ZCastle[(byte)CastlingPermission.BlackQueen];
            }
            if ((board.CastlingPermissions & CastlingPermission.BlackKing) != CastlingPermission.None)
            {
                key ^= ZCastle[(byte)CastlingPermission.BlackKing];
            }

            if (board.WhiteToMove)
            {
                key ^= ZWhiteToMove;
            }

            return key;
        }

        public static ulong CalculatePawnKey(Board board)
        {
            var key = 0UL;
            for (var i = 0; i < 64; i++)
            {
                var piece = board.ArrayBoard[i];
                if ((piece & ~ChessPiece.Color) == ChessPiece.Pawn)
                {
                    key ^= ZPieces[i][piece];
                }
            }
            return key;
        }

    }
}