using System;

namespace ChessDotNet.Evaluation
{
    static class MVVLVAScoreCalculation
    {
        private static int[] PieceScores { get; }
        public static int[,] Scores { get; }

        static MVVLVAScoreCalculation()
        {
            PieceScores = new[] {0, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6};
            Scores = new int[PieceScores.Length, PieceScores.Length];

            for (var i = 0; i < PieceScores.Length; i++)
            {
                for (var j = 1; j < PieceScores.Length; j++)
                {
                    Scores[i, j] = PieceScores[j]*100 + 6 - PieceScores[i];
                }
            }
            
        }
    }
}
