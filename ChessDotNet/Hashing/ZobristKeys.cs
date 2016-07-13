using System;
using ChessDotNet.Data;

namespace ChessDotNet.Hashing
{
    static class ZobristKeys
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

        public static ulong CalculateKey(Board board)
        {
            var key = 0UL;
            for (var i = 0; i < 64; i++)
            {

                switch (board.ArrayBoard[i])
                {
                    case ChessPiece.Empty:
                        break;

                    case ChessPiece.WhitePawn:
                        key ^= ZPieces[0, 0, i];
                        break;
                    case ChessPiece.BlackPawn:
                        key ^= ZPieces[1, 0, i];
                        break;

                    case ChessPiece.WhiteKnight:
                        key ^= ZPieces[0, 1, i];
                        break;
                    case ChessPiece.BlackKnight:
                        key ^= ZPieces[1, 1, i];
                        break;

                    case ChessPiece.WhiteBishop:
                        key ^= ZPieces[0, 2, i];
                        break;
                    case ChessPiece.BlackBishop:
                        key ^= ZPieces[1, 2, i];
                        break;

                    case ChessPiece.WhiteRook:
                        key ^= ZPieces[0, 3, i];
                        break;
                    case ChessPiece.BlackRook:
                        key ^= ZPieces[1, 3, i];
                        break;

                    case ChessPiece.WhiteQueen:
                        key ^= ZPieces[0, 4, i];
                        break;
                    case ChessPiece.BlackQueen:
                        key ^= ZPieces[1, 4, i];
                        break;

                    case ChessPiece.WhiteKing:
                        key ^= ZPieces[0, 5, i];
                        break;
                    case ChessPiece.BlackKing:
                        key ^= ZPieces[1, 5, i];
                        break;
                }
            }

            if (board.EnPassantFileIndex >= 0)
            {
                key ^= ZEnPassant[board.EnPassantFileIndex];
            }

            if (board.WhiteCanCastleQueenSide)
            {
                key ^= ZCastle[0];
            }
            if (board.WhiteCanCastleKingSide)
            {
                key ^= ZCastle[1];
            }
            if (board.BlackCanCastleQueenSide)
            {
                key ^= ZCastle[2];
            }
            if (board.BlackCanCastleKingSide)
            {
                key ^= ZCastle[3];
            }

            key ^= ZToMove[board.WhiteToMove ? 0 : 1];

            return key;
        }

    }
}