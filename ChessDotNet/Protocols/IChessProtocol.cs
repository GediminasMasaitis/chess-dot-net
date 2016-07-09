using System;

namespace ChessDotNet.Protocols
{
    public interface IChessProtocol
    {
        void Input(string message);
        event Action<string> OnOutput;
    }
}