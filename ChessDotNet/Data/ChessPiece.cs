namespace ChessDotNet.Data
{
    /*public enum ChessPiece
    {
        Empty,

        WhitePawn,
        WhiteKnight,
        WhiteBishop,
        WhiteRook,
        WhiteQueen,
        WhiteKing,

        BlackPawn,
        BlackKnight,
        BlackBishop,
        BlackRook,
        BlackQueen,
        BlackKing
    }*/

    public static class ChessPiece
    {
        public const int Empty = 0;

        public const int WhitePawn = 1;
        public const int WhiteKnight = 2;
        public const int WhiteBishop = 3;
        public const int WhiteRook = 4;
        public const int WhiteQueen = 5;
        public const int WhiteKing = 6;

        public const int BlackPawn = 7;
        public const int BlackKnight = 8;
        public const int BlackBishop = 9;
        public const int BlackRook = 10;
        public const int BlackQueen = 11;
        public const int BlackKing = 12;

        public static char ChessPieceToLetter(int chessPiece)
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

        public static char ChessPieceToSymbol(int chessPiece)
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