namespace ChessDotNet.Evaluation.Nnue
{
    public interface INnueClient
    {
        bool RequiresManagedData { get; }
        int Evaluate(NnuePosition pos);
    }
}