namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public class HalfKpParameters
    {
        public NnueFeatureTransformerParameters FeatureTransformer { get; set; }
        public NnueParameters Hidden1 { get; set; }
        public NnueParameters Hidden2 { get; set; }
        public NnueParameters Output { get; set; }
    }
}