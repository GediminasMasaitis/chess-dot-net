using System;

namespace ChessDotNet.Data
{
    public struct Move
    {
        public Move(int from, int to, int piece, int takesPiece = 0, bool enPassant = false, int? pawnPromoteTo = null)
        {
            From = from;
            To = to;
            Piece = piece;
            TakesPiece = takesPiece;
            EnPassant = enPassant;
            PawnPromoteTo = pawnPromoteTo;

            Castle = (piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing) && Math.Abs(from - to) == 2;
        }

        public int From { get; }
        public int To { get; }
        public int Piece { get; }
        public int TakesPiece { get; }
        public bool EnPassant { get; }
        public int? PawnPromoteTo { get; }
        public bool Castle { get; }

        private string PositionToText(int position)
        {
            var rank = position / 8;
            var file = position % 8;

            var str = (char)(97 + file) + (rank + 1).ToString();
            return str;
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