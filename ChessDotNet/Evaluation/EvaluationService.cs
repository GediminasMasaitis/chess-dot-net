using System;
using ChessDotNet.Data;

namespace ChessDotNet.Evaluation
{
    public class EvaluationService : IEvaluationService
    {
        private static int[] Weights = {100, 325, 325, 550, 1000, 50000};

        private static int[] PawnTable { get; } =
        {
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

        private static int[] Mirror = {
            56  ,   57  ,   58  ,   59  ,   60  ,   61  ,   62  ,   63  ,
            48  ,   49  ,   50  ,   51  ,   52  ,   53  ,   54  ,   55  ,
            40  ,   41  ,   42  ,   43  ,   44  ,   45  ,   46  ,   47  ,
            32  ,   33  ,   34  ,   35  ,   36  ,   37  ,   38  ,   39  ,
            24  ,   25  ,   26  ,   27  ,   28  ,   29  ,   30  ,   31  ,
            16  ,   17  ,   18  ,   19  ,   20  ,   21  ,   22  ,   23  ,
            8   ,   9   ,   10  ,   11  ,   12  ,   13  ,   14  ,   15  ,
            0   ,   1   ,   2   ,   3   ,   4   ,   5   ,   6   ,   7
        };

        public int Evaluate(Board board)
        {
            var score = 0;
            score += EvaluateWeights(board);
            score += EvaluatePositions(board);

            if (!board.WhiteToMove)
            {
                score = -score;
            }
            return score;
        }

        public int EvaluatePositions(Board board)
        {
            var score = 0;

            for (var i = 0; i < 64; i++)
            {
                switch (board.ArrayBoard[i])
                {
                    case ChessPiece.Empty:
                        break;

                    case ChessPiece.WhitePawn:
                        score += PawnTable[i];
                        break;
                    case ChessPiece.BlackPawn:
                        score -= PawnTable[Mirror[i]];
                        break;

                    case ChessPiece.WhiteKnight:
                        score += KnightTable[i];
                        break;
                    case ChessPiece.BlackKnight:
                        score -= KnightTable[Mirror[i]];
                        break;

                    case ChessPiece.WhiteBishop:
                        score += BishopTable[i];
                        break;
                    case ChessPiece.BlackBishop:
                        score -= BishopTable[Mirror[i]];
                        break;

                    case ChessPiece.WhiteRook:
                        score += RookTable[i];
                        break;
                    case ChessPiece.BlackRook:
                        score -= RookTable[Mirror[i]];
                        break;

                    case ChessPiece.WhiteQueen:
                        break;
                    case ChessPiece.BlackQueen:
                        break;

                    case ChessPiece.WhiteKing:
                        break;
                    case ChessPiece.BlackKing:
                        break;
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
