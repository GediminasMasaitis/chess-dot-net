using System;
using ChessDotNet.Data;

namespace ChessDotNet.Hashing
{
    public static class ZobristKeys
    {
        public static ulong[,] ZPieces { get; }
        public static ulong[] ZEnPassant { get; }
        public static ulong[] ZCastle { get; }
        public static ulong ZWhiteToMove { get; }

        static ZobristKeys()
        {
            ZPieces = new ulong[64, 13];
            ZEnPassant = new ulong[8];
            ZCastle = new ulong[4];

            var rng = new Random(0);
            for (var i = 1; i < 64; i++)
            {
                for (var j = 0; j < 13; j++)
                {
                    ZPieces[i, j] = NextKey(rng);
                }
            }

            for (var i = 0; i < 8; i++)
            {
                ZEnPassant[i] = NextKey(rng);
            }

            for (var i = 0; i < 4; i++)
            {
                ZCastle[i] = NextKey(rng);
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
                    key ^= ZPieces[i, piece];
                }
            }

            if (board.EnPassantFileIndex >= 0)
            {
                key ^= ZEnPassant[board.EnPassantFileIndex];
            }

            for (var i = 0; i < 4; i++)
            {
                if (board.CastlingPermissions[i])
                {
                    key ^= ZCastle[i];
                }
            }

            if (board.WhiteToMove)
            {
                key ^= ZWhiteToMove;
            }

            return key;
        }

    }
}