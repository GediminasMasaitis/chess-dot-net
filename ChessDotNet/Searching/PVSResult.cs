using ChessDotNet.Data;

namespace ChessDotNet.Searching
{
    public class PVSResult
    {
        public PVSResult(int score, Board board, Move move)
        {
            Score = score;
            Board = board;
            Move = move;
        }

        public int Score { get; }
        public Board Board { get; }
        public Move Move { get; }
    }
}