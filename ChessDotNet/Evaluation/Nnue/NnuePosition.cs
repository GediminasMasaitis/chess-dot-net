using ChessDotNet.Evaluation.Nnue.Managed;

namespace ChessDotNet.Evaluation.Nnue
{
    public class NnuePosition
    {
        public int Player { get; set; }
        public int[] Pieces { get; set; }
        public int[] Squares { get; set; } 
        public int NnueCount { get; set; }
        public NnueNnueData[] Nnue { get; set; }

        public NnuePosition(bool createManagedData)
        {
            Player = 0;
            Pieces = new int[33];
            Squares = new int[33];
            NnueCount = 1;
            Nnue = new NnueNnueData[3];
            //if (createManagedData)
            {
                for (int i = 0; i < 3; i++)
                {
                    Nnue[i] = new NnueNnueData();
                }
            }
        }
    }
}