using System;

namespace ChessDotNet.Search2
{
    public static class SearchConstants
    {
        public const int MaxDepth = 64;

        public const int MateScore = 30000;
        public const int MateThereshold = 29000;
        public const int Inf = short.MaxValue;

        public const int EndgameMaterial = 1300;

        public const bool Multithreading = false;
        public const int ThreadCount = Multithreading ? 8 : 1;
        public const int InitialDepth = 1;

        public static int[][] FutilityMoveCounts { get; }
        public static int[][][][] Reductions { get; }

        static SearchConstants()
        {
            FutilityMoveCounts = new int[2][];
            for (int i = 0; i < FutilityMoveCounts.Length; i++)
            {
                FutilityMoveCounts[i] = new int[16];
            }

            for (int d = 0; d < 16; ++d)
            {
                FutilityMoveCounts[0][d] = (int)(2.4 + 0.773 * Math.Pow(d + 0.00, 1.8));
                FutilityMoveCounts[1][d] = (int)(2.9 + 1.045 * Math.Pow(d + 0.49, 1.8));
            }

            var K = new double[][] { new double[]{ 0.799, 2.281 }, new double[] { 0.484, 3.023 } };
            Reductions = new int[2][][][];
            for (int pv = 0; pv <= 1; ++pv)
            {
                Reductions[pv] = new int[2][][];
                for (int imp = 0; imp <= 1; ++imp)
                {
                    Reductions[pv][imp] = new int[MaxDepth][];
                    for (int d = 1; d < MaxDepth; ++d)
                    {
                        Reductions[pv][imp][d] = new int[64];
                        for (int mc = 1; mc < 64; ++mc)
                        {
                            double r = K[pv][0] + Math.Log(d) * Math.Log(mc) / K[pv][1];

                            if (r >= 1.5)
                            {
                                Reductions[pv][imp][d][mc] = (int) r;
                            }

                            // Increase reduction when eval is not improving
                            if (pv == 0 && imp == 0 && Reductions[pv][imp][d][mc] >= 2)
                            {
                                Reductions[pv][imp][d][mc] += 1;
                            }
                        }
                    }
                }
            }
        }
    }
}