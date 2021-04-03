using ChessDotNet.Evaluation.Nnue.Managed;

namespace ChessDotNet.Evaluation.Nnue
{
    public class NnuePosition
    {
        public int player;
        public int[] pieces;
        public int[] squares;
        public NnueNnueData[] nnue;

        public NnuePosition(bool createManagedData)
        {
            player = 0;
            pieces = new int[33];
            squares = new int[33];
            if (createManagedData)
            {
                nnue = new NnueNnueData[3];
                for (int i = 0; i < 3; i++)
                {
                    nnue[i] = new NnueNnueData();
                }
            }
        }
    }
}