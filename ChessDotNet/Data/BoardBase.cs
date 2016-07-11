namespace ChessDotNet.Data
{
    public class BoardBase
    {
        public bool WhiteToMove { get; set; }

        public bool WhiteCanCastleKingSide { get; set; }
        public bool WhiteCanCastleQueenSide { get; set; }
        public bool BlackCanCastleKingSide { get; set; }
        public bool BlackCanCastleQueenSide { get; set; }
    }
}