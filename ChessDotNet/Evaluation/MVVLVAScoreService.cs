using System;
using ChessDotNet.Data;

namespace ChessDotNet.Evaluation
{
    static class MVVLVAScoreCalculation
    {
        private static int[] PieceScores { get; }
        public static int[][] Scores { get; }

        static MVVLVAScoreCalculation()
        {
            PieceScores = new int[ChessPiece.Count];
            PieceScores[ChessPiece.WhitePawn] = 1;
            PieceScores[ChessPiece.BlackPawn] = 1;
            PieceScores[ChessPiece.WhiteKnight] = 2;
            PieceScores[ChessPiece.BlackKnight] = 2;
            PieceScores[ChessPiece.WhiteBishop] = 3;
            PieceScores[ChessPiece.BlackBishop] = 3;
            PieceScores[ChessPiece.WhiteRook] = 4;
            PieceScores[ChessPiece.BlackRook] = 4;
            PieceScores[ChessPiece.WhiteQueen] = 5;
            PieceScores[ChessPiece.BlackQueen] = 5;
            PieceScores[ChessPiece.WhiteKing] = 6;
            PieceScores[ChessPiece.BlackKing] = 6;

            Scores = new int[PieceScores.Length][];
            for (int i = 0; i < Scores.Length; i++)
            {
                Scores[i] = new int[PieceScores.Length];
            }

            for (var i = 0; i < PieceScores.Length; i++)
            {
                for (var j = 1; j < PieceScores.Length; j++)
                {
                    var score = (PieceScores[j] * 10) + (6 - PieceScores[i]);
                    score *= 1000000;
                    score += 100000000;
                    Scores[i][j] = score;
                }
            }
            
        }
    }
}
