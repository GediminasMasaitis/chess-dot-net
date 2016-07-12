using System;
using ChessDotNet.Data;

namespace ChessDotNet.Hashing
{
    class ZobristKeys
    {
        private static ulong[,,] ZPieces { get; }
        private static ulong[] ZEnPassant { get; }
        private static ulong[] ZCastle { get; }
        private static ulong[] ZToMove { get; }

        static ZobristKeys()
        {
            ZPieces = new ulong[2, 6, 64];
            ZEnPassant = new ulong[8];
            ZCastle = new ulong[4];
            ZToMove = new ulong[2];

            var rng = new Random();
            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 6; j++)
                {
                    for (var k = 0; k < 64; k++)
                    {
                        ZPieces[i, j, k] = NextKey(rng);
                    }
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

            for (var i = 0; i < 2; i++)
            {
                ZToMove[i] = NextKey(rng);
            }
        }

        private static ulong NextKey(Random rng)
        {
            var bytes = new byte[8];
            rng.NextBytes(bytes);
            var num = BitConverter.ToUInt64(bytes, 0);
            return num;
        }

        public ulong GetKey(BitBoards bitBoards)
        {
            var key = 0UL;
            for (var i = 0; i < 64; i++)
            {
                if ((bitBoards.EmptySquares & (1UL << i)) != 0)
                {
                    // skip
                }

                else if ((bitBoards.WhitePawns & (1UL << i)) != 0)
                {
                    key ^= ZPieces[0, 0, i];
                }
                else if ((bitBoards.BlackPawns & (1UL << i)) != 0)
                {
                    key ^= ZPieces[1, 0, i];
                }

                else if ((bitBoards.WhiteNights & (1UL << i)) != 0)
                {
                    key ^= ZPieces[0, 1, i];
                }
                else if ((bitBoards.BlackNights & (1UL << i)) != 0)
                {
                    key ^= ZPieces[1, 1, i];
                }

                else if ((bitBoards.WhiteBishops & (1UL << i)) != 0)
                {
                    key ^= ZPieces[0, 2, i];
                }
                else if ((bitBoards.BlackBishops & (1UL << i)) != 0)
                {
                    key ^= ZPieces[1, 2, i];
                }

                else if ((bitBoards.WhiteRooks & (1UL << i)) != 0)
                {
                    key ^= ZPieces[0, 3, i];
                }
                else if ((bitBoards.BlackRooks & (1UL << i)) != 0)
                {
                    key ^= ZPieces[1, 3, i];
                }

                else if ((bitBoards.WhiteQueens & (1UL << i)) != 0)
                {
                    key ^= ZPieces[0, 4, i];
                }
                else if ((bitBoards.BlackQueens & (1UL << i)) != 0)
                {
                    key ^= ZPieces[1, 4, i];
                }

                else if ((bitBoards.WhiteKings & (1UL << i)) != 0)
                {
                    key ^= ZPieces[0, 5, i];
                }
                else if ((bitBoards.BlackKings & (1UL << i)) != 0)
                {
                    key ^= ZPieces[1, 5, i];
                }
            }

            if (bitBoards.EnPassantFileIndex >= 0)
            {
                key ^= ZEnPassant[bitBoards.EnPassantFileIndex];
            }

            if (bitBoards.WhiteCanCastleQueenSide)
            {
                key ^= ZCastle[0];
            }
            if (bitBoards.WhiteCanCastleKingSide)
            {
                key ^= ZCastle[1];
            }
            if (bitBoards.BlackCanCastleQueenSide)
            {
                key ^= ZCastle[2];
            }
            if (bitBoards.BlackCanCastleKingSide)
            {
                key ^= ZCastle[3];
            }

            key ^= ZToMove[bitBoards.WhiteToMove ? 0 : 1];

            return key;
        }

    }
}