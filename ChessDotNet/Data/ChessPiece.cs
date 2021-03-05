using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;

namespace ChessDotNet.Data
{
    public static class ChessPiece
    {
        public const Piece Empty = 0;

        public const Piece WhitePawn = 1;
        public const Piece WhiteKnight = 2;
        public const Piece WhiteBishop = 3;
        public const Piece WhiteRook = 4;
        public const Piece WhiteQueen = 5;
        public const Piece WhiteKing = 6;

        public const Piece BlackPawn = 7;
        public const Piece BlackKnight = 8;
        public const Piece BlackBishop = 9;
        public const Piece BlackRook = 10;
        public const Piece BlackQueen = 11;
        public const Piece BlackKing = 12;

        public static char ChessPieceToLetter(Piece chessPiece)
        {
            switch (chessPiece)
            {
                case ChessPiece.Empty:
                    return ' ';

                case ChessPiece.WhitePawn:
                    return 'P';
                case ChessPiece.BlackPawn:
                    return 'p';

                case ChessPiece.WhiteKnight:
                    return 'N';
                case ChessPiece.BlackKnight:
                    return 'n';

                case ChessPiece.WhiteBishop:
                    return 'B';
                case ChessPiece.BlackBishop:
                    return 'b';

                case ChessPiece.WhiteRook:
                    return 'R';
                case ChessPiece.BlackRook:
                    return 'r';

                case ChessPiece.WhiteQueen:
                    return 'Q';
                case ChessPiece.BlackQueen:
                    return 'q';

                case ChessPiece.WhiteKing:
                    return 'K';
                case ChessPiece.BlackKing:
                    return 'k';
                default:
                    return '?';
            }
        }

        public static char ChessPieceToSymbol(Piece chessPiece)
        {
            switch (chessPiece)
            {
                case ChessPiece.Empty:
                    return ' ';

                case ChessPiece.WhitePawn:
                    return (char)0x2659;
                case ChessPiece.BlackPawn:
                    return (char)0x265F;

                case ChessPiece.WhiteKnight:
                    return (char)0x2658;
                case ChessPiece.BlackKnight:
                    return (char)0x265E;

                case ChessPiece.WhiteBishop:
                    return (char)0x2657;
                case ChessPiece.BlackBishop:
                    return (char)0x265D;

                case ChessPiece.WhiteRook:
                    return (char)0x2656;
                case ChessPiece.BlackRook:
                    return (char)0x265C;

                case ChessPiece.WhiteQueen:
                    return (char)0x2655;
                case ChessPiece.BlackQueen:
                    return (char)0x265B;

                case ChessPiece.WhiteKing:
                    return (char)0x265A;
                case ChessPiece.BlackKing:
                    return (char)0x2659;
                default:
                    return '?';
            }
        }
    }


}