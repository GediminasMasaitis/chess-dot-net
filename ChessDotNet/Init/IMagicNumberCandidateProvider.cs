using ChessDotNet.Data;

namespace ChessDotNet.Init
{
    public interface IMagicNumberCandidateProvider
    {
        ulong GetMagicNumberCandidate(int pos, bool bishop);
    }
}