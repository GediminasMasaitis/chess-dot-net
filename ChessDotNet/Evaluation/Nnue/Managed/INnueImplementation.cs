namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public interface INnueImplementation
    {
        int Evaluate(NnuePosition position);
    }
}