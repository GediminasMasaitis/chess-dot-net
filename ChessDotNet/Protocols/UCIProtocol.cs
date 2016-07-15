using System;
using System.Linq;
using ChessDotNet.Searching;

namespace ChessDotNet.Protocols
{
    public class UCIProtocol : IChessProtocol
    {
        public UCIProtocol()
        {
            Game = new Game();
        }

        private Game Game { get; set; }

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
                case "ucinewgame":
                    Game.SetStartingPos();
                    break;
                case "position":
                {
                    var isStartPos = false;
                    var byFen = false;

                    var i = 1;
                    var positionParsingReady = false;
                    for (; i < words.Length && !positionParsingReady; i++)
                    {
                        switch (words[i])
                        {
                            case "startpos":
                                isStartPos = true;
                                break;
                            case "moves":
                                byFen = false;
                                positionParsingReady = true;
                                break;
                            case "fen":
                                byFen = true;
                                positionParsingReady = true;
                                break;
                        }
                    }

                    if (byFen)
                    {
                        var remainingText = words.Skip(i).Aggregate((c, n) => c + " " + n);
                        Game.SetPositionByFEN(remainingText);
                    }
                    else
                    {
                        var moves = words.Skip(i).ToList();
                        Game.SetPositionByMoves(isStartPos, moves);
                    }
                }
                    break;
                case "go":
                {
                    var searchParams = new SearchParams();
                    for (var i = 0; i < words.Length; i++)
                    {
                        switch (words[i])
                        {
                            case "wtime":
                                searchParams.WhiteTime = int.Parse(words[++i]);
                                break;
                            case "btime":
                                searchParams.BlackTime = int.Parse(words[++i]);
                                break;
                            case "winc":
                                searchParams.WhiteTimeIncrement = int.Parse(words[++i]);
                                break;
                            case "binc":
                                searchParams.BlackTimeIncrement = int.Parse(words[++i]);
                                break;

                        }
                    }

                    var results = Game.SearchMove(searchParams);
                    var moveStr = results[0].Move.ToPositionString();
                    var ponderStr = results[1].Move.ToPositionString();
                    Output($"bestmove {moveStr} ponder {ponderStr}");
                }
                    break;
                case "print":
                    Output(Game.Print());
                    break;
                case "quit":
                    Exit(0);
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