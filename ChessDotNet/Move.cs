namespace ChessDotNet
{
    public struct Move
    {
        public Move(byte from, byte to)
        {
            From = from;
            To = to;
        }

        public byte From { get; }
        public byte To { get; }
    }
}