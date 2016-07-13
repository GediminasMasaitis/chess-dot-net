namespace ChessDotNet.Data
{
    public struct PieceCounts
    {
        public PieceCounts(int pawns, int knights, int bishops, int rooks, int queens)
        {
            Pawns = pawns;
            Knights = knights;
            Bishops = bishops;
            Rooks = rooks;
            Queens = queens;
        }

        public int Pawns { get; }
        public int Knights { get; }
        public int Bishops { get; }
        public int Rooks { get; }
        public int Queens { get; }
    }
}