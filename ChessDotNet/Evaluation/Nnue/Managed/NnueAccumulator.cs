namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public class NnueAccumulator
    {
        public short[][] accumulation;
        public int computedAccumulation;

        public NnueAccumulator()
        {
            accumulation = new short[][] { new short[256], new short[256] };
            computedAccumulation = 0;
        }
    }
}