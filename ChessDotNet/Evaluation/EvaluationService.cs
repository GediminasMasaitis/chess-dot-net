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

        public int Evaluate(Board board)
        {
            var score = 0;
            score += EvaluateWeights(board);
            score += EvaluatePositions(board);
            return score;
        }

        public int EvaluatePositions(Board board)
        {
            var score = 0;

            for (var i = 0; i < 64; i++)
            {
                if ((board.BitBoard[ChessPiece.WhitePawn] & (1UL << i)) != 0)
                {
                    score += PawnTable[i];
                }
                else if ((board.BitBoard[ChessPiece.WhiteKnight] & (1UL << i)) != 0)
                {
                    score += KnightTable[i];
                }
                else if ((board.BitBoard[ChessPiece.WhiteBishop] & (1UL << i)) != 0)
                {
                    score += BishopTable[i];
                }
                else if ((board.BitBoard[ChessPiece.WhiteRook] & (1UL << i)) != 0)
                {
                    score += RookTable[i];
                }

                else if ((board.BitBoard[ChessPiece.BlackPawn] & (1UL << i)) != 0)
                {
                    score -= PawnTable[63-i];
                }
                else if ((board.BitBoard[ChessPiece.BlackKnight] & (1UL << i)) != 0)
                {
                    score -= KnightTable[63-i];
                }
                else if ((board.BitBoard[ChessPiece.BlackBishop] & (1UL << i)) != 0)
                {
                    score -= BishopTable[63-i];
                }
                else if ((board.BitBoard[ChessPiece.BlackRook] & (1UL << i)) != 0)
                {
                    score -= RookTable[63-i];
                }
            }

            return score;
        }

        public int EvaluateWeights(Board board)
        {
            var whitePieces = board.CountPiecesForWhite();
            var blackPieces = board.CountPiecesForBlack();

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
