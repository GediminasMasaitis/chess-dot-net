using System;
using ChessDotNet.Data;

namespace ChessDotNet.Evaluation
{
    class EvaluationService
    {
        private static int[] Weights = {100,340,350,500,950,50000};

        public int Evaluate(BitBoards bitBoards, bool forWhite)
        {
            var score = 0;
            score += EvaluateWeights(bitBoards);
            return score;
        }

        public int EvaluateWeights(BitBoards bitBoards)
        {
            var whitePieces = bitBoards.CountPiecesForWhite();
            var blackPieces = bitBoards.CountPiecesForBlack();

            var whiteWeights = EvaluatePieceCountWeights(whitePieces);
            var blackWeights = EvaluatePieceCountWeights(blackPieces);

            var score = whiteWeights - blackWeights;
            return score;
        }

        private int EvaluatePieceCountWeights(PieceCounts counts)
        {
            var score = 0;
            score += Weights[0]*counts.Pawns;
            score += Weights[1]*counts.Knights;
            score += Weights[2]*counts.Bishops;
            score += Weights[3]*counts.Rooks;
            score += Weights[4]*counts.Queens;
            return score;
        }
    }
}
