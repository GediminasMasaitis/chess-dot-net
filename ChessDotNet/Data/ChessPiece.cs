using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;

namespace ChessDotNet.Data
{
    public static class ChessPiece
    {
        public const Piece Color = 1;
        public const Piece White = 0;
        public const Piece Black = 1;
        public const Piece NoColor = 2;
        
        public const Piece Empty = 0;

        public const Piece Pawn = 2 << 1;
        public const Piece Knight = 3 << 1;
        public const Piece Bishop = 4 << 1;
        public const Piece Rook = 5 << 1;
        public const Piece Queen = 6 << 1;
        public const Piece King = 7 << 1;

        public const Piece WhitePawn = White | Pawn;
        public const Piece WhiteKnight = White | Knight;
        public const Piece WhiteBishop = White | Bishop;
        public const Piece WhiteRook = White | Rook;
        public const Piece WhiteQueen = White | Queen;
        public const Piece WhiteKing = White | King;

        public const Piece BlackPawn = Black | Pawn;
        public const Piece BlackKnight = Black | Knight;
        public const Piece BlackBishop = Black | Bishop;
        public const Piece BlackRook = Black | Rook;
        public const Piece BlackQueen = Black | Queen;
        public const Piece BlackKing = Black | King;

        public const int Count = 16;
        public const int NextPiece = 1 << 1;

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