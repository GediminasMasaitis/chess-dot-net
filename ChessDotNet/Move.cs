using System;

namespace ChessDotNet
{
    public struct Move
    {
        public Move(int from, int to, ChessPiece piece, bool enPassant = false, ChessPiece? pawnPromoteTo = null)
        {
            From = from;
            To = to;
            Piece = piece;
            EnPassant = enPassant;
            PawnPromoteTo = pawnPromoteTo;
        }

        public int From { get; }
        public int To { get; }
        public ChessPiece Piece { get; }
        public bool EnPassant { get; }
        public ChessPiece? PawnPromoteTo { get; }

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
            return $"{text}; From: {From}, To: {To}, Piece: {Piece}, EnPassant: {EnPassant}, PawnPromoteTo: {PawnPromoteTo}";
        }
    }
}