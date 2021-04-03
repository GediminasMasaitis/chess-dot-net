namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public class NnueDirtyPiece
    {
        public int dirtyNum;
        public int[] pc;
        public int[] from;
        public int[] to;

        public NnueDirtyPiece()
        {
            dirtyNum = 0;
            pc = new int[3];
            from = new int[3];
            to = new int[3];
        }
    }
}