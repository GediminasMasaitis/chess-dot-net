using System;

namespace ChessDotNet.MoveGeneration.SlideGeneration.Magics
{
    public class RandomMagicNumberCandidateProvider : IMagicNumberCandidateProvider
    {
        private readonly Random _rng;

        public RandomMagicNumberCandidateProvider()
        {
            _rng = new Random(0);
        }

        private ulong GetRandom64()
        {
            var buf = new byte[8];
            _rng.NextBytes(buf);
            var number = BitConverter.ToUInt64(buf, 0);
            return number;
        }

        public UInt64 GetMagicNumberCandidate(int pos, bool bishop)
        {
            const int sparseness = 3;
            var candidate = ~0UL;
            for (var i = 0; i < sparseness; i++)
            {
                candidate &= GetRandom64();
            }
            return candidate;
        }
    }
}