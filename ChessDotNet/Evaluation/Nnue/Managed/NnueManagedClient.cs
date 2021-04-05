using System.Runtime.Intrinsics.X86;

namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public class NnueManagedClient : INnueClient
    {
        public bool RequiresManagedData => true;
        public NnueArchitecture Architecture { get; }

        private readonly INnueClient _implementation;

        public NnueManagedClient(HalfKpParameters parameters)
        {
            NnueArchitecture architecture;
            if (Avx2.IsSupported)
            {
                architecture = NnueArchitecture.Avx2;
                _implementation = new NnueImplAvx2(parameters);
            }
            else
            {
                architecture = NnueArchitecture.Fallback;
                _implementation = new NnueImplFallback(parameters);
            }
            Architecture = architecture;
        }

        public int Evaluate(NnuePosition pos)
        {
            return _implementation.Evaluate(pos);
        }
    }
}