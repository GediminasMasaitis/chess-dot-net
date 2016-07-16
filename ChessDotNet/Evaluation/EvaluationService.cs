using System;
using ChessDotNet.Data;

namespace ChessDotNet.Evaluation
{
    public class EvaluationService : IEvaluationService
    {
        public static ulong[] PassedPawnMasksWhite { get; }
        public static ulong[] PassedPawnMasksBlack { get; }
        public static ulong[] IsolatedPawnMasks { get; }

        public static int[] PassedPawnScores { get; }
        public static int IsolatedScore { get; }

        public static int[] Weights { get; }

        public static int[] PawnTable { get; }
        public static int[] KnightTable { get; }
        public static int[] BishopTable { get; }
        public static int[] RookTable { get; }

        public static int[] Mirror { get; }

        static EvaluationService()
        {
            PassedPawnMasksWhite = new ulong[64];
            PassedPawnMasksBlack = new ulong[64];
            IsolatedPawnMasks = new ulong[64];

            for (var rank = 0; rank < 8; rank++)
            {
                for (var file = 0; file < 8; file++)
                {
                    var position = rank*8 + file;
                    if (file > 0)
                    {
                        PassedPawnMasksWhite[position] |= Board.Files[file - 1];
                        PassedPawnMasksBlack[position] |= Board.Files[file - 1];
                        IsolatedPawnMasks[position] |= Board.Files[file - 1];
                    }
                    PassedPawnMasksWhite[position] |= Board.Files[file];
                    PassedPawnMasksBlack[position] |= Board.Files[file];
                    if (file < 7)
                    {
                        PassedPawnMasksWhite[position] |= Board.Files[file + 1];
                        PassedPawnMasksBlack[position] |= Board.Files[file + 1];
                        IsolatedPawnMasks[position] |= Board.Files[file + 1];
                    }

                    for (var i = 0; i <= rank; i++)
                    {
                        PassedPawnMasksWhite[position] &= ~Board.Ranks[i];
                    }
                    for (var i = 7; i >= rank; i--)
                    {
                        PassedPawnMasksBlack[position] &= ~Board.Ranks[i];
                    }
                }
            }

            PassedPawnScores = new[]
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                5, 5, 5, 5, 5, 5, 5, 5,
                10, 10, 10, 10, 10, 10, 10, 10,
                20, 20, 20, 20, 20, 20, 20, 20,
                35, 35, 35, 35, 35, 35, 35, 35,
                60, 60, 60, 60, 60, 60, 60, 60,
                100, 100, 100, 100, 100, 100, 100, 100,
                200, 200, 200, 200, 200, 200, 200, 200
            };

            Weights = new[] {100, 325, 325, 550, 1000, 50000};

            PawnTable = new[]
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

            KnightTable = new[]
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
            BishopTable = new[]
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
            RookTable = new[]
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
            Mirror = new[]
            {
                56, 57, 58, 59, 60, 61, 62, 63,
                48, 49, 50, 51, 52, 53, 54, 55,
                40, 41, 42, 43, 44, 45, 46, 47,
                32, 33, 34, 35, 36, 37, 38, 39,
                24, 25, 26, 27, 28, 29, 30, 31,
                16, 17, 18, 19, 20, 21, 22, 23,
                8, 9, 10, 11, 12, 13, 14, 15,
                0, 1, 2, 3, 4, 5, 6, 7
            };
        }

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
                        if ((board.BitBoard[ChessPiece.BlackPawn] & PassedPawnMasksWhite[i]) == 0)
                        {
                            score += PassedPawnScores[i];
                        }
                        if ((board.BitBoard[ChessPiece.WhitePawn] & IsolatedPawnMasks[i]) == 0)
                        {
                            score += IsolatedScore;
                        }
                        break;
                    case ChessPiece.BlackPawn:
                        score -= PawnTable[Mirror[i]];
                        if ((board.BitBoard[ChessPiece.WhitePawn] & PassedPawnMasksBlack[i]) == 0)
                        {
                            score -= PassedPawnScores[Mirror[i]];
                        }
                        if ((board.BitBoard[ChessPiece.BlackPawn] & IsolatedPawnMasks[i]) == 0)
                        {
                            score += IsolatedScore;
                        }
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
