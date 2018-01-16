using System;
using ChessDotNet.Testing;

using Position = System.Int32;
using Piece = System.Byte;

namespace ChessDotNet.Data
{
    public struct Move
    {
        public Move(Position from, Position to, Piece piece, Piece takesPiece = 0, bool enPassant = false, Piece? pawnPromoteTo = null)
        {
            From = from;
            To = to;
            Piece = piece;
            TakesPiece = takesPiece;
            EnPassant = enPassant;
            PawnPromoteTo = pawnPromoteTo;

            Castle = (piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing) && Math.Abs(from - to) == 2;
            NullMove = piece == 0;

#if TEST
            Test.Assert(From >= 0);
            Test.Assert(From < 64);
            Test.Assert(To >= 0);
            Test.Assert(To < 64);
            Test.Assert(Piece >= 0);
            Test.Assert(Piece < 13);
            Test.Assert(TakesPiece >= 0);
            Test.Assert(TakesPiece < 13);
            Test.Assert(TakesPiece != 6);
            Test.Assert(TakesPiece != 12);
#endif
        }

        public bool NullMove { get; }
        public Position From { get; }
        public Position To { get; }
        public Piece Piece { get; }
        public Piece TakesPiece { get; }
        public bool EnPassant { get; }
        public Piece? PawnPromoteTo { get; }
        public bool Castle { get; }
        //public int MVVLVAScore => TakesPiece > 0 ? MVVLVAScoreCalculation.Scores[Piece, TakesPiece] : 0;
        public int Key => (From << 16) + To;

        private static string PositionToText(Position position)
        {
            var rank = position / 8;
            var file = position % 8;

            var str = (char)(file + 97) + (rank + 1).ToString();
            return str;
        }

        private static Position TextToPosition(string text)
        {
            var textLower = text.ToLower();
            var file = text[0] - 97;
            var rank = text[1] - 0x31;
            var position = rank*8 + file;
            return position;
        }

        public static Move FromPositionString(Board board, string moveText)
        {
            var from = TextToPosition(moveText.Substring(0, 2));
            var to = TextToPosition(moveText.Substring(2, 2));
            var piece = board.ArrayBoard[from];
            var takesPiece = board.ArrayBoard[to];
            var enPassant = false;
            Piece? pawnPromotesTo = null;
            var isWhite = board.WhiteToMove;
            if (piece == ChessPiece.WhitePawn || piece == ChessPiece.BlackPawn)
            {
                if (from%8 != to%8) // Must be take
                {
                    if (takesPiece == ChessPiece.Empty) // Must be en-passant
                    {
                        takesPiece = isWhite ? ChessPiece.BlackPawn : ChessPiece.WhitePawn;
                        enPassant = true;
                    }
                }
            }

            if (moveText.Length == 5)
            {
                switch (moveText[4])
                {
                    case 'Q':
                        pawnPromotesTo = isWhite ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
                        break;
                    case 'N':
                        pawnPromotesTo = isWhite ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight;
                        break;
                    case 'B':
                        pawnPromotesTo = isWhite ? ChessPiece.WhitePawn : ChessPiece.BlackPawn;
                        break;
                    case 'R':
                        pawnPromotesTo = isWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook;
                        break;
                }
            }

            var move = new Move(from, to, piece, takesPiece, enPassant, pawnPromotesTo);

            return move;
        }

        public string ToPositionString()
        {
            var text = PositionToText(From) + PositionToText(To);
            if (PawnPromoteTo.HasValue)
            {
                char promotionLetter;
                switch (PawnPromoteTo.Value)
                {
                    case ChessPiece.WhiteKnight:
                    case ChessPiece.BlackKnight:
                        promotionLetter = 'N';
                        break;
                    case ChessPiece.WhiteBishop:
                    case ChessPiece.BlackBishop:
                        promotionLetter = 'B';
                        break;
                    case ChessPiece.WhiteRook:
                    case ChessPiece.BlackRook:
                        promotionLetter = 'R';
                        break;
                    case ChessPiece.WhiteQueen:
                    case ChessPiece.BlackQueen:
                        promotionLetter = 'Q';
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(PawnPromoteTo), PawnPromoteTo, "Invalid pawn promotion");
                }
                text += promotionLetter;
            }
            return text;
        }

        public override string ToString()
        {
            var text = ToPositionString();
            return $"{text}; From: {From}, To: {To}, Piece: {Piece}, TakesPiece: {TakesPiece}, EnPassant: {EnPassant}, PawnPromoteTo: {PawnPromoteTo}";
        }
    }
}