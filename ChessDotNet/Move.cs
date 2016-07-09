namespace ChessDotNet
{
    public struct Move
    {
        public Move(int from, int to, ChessPiece piece)
        {
            From = from;
            To = to;
            Piece = piece;
        }

        public int From { get; }
        public int To { get; }
        public ChessPiece Piece { get; }
    }
}