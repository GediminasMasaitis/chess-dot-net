namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public class NnueDirtyPiece
    {
        public int dirtyNum;
        public byte[] pc;
        public byte[] from;
        public byte[] to;

        public NnueDirtyPiece()
        {
            dirtyNum = 0;
            pc = new byte[3];
            from = new byte[3];
            to = new byte[3];
        }
    }
}