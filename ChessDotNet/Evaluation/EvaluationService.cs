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

        public static int BishopPairScore { get; }

        public static int RookOpenScore { get; set; }
        public static int RookSemiOpenScore { get; set; }
        public static int QueenOpenScore { get; set; }
        public static int QueenSemiOpenScore { get; set; }

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
                        PassedPawnMasksWhite[position] |= BitboardConstants.Files[file - 1];
                        PassedPawnMasksBlack[position] |= BitboardConstants.Files[file - 1];
                        IsolatedPawnMasks[position] |= BitboardConstants.Files[file - 1];
                    }
                    PassedPawnMasksWhite[position] |= BitboardConstants.Files[file];
                    PassedPawnMasksBlack[position] |= BitboardConstants.Files[file];
                    if (file < 7)
                    {
                        PassedPawnMasksWhite[position] |= BitboardConstants.Files[file + 1];
                        PassedPawnMasksBlack[position] |= BitboardConstants.Files[file + 1];
                        IsolatedPawnMasks[position] |= BitboardConstants.Files[file + 1];
                    }

                    for (var i = 0; i <= rank; i++)
                    {
                        PassedPawnMasksWhite[position] &= ~BitboardConstants.Ranks[i];
                    }
                    for (var i = 7; i >= rank; i--)
                    {
                        PassedPawnMasksBlack[position] &= ~BitboardConstants.Ranks[i];
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

            IsolatedScore = -10;
            BishopPairScore = 25;

            Weights = new[] {0, 100, 325, 325, 550, 1000, 50000, 100, 325, 325, 550, 1000, 50000 };

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
            var score = board.WhiteMaterial - board.BlackMaterial;
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

            for (var rank = 0; rank < 8; rank++)
            {
                for (var file = 0; file < 8; file++)
                {
                    var pos = rank*8 + file;
                    switch (board.ArrayBoard[pos])
                    {
                        case ChessPiece.Empty:
                            break;

                        case ChessPiece.WhitePawn:
                            score += PawnTable[pos];
                            if ((board.BitBoard[ChessPiece.BlackPawn] & PassedPawnMasksWhite[pos]) == 0)
                            {
                                score += PassedPawnScores[pos];
                            }
                            if ((board.BitBoard[ChessPiece.WhitePawn] & IsolatedPawnMasks[pos]) == 0)
                            {
                                score += IsolatedScore;
                            }
                            break;
                        case ChessPiece.BlackPawn:
                            score -= PawnTable[Mirror[pos]];
                            if ((board.BitBoard[ChessPiece.WhitePawn] & PassedPawnMasksBlack[pos]) == 0)
                            {
                                score -= PassedPawnScores[Mirror[pos]];
                            }
                            if ((board.BitBoard[ChessPiece.BlackPawn] & IsolatedPawnMasks[pos]) == 0)
                            {
                                score -= IsolatedScore;
                            }
                            break;

                        case ChessPiece.WhiteKnight:
                            score += KnightTable[pos];
                            break;
                        case ChessPiece.BlackKnight:
                            score -= KnightTable[Mirror[pos]];
                            break;

                        case ChessPiece.WhiteBishop:
                            score += BishopTable[pos];
                            break;
                        case ChessPiece.BlackBishop:
                            score -= BishopTable[Mirror[pos]];
                            break;

                        case ChessPiece.WhiteRook:
                            score += RookTable[pos];
                            if ((board.BitBoard[ChessPiece.BlackPawn] & BitboardConstants.Files[file]) == 0)
                            {
                                if ((board.BitBoard[ChessPiece.WhitePawn] & BitboardConstants.Files[file]) == 0)
                                {
                                    score += RookOpenScore;
                                }
                                else
                                {
                                    score += RookSemiOpenScore;
                                }
                            }
                            break;
                        case ChessPiece.BlackRook:
                            score -= RookTable[Mirror[pos]];
                            if ((board.BitBoard[ChessPiece.WhitePawn] & BitboardConstants.Files[file]) == 0)
                            {
                                if ((board.BitBoard[ChessPiece.BlackPawn] & BitboardConstants.Files[file]) == 0)
                                {
                                    score -= RookOpenScore;
                                }
                                else
                                {
                                    score -= RookSemiOpenScore;
                                }
                            }
                            break;

                        case ChessPiece.WhiteQueen:
                            if ((board.BitBoard[ChessPiece.BlackPawn] & BitboardConstants.Files[file]) == 0)
                            {
                                if ((board.BitBoard[ChessPiece.WhitePawn] & BitboardConstants.Files[file]) == 0)
                                {
                                    score += QueenOpenScore;
                                }
                                else
                                {
                                    score += QueenSemiOpenScore;
                                }
                            }
                            break;
                        case ChessPiece.BlackQueen:
                            if ((board.BitBoard[ChessPiece.WhitePawn] & BitboardConstants.Files[file]) == 0)
                            {
                                if ((board.BitBoard[ChessPiece.BlackPawn] & BitboardConstants.Files[file]) == 0)
                                {
                                    score -= QueenOpenScore;
                                }
                                else
                                {
                                    score -= QueenOpenScore;
                                }
                            }
                            break;

                        case ChessPiece.WhiteKing:
                            break;
                        case ChessPiece.BlackKing:
                            break;
                    }
                }

                if (board.PieceCounts[ChessPiece.WhiteBishop] == 2)
                {
                    score += BishopPairScore;
                }
                if (board.PieceCounts[ChessPiece.BlackBishop] == 2)
                {
                    score -= BishopPairScore;
                }
            }
            return score;
        }
    }
}
