using System;

namespace ChessDotNet.Protocols
{
    public class UCIProtocol : IChessProtocol
    {
        public void Input(string message)
        {
            var words = message.Trim().Split(' ');
            switch (words[0])
            {
                case "uci":
                    ConfigureUCI();
                    break;
                case "setoption":
                    break;
                case "isready":
                    Output("readyok");
                    break;
            }
        }

        public event Action<string> OnOutput;
        public event Action<int> OnExit;

        private void Output(string message)
        {
            OnOutput?.Invoke(message);
        }

        private void Exit(int errorCode)
        {
            OnExit?.Invoke(errorCode);
        }
        
        private void ConfigureUCI()
        {
            Output("id name Chess.NET");
            Output("id author Gediminas Masaitis");
            Output("uciok");
        }


    }
}