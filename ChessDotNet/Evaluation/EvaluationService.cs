using System;
using ChessDotNet.Data;

namespace ChessDotNet.Evaluation
{
    public class EvaluationService
    {
        private static int[] Weights = {100,340,350,500,950,50000};

        private static int[] PawnTable { get; } = {
            0, 0, 0, 0, 0, 0, 0, 0,
            10, 10, 0, -10, -10, 0, 10, 10,
            5, 0, 0, 5, 5, 0, 0, 5,
            0, 0, 10, 20, 20, 10, 0, 0,
            5, 5, 5, 10, 10, 5, 5, 5,
            10, 10, 10, 20, 20, 10, 10, 10,
            20, 20, 20, 30, 30, 20, 20, 20,
            0, 0, 0, 0, 0, 0, 0, 0
        };

        private static int[] KnightTable { get; } =
        {
            0, -10, 0, 0, 0, 0, -10, 0,
            0, 0, 0, 5, 5, 0, 0, 0,
            0, 0, 10, 10, 10, 10, 0, 0,
            0, 0, 10, 20, 20, 10, 5, 0,
            5, 10, 15, 20, 20, 15, 10, 5,
            5, 10, 10, 20, 20, 10, 10, 5,
            0, 0, 5, 10, 10, 5, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        };

        private static int[] BishopTable { get; } =
        {
            0, 0, -10, 0, 0, -10, 0, 0,
            0, 0, 0, 10, 10, 0, 0, 0,
            0, 0, 10, 15, 15, 10, 0, 0,
            0, 10, 15, 20, 20, 15, 10, 0,
            0, 10, 15, 20, 20, 15, 10, 0,
            0, 0, 10, 15, 15, 10, 0, 0,
            0, 0, 0, 10, 10, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        };

        private static int[] RookTable { get; } =
        {
            0, 0, 5, 10, 10, 5, 0, 0,
            0, 0, 5, 10, 10, 5, 0, 0,
            0, 0, 5, 10, 10, 5, 0, 0,
            0, 0, 5, 10, 10, 5, 0, 0,
            0, 0, 5, 10, 10, 5, 0, 0,
            0, 0, 5, 10, 10, 5, 0, 0,
            25, 25, 25, 25, 25, 25, 25, 25,
            0, 0, 5, 10, 10, 5, 0, 0
        };

        public int Evaluate(BitBoards bitBoards)
        {
            var score = 0;
            score += EvaluateWeights(bitBoards);
            score += EvaluatePositions(bitBoards);
            return score;
        }

        public int EvaluatePositions(BitBoards bitBoards)
        {
            var score = 0;

            for (var i = 0; i < 64; i++)
            {
                if ((bitBoards.WhitePawns & (1UL << i)) != 0)
                {
                    score += PawnTable[i];
                }
                else if ((bitBoards.WhiteNights & (1UL << i)) != 0)
                {
                    score += KnightTable[i];
                }
                else if ((bitBoards.WhiteBishops & (1UL << i)) != 0)
                {
                    score += BishopTable[i];
                }
                else if ((bitBoards.WhiteRooks & (1UL << i)) != 0)
                {
                    score += RookTable[i];
                }

                else if ((bitBoards.BlackPawns & (1UL << i)) != 0)
                {
                    score -= PawnTable[63-i];
                }
                else if ((bitBoards.BlackNights & (1UL << i)) != 0)
                {
                    score -= KnightTable[63-i];
                }
                else if ((bitBoards.BlackBishops & (1UL << i)) != 0)
                {
                    score -= BishopTable[63-i];
                }
                else if ((bitBoards.BlackRooks & (1UL << i)) != 0)
                {
                    score -= RookTable[63-i];
                }
            }

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
