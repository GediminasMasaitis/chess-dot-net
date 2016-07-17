namespace ChessDotNet.Protocols
{
    public interface IInterruptor
    {
        bool IsInterrupted();
        void Start();
    }
}