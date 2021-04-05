using System;
using ChessDotNet.Data;

namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public static class NnueConstants
    {
        public const byte white = 0;
        public const byte black = 1;

        public const byte blank = 0;
        public const byte wking = 1;
        public const byte wqueen = 2;
        public const byte wrook = 3;
        public const byte wbishop = 4;
        public const byte wknight = 5;
        public const byte wpawn = 6;
        public const byte bking = 7;
        public const byte bqueen = 8;
        public const byte brook = 9;
        public const byte bbishop = 10;
        public const byte bknight = 11;
        public const byte bpawn = 12;


        public static byte GetNnuePiece(byte piece)
        {
            byte pieceNum;
            switch (piece)
            {
                case ChessPiece.WhiteKing: pieceNum = 1; break;
                case ChessPiece.WhiteQueen: pieceNum = 2; break;
                case ChessPiece.WhiteRook: pieceNum = 3; break;
                case ChessPiece.WhiteBishop: pieceNum = 4; break;
                case ChessPiece.WhiteKnight: pieceNum = 5; break;
                case ChessPiece.WhitePawn: pieceNum = 6; break;

                case ChessPiece.BlackKing: pieceNum = 7; break;
                case ChessPiece.BlackQueen: pieceNum = 8; break;
                case ChessPiece.BlackRook: pieceNum = 9; break;
                case ChessPiece.BlackBishop: pieceNum = 10; break;
                case ChessPiece.BlackKnight: pieceNum = 11; break;
                case ChessPiece.BlackPawn: pieceNum = 12; break;
                default: throw new Exception();
            }
            return pieceNum;
        }
    }
}