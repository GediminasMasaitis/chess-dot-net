namespace ChessDotNet.Data
{
    public class ArrayBoard : BoardBase
    {
        public ArrayBoard()
        {
            Pieces = new ChessPiece[8,8];
        }

        public ChessPiece[,] Pieces { get; }

        public ChessPiece this[int i]
        {
            get { return Pieces[i/8, i%8]; }
            set { Pieces[i/8, i%8] = value; }
        }

        public ChessPiece this[int i, int j]
        {
            get { return Pieces[i, j]; }
            set { Pieces[i, j] = value; }
        }
    }
}