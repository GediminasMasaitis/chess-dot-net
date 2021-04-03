namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public class NnueNnueData
    {
        public NnueAccumulator accumulator;
        public NnueDirtyPiece dirtyPiece;

        public NnueNnueData()
        {
            accumulator = new NnueAccumulator();
            dirtyPiece = new NnueDirtyPiece();
        }
    }
}