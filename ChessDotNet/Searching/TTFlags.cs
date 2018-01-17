using TTFlag = System.Byte;

namespace ChessDotNet.Searching
{
    public class TTFlags
    {
        public const TTFlag Beta = 0;
        public const TTFlag Exact = 1;
        public const TTFlag Alpha = 2;
    }
}