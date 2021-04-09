namespace ChessDotNet.MoveGeneration.SlideGeneration.Magics
{
    public interface IMagicNumberCandidateProvider
    {
        ulong GetMagicNumberCandidate(int pos, bool bishop);
    }
}