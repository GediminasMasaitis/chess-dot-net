namespace ChessDotNet
{
    public struct Move
    {
        public Move(int from, int to, ChessPiece piece, bool enPassant = false)
        {
            From = from;
            To = to;
            Piece = piece;
            EnPassant = enPassant;
        }

        public int From { get; }
        public int To { get; }
        public ChessPiece Piece { get; }
        public bool EnPassant { get; }

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
            return text;
        }

        public override string ToString()
        {
            var text = ToPositionString();
            return $"{text}; From: {From}, To: {To}, Piece: {Piece}, EnPassant: {EnPassant}";
        }
    }
}