namespace ChessDotNet
{
    public struct Move
    {
        public Move(int from, int to)
        {
            From = from;
            To = to;
        }

        public int From { get; }
        public int To { get; }
    }
}